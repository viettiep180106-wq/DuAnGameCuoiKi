using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[Serializable]
public struct DialogueLine
{
    public string Speaker; // "Pháp Sư" hoặc "Thánh Vật"
    public string Message;
    public bool IsPlayer;  // true = Pháp Sư (trái), false = Thánh Vật/Quái (phải)
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private RectTransform chatContent;
    [SerializeField] private GameObject playerBubbleTemplate;
    [SerializeField] private GameObject npcBubbleTemplate;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipDialogueButton;

    private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
    private Coroutine typewriterRoutine;
    private Coroutine skipAutoEndRoutine;
    private TMP_Text currentBubbleText;
    private string currentFullMessage;
    private bool isTyping = false;
    private Action onDialogueCompleted;
    private List<GameObject> spawnedBubbles = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }

        EnsureSkipButton();

        // Dọn sạch mọi bubble mẫu hoặc dummy clones lưu sẵn trong editor của chatContent
        if (chatContent != null)
        {
            for (int i = chatContent.childCount - 1; i >= 0; i--)
            {
                Transform child = chatContent.GetChild(i);
                if (child.gameObject != playerBubbleTemplate && child.gameObject != npcBubbleTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        if (playerBubbleTemplate != null) playerBubbleTemplate.SetActive(false);
        if (npcBubbleTemplate != null) npcBubbleTemplate.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (chatScrollRect != null) chatScrollRect.gameObject.SetActive(false);
    }

    private void EnsureSkipButton()
    {
        if (skipDialogueButton != null)
        {
            skipDialogueButton.onClick.RemoveAllListeners();
            skipDialogueButton.onClick.AddListener(SkipDialogue);
            skipDialogueButton.gameObject.SetActive(false);
            return;
        }

        if (dialoguePanel == null) return;

        var existing = dialoguePanel.transform.Find("SkipDialogueBtn");
        if (existing != null)
        {
            skipDialogueButton = existing.GetComponent<Button>();
            if (skipDialogueButton != null)
            {
                skipDialogueButton.onClick.RemoveAllListeners();
                skipDialogueButton.onClick.AddListener(SkipDialogue);
                skipDialogueButton.gameObject.SetActive(false);
                return;
            }
        }

        // Tạo nút SKIP >> hiện đại, rõ nét tại góc trên bên phải của dialoguePanel
        GameObject btnObj = new GameObject("SkipDialogueBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(dialoguePanel.transform, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-16f, -12f);
        rt.sizeDelta = new Vector2(85f, 32f);

        Image img = btnObj.GetComponent<Image>();
#if UNITY_EDITOR
        Sprite skipSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Buttons/btn_1.png");
        if (skipSprite != null)
        {
            img.sprite = skipSprite;
            img.type = Image.Type.Simple;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
        }
#else
        img.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
#endif

        skipDialogueButton = btnObj.GetComponent<Button>();
        skipDialogueButton.targetGraphic = img;
        ColorBlock cb = skipDialogueButton.colors;
        cb.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        cb.pressedColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        skipDialogueButton.colors = cb;
        skipDialogueButton.onClick.AddListener(SkipDialogue);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "SKIP >>";
        tmp.fontSize = 13;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.88f, 0.35f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;

        btnObj.SetActive(false);
    }

    /// <summary>
    /// Bỏ qua nhanh: Sổ toàn bộ các câu thoại còn lại ra khung chat ngay lập tức (trong 1s)
    /// Giúp người chơi vẫn giữ được đầy đủ nội dung hội thoại trong lịch sử chat
    /// </summary>
    public void SkipDialogue()
    {
        SkipDialogue(false);
    }

    public void SkipDialogue(bool immediate)
    {
        if (skipAutoEndRoutine != null)
        {
            // Bấm lần 2 trong lúc chờ 1s -> kết thúc ngay lập tức
            StopCoroutine(skipAutoEndRoutine);
            skipAutoEndRoutine = null;
            EndDialogue();
            return;
        }

        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }

        // 1. Hoàn tất ngay câu thoại đang gõ
        if (isTyping && currentBubbleText != null)
        {
            currentBubbleText.text = currentFullMessage;
        }
        isTyping = false;

        // 2. Sổ toàn bộ các câu thoại còn lại trong hàng đợi ra khung chat ngay
        while (dialogueQueue.Count > 0)
        {
            DialogueLine line = dialogueQueue.Dequeue();
            GameObject template = line.IsPlayer ? playerBubbleTemplate : npcBubbleTemplate;
            if (template != null && chatContent != null)
            {
                GameObject newBubble = Instantiate(template, chatContent);
                newBubble.SetActive(true);
                FormatBubble(newBubble, line.IsPlayer, line.Speaker, line.Message);
                spawnedBubbles.Add(newBubble);
            }
        }

        ScrollToBottom();

        // 3. Trong PlayMode: tạm dừng 1.0s để người chơi thấy đầy đủ hội thoại rồi tự động kết thúc.
        // Trong EditMode/Unit test hoặc khi immediate = true: kết thúc ngay lập tức.
        if (immediate || !Application.isPlaying)
        {
            EndDialogue();
        }
        else
        {
            skipAutoEndRoutine = StartCoroutine(WaitAndEndAfterSkip(1.0f));
        }
    }

    private IEnumerator WaitAndEndAfterSkip(float delay)
    {
        yield return new WaitForSeconds(delay);
        skipAutoEndRoutine = null;
        EndDialogue();
    }

    /// <summary>
    /// Kịch bản thoại thoát chạy khi bước vào đầu wave (trước khi lia cam soi quái):
    /// Wave 1: Thoại mở đầu kinh điển
    /// Wave >= 2: Thoại thở phào vì vừa thoát chết, cắt đuôi được đàn quái trước
    /// </summary>
    public void StartEscapeDialogue(int wave, Action onComplete)
    {
        if (skipAutoEndRoutine != null)
        {
            StopCoroutine(skipAutoEndRoutine);
            skipAutoEndRoutine = null;
        }

        onDialogueCompleted = onComplete;
        dialogueQueue.Clear();
        ClearChatHistory();

        if (skipDialogueButton != null) skipDialogueButton.gameObject.SetActive(true);

        if (wave <= 1)
        {
            dialogueQueue.Enqueue(new DialogueLine { 
                Speaker = "Pháp Sư", 
                Message = "Hộc... hộc... Xém tí nữa thì 'bay màu'! Quỷ đông như đi trẩy hội thế này, mệt xỉu up xỉu down!", 
                IsPlayer = true 
            });
            dialogueQueue.Enqueue(new DialogueLine { 
                Speaker = "Thánh Vật", 
                Message = "Bảo tập cardio thêm thì lười! Nhìn kìa, vạt áo choàng rách te tua rồi đấy 'đấng cứu thế' của tui!", 
                IsPlayer = false 
            });
            dialogueQueue.Enqueue(new DialogueLine { 
                Speaker = "Pháp Sư", 
                Message = "Biết thế ở nhà ngủ nướng cho lành. Nhóm tí lửa sưởi ấm xem nào!", 
                IsPlayer = true 
            });
        }
        else
        {
            dialogueQueue.Enqueue(new DialogueLine { 
                Speaker = "Pháp Sư", 
                Message = "Phù! May quá, rốt cuộc cũng cắt đuôi được đợt quái trước rồi! Tưởng đâu nãy 'bay màu' luôn chứ, chạy muốn đứt cả hơi!", 
                IsPlayer = true 
            });
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (chatScrollRect != null) chatScrollRect.gameObject.SetActive(true);
        DisplayNextLine();
    }

    /// <summary>
    /// Kịch bản thoại khi camera lia sang soi quái vật ngoài màn hình:
    /// Pháp Sư phàn nàn về chủng loại & nguyên tố quái mới kéo tới
    /// (Đã lược bỏ câu thoại Thánh vật giục xếp đá theo yêu cầu người dùng)
    /// </summary>
    public void StartEnemyInspectDialogue(int wave, Action onComplete)
    {
        if (skipAutoEndRoutine != null)
        {
            StopCoroutine(skipAutoEndRoutine);
            skipAutoEndRoutine = null;
        }

        onDialogueCompleted = onComplete;
        dialogueQueue.Clear();
        ClearChatHistory();

        if (skipDialogueButton != null) skipDialogueButton.gameObject.SetActive(true);

        string enemyName = "Lũ Quỷ";
        string elementName = "Nguyên Tố";

        if (LevelManager.instance != null)
        {
            StepData stepData = LevelManager.instance.GetStepData(wave);
            if (stepData != null)
            {
                ElementType enemyElem = stepData.GetEnemyElement();
                elementName = ElementalHelper.GetVietnameseName(enemyElem);

                if (stepData.GetListEnemy() != null && stepData.GetListEnemy().Count > 0)
                {
                    EnemyType eType = stepData.GetListEnemy()[0].GetName();
                    enemyName = GetEnemyDisplayName(eType);
                }
            }
        }

        dialogueQueue.Enqueue(new DialogueLine { 
            Speaker = "Pháp Sư", 
            Message = $"Ủa mà khoan... Đùa nhau à?! Đằng kia lại có đàn {enemyName} hệ {elementName} đang lù lù kéo tới kìa! Cái hầm ngục này mở tiệc buffet quái hay sao mà đông thế?!", 
            IsPlayer = true 
        });

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (chatScrollRect != null) chatScrollRect.gameObject.SetActive(true);
        DisplayNextLine();
    }

    public void StartDialogueQueue(List<DialogueLine> lines, Action onComplete)
    {
        if (skipAutoEndRoutine != null)
        {
            StopCoroutine(skipAutoEndRoutine);
            skipAutoEndRoutine = null;
        }

        onDialogueCompleted = onComplete;
        dialogueQueue.Clear();
        ClearChatHistory();

        if (skipDialogueButton != null) skipDialogueButton.gameObject.SetActive(true);

        if (lines != null)
        {
            foreach (var l in lines)
            {
                dialogueQueue.Enqueue(l);
            }
        }

        if (dialogueQueue.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (chatScrollRect != null) chatScrollRect.gameObject.SetActive(true);
        DisplayNextLine();
    }

    public void PlayPreWaveNarrative(Action onComplete)
    {
        StartDialogueQueue(DialogueDatabase.GetPreWaveLines(), onComplete);
    }

    public void PlayDiscoveryNarrative(StepEventType eventType, Action onComplete)
    {
        List<DialogueLine> lines;
        switch (eventType)
        {
            case StepEventType.RuneStone_Roll:
                lines = DialogueDatabase.GetRollIntroLines();
                break;
            case StepEventType.RuneStone_Altar:
                lines = DialogueDatabase.GetAltarIntroLines();
                break;
            case StepEventType.BeastBetting:
                lines = DialogueDatabase.GetBeastBettingIntroLines();
                break;
            case StepEventType.RuneStone_Chest:
                lines = DialogueDatabase.GetChestIntroLines();
                break;
            default:
                lines = DialogueDatabase.GetDiscoveryBannerLines();
                break;
        }
        StartDialogueQueue(lines, onComplete);
    }

    public void PlayCombatNarrative(int currentStep, Action onComplete)
    {
        StepDifficulty diff = StepDifficulty.Normal;
        bool isBoss = false;
        bool isFinalBoss = false;

        if (LevelManager.instance != null)
        {
            var stepData = LevelManager.instance.GetStepData(currentStep);
            if (stepData != null)
            {
                diff = stepData.GetDifficulty();
            }
        }

        if (GameManager.instance != null)
        {
            isBoss = currentStep == GameManager.instance.TotalSteps;
            isFinalBoss = isBoss && GameManager.instance.CurrentLevel >= 50;
        }

        StartDialogueQueue(DialogueDatabase.GetCombatIntroLines(diff, isFinalBoss, isBoss), onComplete);
    }

    public void PlayVictoryNarrative(Action onComplete)
    {
        int step = GameManager.instance != null ? GameManager.instance.CurrentStep : 1;
        PlayVictoryNarrative(step, onComplete);
    }

    public void PlayVictoryNarrative(int currentStep, Action onComplete)
    {
        bool isBoss = false;
        bool isFinalBoss = false;
        if (GameManager.instance != null)
        {
            isBoss = currentStep == GameManager.instance.TotalSteps;
            isFinalBoss = isBoss && currentStep >= 10;
        }
        if (LevelManager.instance != null)
        {
            var stepData = LevelManager.instance.GetStepData(currentStep);
            if (stepData != null && stepData.GetDifficulty() >= StepDifficulty.Hard)
            {
                isBoss = true;
            }
        }

        Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        float hpRatio = player != null && player.MaxHealth > 0 ? player.CurrentHP / player.MaxHealth : 1f;

        List<DialogueLine> lines = DialogueDatabase.GetCombatVictoryLines(isBoss, isFinalBoss, hpRatio);
        StartDialogueQueue(lines, onComplete);
    }

    public void PlayDefeatNarrative(Action onComplete)
    {
        StartDialogueQueue(DialogueDatabase.GetCombatDefeatLines(), onComplete);
    }

    /// <summary>
    /// Kịch bản mở đầu wave nghỉ: Bước vào hang sưởi ấm & hồi sức
    /// </summary>
    public void StartWaveRestDialogue(int wave, Action onComplete)
    {
        StartEscapeDialogue(wave, onComplete);
    }

    private string GetEnemyDisplayName(EnemyType type)
    {
        return type switch
        {
            EnemyType.QuyRung => "Quỷ Rừng",
            EnemyType.BoRac => "Bọ Rác",
            EnemyType.TinhLinhLua => "Tinh Linh Lửa",
            EnemyType.ChoSanDiaNguc => "Chó Săn Địa Ngục",
            EnemyType.HoaQuyNho => "Hỏa Quỷ Nhỏ",
            EnemyType.SlimeBun => "Slime Bùn",
            EnemyType.LinhDat => "Linh Đất",
            EnemyType.SlimeNuoc => "Slime Nước",
            EnemyType.ThuyQuaiNho => "Thủy Quái Nhỏ",
            EnemyType.GolemThep => "Golem Thép",
            EnemyType.HatNhamThach => "Hạt Nham Thạch (BOSS)",
            _ => type.ToString()
        };
    }

    private string GetElementDisplayName(ElementType elem)
    {
        return elem switch
        {
            ElementType.Wood => "Mộc (Thảo Mộc)",
            ElementType.Fire => "Hỏa (Rực Lửa)",
            ElementType.Earth => "Thổ (Đất Đá)",
            ElementType.Water => "Thủy (Băng Nước)",
            ElementType.Metal => "Kim (Kim Loại)",
            _ => elem.ToString()
        };
    }

    private string GetCounterElementDisplayName(ElementType elem)
    {
        return elem switch
        {
            ElementType.Wood => "hệ Kim hoặc Hỏa",
            ElementType.Fire => "hệ Thủy",
            ElementType.Earth => "hệ Mộc",
            ElementType.Water => "hệ Thổ",
            ElementType.Metal => "hệ Hỏa",
            _ => "nguyên tố tương khắc"
        };
    }

    public void StartLevel1IntroDialogue(Action onComplete)
    {
        StartWaveRestDialogue(1, onComplete);
    }

    /// <summary>
    /// Kịch bản Cố định khi bắt đầu vào trận: Quỷ kéo đến (SC_41 -> SC_44 từ GDD)
    /// Kích hoạt khi người chơi nhấn nút [BẮT ĐẦU] trước khi quái vật lao vào
    /// </summary>
    public void StartMonsterAlertDialogue(Action onComplete)
    {
        if (skipAutoEndRoutine != null)
        {
            StopCoroutine(skipAutoEndRoutine);
            skipAutoEndRoutine = null;
        }

        onDialogueCompleted = onComplete;
        dialogueQueue.Clear();
        ClearChatHistory();

        if (skipDialogueButton != null) skipDialogueButton.gameObject.SetActive(true);

        dialogueQueue.Enqueue(new DialogueLine { 
            Speaker = "Thánh Vật", 
            Message = "Hú còi khẩn cấp! Cảm biến ma năng rung lắc dữ dội, đàn quỷ khát máu đang tràn ngập trước cửa hang kìa!", 
            IsPlayer = false 
        });
        dialogueQueue.Enqueue(new DialogueLine { 
            Speaker = "Pháp Sư", 
            Message = "Đang than thở dở mà lũ quái vô duyên cũng không tha! Đã thế trẫm cho các ngươi nếm mùi gậy ma thuật!", 
            IsPlayer = true 
        });
        dialogueQueue.Enqueue(new DialogueLine { 
            Speaker = "Thánh Vật", 
            Message = "Đá nguyên tố đã lên nòng, mana tràn bình! Chiến hết mình đi, sống sót về đây ta lại cho quay tiếp!", 
            IsPlayer = false 
        });
        dialogueQueue.Enqueue(new DialogueLine { 
            Speaker = "Pháp Sư", 
            Message = "Dù buff hay debuff thì trình độ này vẫn ao trình! Ra đây hết xem ai mới là bá chủ hầm ngục!", 
            IsPlayer = true 
        });

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (chatScrollRect != null) chatScrollRect.gameObject.SetActive(true);
        DisplayNextLine();
    }

    public void ClearChatHistory()
    {
        foreach (var b in spawnedBubbles)
        {
            if (b != null) Destroy(b);
        }
        spawnedBubbles.Clear();
    }

    public void AddDirectDialogue(string speaker, string message, bool isPlayer)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (chatScrollRect != null) chatScrollRect.gameObject.SetActive(true);

        GameObject template = isPlayer ? playerBubbleTemplate : npcBubbleTemplate;
        if (template == null || chatContent == null) return;

        GameObject newBubble = Instantiate(template, chatContent);
        newBubble.SetActive(true);
        FormatBubble(newBubble, isPlayer, speaker, message);
        spawnedBubbles.Add(newBubble);

        ScrollToBottom();
    }

    private void OnNextClicked()
    {
        if (isTyping)
        {
            // Bấm khi đang gõ: Hiện hết chữ ngay
            if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
            if (currentBubbleText != null) currentBubbleText.text = currentFullMessage;
            isTyping = false;
            ScrollToBottom();
            StartCoroutine(WaitAndAdvance(1.2f));
        }
    }

    private void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();
        currentFullMessage = line.Message;

        // Chọn template tương ứng: Player (trái) hoặc NPC (phải)
        GameObject template = line.IsPlayer ? playerBubbleTemplate : npcBubbleTemplate;
        if (template == null || chatContent == null) return;

        // Sinh bong bóng chat mới vào Scroll Content và định dạng chuẩn xác theo toàn bộ câu thoại
        GameObject newBubble = Instantiate(template, chatContent);
        newBubble.SetActive(true);
        FormatBubble(newBubble, line.IsPlayer, line.Speaker, line.Message);
        spawnedBubbles.Add(newBubble);

        currentBubbleText = newBubble.transform.Find("Bubble/Message")?.GetComponent<TMP_Text>();

        // Chạy hiệu ứng gõ chữ
        if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
        typewriterRoutine = StartCoroutine(TypewriterAndAutoAdvance(line.Message));
    }

    private IEnumerator TypewriterAndAutoAdvance(string sentence)
    {
        isTyping = true;
        if (currentBubbleText != null) currentBubbleText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            if (currentBubbleText != null) currentBubbleText.text += letter;
            ScrollToBottom();
            yield return new WaitForSeconds(0.02f);
        }

        isTyping = false;
        ScrollToBottom();

        // TỰ ĐỘNG CHUYỂN THOẠI: Dừng 1.2s cho người chơi đọc rồi tự động nhảy thoại nhân vật tiếp theo
        yield return new WaitForSeconds(1.2f);
        DisplayNextLine();
    }

    private IEnumerator WaitAndAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        DisplayNextLine();
    }

    private void FormatBubble(GameObject bubble, bool isPlayer, string speaker = null, string message = null)
    {
        if (bubble == null) return;

        var avatarRt = bubble.transform.Find("Avatar")?.GetComponent<RectTransform>();
        if (avatarRt != null)
        {
            avatarRt.sizeDelta = new Vector2(80f, 80f);
            var avLe = avatarRt.GetComponent<LayoutElement>();
            if (avLe == null) avLe = avatarRt.gameObject.AddComponent<LayoutElement>();
            avLe.preferredWidth = 80f;
            avLe.preferredHeight = 80f;
        }

        var speakerText = bubble.transform.Find("Bubble/Speaker")?.GetComponent<TMP_Text>();
        if (speakerText != null)
        {
            if (speaker != null) speakerText.text = speaker;
            speakerText.fontSize = 20f;
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.color = isPlayer ? new Color(1f, 0.867f, 0.267f, 1f) : new Color(0.9f, 0.54f, 0.21f, 1f);
        }

        var msgText = bubble.transform.Find("Bubble/Message")?.GetComponent<TMP_Text>();
        if (msgText != null)
        {
            if (message != null) msgText.text = message;
            msgText.fontSize = 20f;
            msgText.color = Color.white;
            msgText.enableAutoSizing = false; // Không tự co nhỏ kích thước font khi câu dài
            msgText.textWrappingMode = TextWrappingModes.Normal;
        }

        float maxBubbleWidth = 580f;
        float minBubbleWidth = 280f;
        float paddingLeft = 18f;
        float paddingRight = 18f;
        float paddingTop = 12f;
        float paddingBottom = 14f;
        float spacing = 4f;

        string fullMsg = !string.IsNullOrEmpty(message) ? message : (msgText != null ? msgText.text : "");
        string spk = !string.IsNullOrEmpty(speaker) ? speaker : (speakerText != null ? speakerText.text : "");

        // Tính toán độ rộng bong bóng co giãn linh hoạt theo độ dài tin nhắn (tối đa 580f)
        float singleLineWidth = msgText != null ? msgText.GetPreferredValues(fullMsg).x : minBubbleWidth;
        float preferredBubbleW = singleLineWidth + paddingLeft + paddingRight + 14f;
        float bubbleWidth = Mathf.Clamp(preferredBubbleW, minBubbleWidth, maxBubbleWidth);
        float textWidth = bubbleWidth - (paddingLeft + paddingRight);

        float speakerH = speakerText != null ? speakerText.GetPreferredValues(spk, textWidth, float.PositiveInfinity).y : 22f;
        float msgH = msgText != null ? msgText.GetPreferredValues(fullMsg, textWidth, float.PositiveInfinity).y : 40f;
        float totalBubbleH = Mathf.Max(80f, paddingTop + speakerH + spacing + msgH + paddingBottom);

        // Căn chỉnh kích thước hộp Bubble chuẩn xác, ôm khít nội dung text
        var bubbleRt = bubble.transform.Find("Bubble")?.GetComponent<RectTransform>();
        if (bubbleRt != null)
        {
            bubbleRt.sizeDelta = new Vector2(bubbleWidth, totalBubbleH);
            var le = bubbleRt.GetComponent<LayoutElement>();
            if (le == null) le = bubbleRt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = bubbleWidth;
            le.preferredHeight = totalBubbleH;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            var vlg = bubbleRt.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.padding = new RectOffset((int)paddingLeft, (int)paddingRight, (int)paddingTop, (int)paddingBottom);
                vlg.spacing = spacing;
            }
        }

        // Căn chỉnh kích thước Root của bong bóng để ChatContent xếp đều đặn, không đè lên nhau
        var rootRt = bubble.GetComponent<RectTransform>();
        if (rootRt != null)
        {
            float rootWidth = Mathf.Min(820f, bubbleWidth + 96f);
            rootRt.sizeDelta = new Vector2(rootWidth, totalBubbleH);
            var rootLe = bubble.GetComponent<LayoutElement>();
            if (rootLe == null) rootLe = bubble.AddComponent<LayoutElement>();
            rootLe.preferredWidth = rootWidth;
            rootLe.preferredHeight = totalBubbleH;
            rootLe.flexibleWidth = 0f;
            rootLe.flexibleHeight = 0f;
        }

        var hlg = bubble.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            if (!isPlayer)
            {
                hlg.childAlignment = TextAnchor.MiddleRight;
                hlg.padding.right = 14;
                hlg.padding.left = 4;
            }
            else
            {
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.padding.left = 8;
                hlg.padding.right = 4;
            }
        }

        if (bubbleRt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRt);
        if (rootRt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rootRt);
    }

    public void ScrollToBottom()
    {
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            if (chatContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
            }
            chatScrollRect.verticalNormalizedPosition = 0f;

            if (gameObject.activeInHierarchy && Application.isPlaying)
            {
                StartCoroutine(DelayedScrollToBottom());
            }
        }
    }

    private IEnumerator DelayedScrollToBottom()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            if (chatContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
            }
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void HideDialogue()
    {
        if (skipAutoEndRoutine != null)
        {
            StopCoroutine(skipAutoEndRoutine);
            skipAutoEndRoutine = null;
        }
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }
        isTyping = false;
        if (skipDialogueButton != null)
        {
            skipDialogueButton.gameObject.SetActive(false);
        }
        if (chatScrollRect != null)
        {
            chatScrollRect.gameObject.SetActive(false);
        }
    }

    private void EndDialogue()
    {
        if (skipAutoEndRoutine != null)
        {
            StopCoroutine(skipAutoEndRoutine);
            skipAutoEndRoutine = null;
        }
        if (skipDialogueButton != null)
        {
            skipDialogueButton.gameObject.SetActive(false);
        }
        if (chatScrollRect != null)
        {
            chatScrollRect.gameObject.SetActive(false);
        }
        ScrollToBottom();
        onDialogueCompleted?.Invoke();
    }
}
