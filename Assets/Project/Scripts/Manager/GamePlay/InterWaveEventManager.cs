using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterWaveEventManager : MonoBehaviour
{
    private static InterWaveEventManager _instance;
    public static InterWaveEventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InterWaveEventManager>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    GameObject go = new GameObject("InterWaveEventManager", typeof(InterWaveEventManager));
                    _instance = go.GetComponent<InterWaveEventManager>();
                }
            }
            return _instance;
        }
        set => _instance = value;
    }
    public static void SetInstanceForTest(InterWaveEventManager instance) => _instance = instance;

    public event Action<StepEventType, BuffDebuffType, bool> OnEventResolved; // (eventType, resultBuff, isBuff)
    public static event Action<BuffDebuffType, bool> OnBuffDebuffApplied;
    public static event Action OnBuffDebuffReset;

    // Danh sách Buff tồn tại xuyên suốt toàn bộ Level (Cường Hóa, Passive Buffs, Rare Buffs)
    private readonly List<BuffDebuffType> _activeBuffsInLevel = new List<BuffDebuffType>();
    // Danh sách Debuff tồn tại xuyên suốt toàn bộ Level (Theo yêu cầu GDD & người chơi: Toàn bộ debuff kéo dài 1 Level)
    private readonly List<BuffDebuffType> _activeDebuffsInLevel = new List<BuffDebuffType>();

    public IReadOnlyList<BuffDebuffType> ActiveBuffsInLevel => _activeBuffsInLevel;
    public IReadOnlyList<BuffDebuffType> ActiveDebuffsInLevel => _activeDebuffsInLevel;

    [Header("Passive & Debuff Cooldowns (2 Round Cooldown)")]
    public int DoubleSkillCooldown = 0;
    public int DoubleStoneEffectCooldown = 0;
    public int KhoaSkillPassiveCooldown = 0;
    public int XoaPassiveCooldown = 0;

    public bool HasActiveBuff(BuffDebuffType buff) => _activeBuffsInLevel.Contains(buff);
    public bool HasActiveDebuff(BuffDebuffType debuff) => _activeDebuffsInLevel.Contains(debuff);

    public bool CanTriggerDoubleSkill() => HasActiveBuff(BuffDebuffType.Buff_Passive_DoubleSkill) && DoubleSkillCooldown == 0;
    public void ConsumeDoubleSkill() { DoubleSkillCooldown = 2; }

    public bool CanTriggerDoubleStoneEffect() => HasActiveBuff(BuffDebuffType.Buff_Passive_DoubleStoneEffect) && DoubleStoneEffectCooldown == 0;
    public void ConsumeDoubleStoneEffect() { DoubleStoneEffectCooldown = 2; }

    public bool CanTriggerKhoaSkillPassive() => HasActiveDebuff(BuffDebuffType.Debuff_KhoaSkillPassive) && KhoaSkillPassiveCooldown == 0;
    public void ConsumeKhoaSkillPassive() { KhoaSkillPassiveCooldown = 2; }

    public bool CanTriggerXoaPassive() => HasActiveDebuff(BuffDebuffType.Debuff_XoaPassive) && XoaPassiveCooldown == 0;
    public void ConsumeXoaPassive() { XoaPassiveCooldown = 2; }

    /// <summary>
    /// Kiểm tra xem buff có phải là buff tức thời 1 lần (Hồi máu, Hồi lõi, Reset CD) hay không
    /// Theo yêu cầu: Các buff tức thời này không đưa vào thanh buff_debuff_container
    /// </summary>
    public static bool IsInstantBuff(BuffDebuffType type)
    {
        return type == BuffDebuffType.Buff_TaiTao_Energy
            || type == BuffDebuffType.Buff_TaiTao_HealHP
            || type == BuffDebuffType.Buff_TaiTao_HealHP20
            || type == BuffDebuffType.Buff_LamMoiSkillActive;
    }

    public void ResetLevelBuffsDebuffs()
    {
        _activeBuffsInLevel.Clear();
        _activeDebuffsInLevel.Clear();
        DoubleSkillCooldown = 0;
        DoubleStoneEffectCooldown = 0;
        KhoaSkillPassiveCooldown = 0;
        XoaPassiveCooldown = 0;
        OnBuffDebuffReset?.Invoke();
    }

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (CombatTurnManager.Instance != null)
        {
            CombatTurnManager.Instance.OnRoundEnded += HandleRoundEnded;
            CombatTurnManager.Instance.OnTurnStateChanged += HandleTurnStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (CombatTurnManager.Instance != null)
        {
            CombatTurnManager.Instance.OnRoundEnded -= HandleRoundEnded;
            CombatTurnManager.Instance.OnTurnStateChanged -= HandleTurnStateChanged;
        }
    }

    private void HandleRoundEnded()
    {
        // Giảm cooldown 2 Round của các buff passive và debuff khi kết thúc mỗi round
        if (DoubleSkillCooldown > 0)
        {
            DoubleSkillCooldown--;
        }
        if (DoubleStoneEffectCooldown > 0)
        {
            DoubleStoneEffectCooldown--;
        }
        if (KhoaSkillPassiveCooldown > 0)
        {
            KhoaSkillPassiveCooldown--;
        }
        if (XoaPassiveCooldown > 0)
        {
            XoaPassiveCooldown--;
        }
    }

    private void HandleTurnStateChanged(CombatTurnState state)
    {
        // Toàn bộ Debuff là xuyên suốt 1 Level (10 Wave), KHÔNG tự động revert khi kết thúc 1 Wave!
    }

    /// <summary>
    /// Giữ lại hàm tương thích ngược. Debuff chỉ được reset khi toàn bộ Level kết thúc qua ResetLevelBuffsDebuffs()
    /// </summary>
    public void RevertWaveScopedDebuffs()
    {
        // Không revert giữa các wave theo yêu cầu: Toàn bộ debuff kéo dài 1 Level
    }

    /// <summary>
    /// Kích hoạt kịch bản sự kiện giữa các Wave dựa trên StepData và dữ liệu GDD
    /// </summary>
    public void TriggerInterWaveEvent(StepEventType eventType, Action onComplete)
    {
        if (eventType == StepEventType.None)
        {
            onComplete?.Invoke();
            return;
        }

        if (eventType == StepEventType.RandomEvent)
        {
            // Tạm thời bỏ hoạt động Cá Cược (BeastBetting) do chưa có UI theo yêu cầu của user
            StepEventType[] possibleEvents = new StepEventType[] {
                StepEventType.RuneStone_Gacha,
                StepEventType.RuneStone_Roll,
                StepEventType.RuneStone_Chest,
                StepEventType.RuneStone_Altar
            };
            eventType = possibleEvents[UnityEngine.Random.Range(0, possibleEvents.Length)];
        }

        bool isRuneStoneEvent = eventType == StepEventType.RuneStone_Gacha ||
                               eventType == StepEventType.RuneStone_Roll ||
                               eventType == StepEventType.RuneStone_Chest ||
                               eventType == StepEventType.RuneStone_Altar;

        if (isRuneStoneEvent && RuneEventUIController.Instance != null)
        {
            RuneEventUIController.Instance.StartRuneEvent(eventType, onComplete);
        }
        else
        {
            StartCoroutine(RunEventRoutine(eventType, onComplete));
        }
    }

    private IEnumerator RunEventRoutine(StepEventType eventType, Action onComplete)
    {
        if (eventType == StepEventType.RandomEvent)
        {
            StepEventType[] possibleEvents = new StepEventType[] {
                StepEventType.RuneStone_Gacha,
                StepEventType.RuneStone_Roll,
                StepEventType.RuneStone_Chest,
                StepEventType.RuneStone_Altar
            };
            eventType = possibleEvents[UnityEngine.Random.Range(0, possibleEvents.Length)];
        }

        switch (eventType)
        {
            case StepEventType.Relic_OpeningBuff:
                yield return StartCoroutine(RunRelicOpeningBuffRoutine());
                break;

            case StepEventType.RuneStone_Gacha:
                yield return StartCoroutine(RunRuneGachaRoutine());
                break;

            case StepEventType.RuneStone_Roll:
                yield return StartCoroutine(RunRuneRollRoutine());
                break;

            case StepEventType.RuneStone_Chest:
                yield return StartCoroutine(RunRuneChestRoutine());
                break;

            case StepEventType.RuneStone_Altar:
                yield return StartCoroutine(RunRuneAltarRoutine());
                break;

            case StepEventType.BeastBetting:
                yield return StartCoroutine(RunBeastBettingRoutine());
                break;
        }

        // Đoạn thoại báo động quái kéo đến kết thúc wave nghỉ (SC_41 -> SC_44)
        yield return StartCoroutine(RunMonsterAlertRoutine());

        onComplete?.Invoke();
    }

    // --- SỰ KIỆN MỞ ĐẦU WAVE 1: PHƯỚC LÀNH THÁNH VẬT (SC_05 -> SC_07) ---
    private IEnumerator RunRelicOpeningBuffRoutine()
    {
        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Chúc mừng đã hoàn thành trận xếp đá đầu tiên! Mau nhận món quà phước lành mở đầu từ thánh vật này!", false);
        yield return new WaitForSeconds(1.4f);

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Ôi chu choa, hôm nay Thánh Vật thơm thảo thế? Để xem được buff xịn nào!", true);
        yield return new WaitForSeconds(1.4f);

        BuffDebuffType buff = GetRandomBuff();
        ApplyBuffEffect(buff);

        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Tada! Ánh sáng thánh linh ban phước: nhận ngay [{GetBuffName(buff)}]!", false);
        yield return new WaitForSeconds(1.4f);

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Quá đã luôn! Có thêm sức mạnh này thì lũ quỷ wave đầu chỉ là muỗi! Lên đường thôi!", true);
        yield return new WaitForSeconds(1.4f);
    }

    // --- HOẠT ĐỘNG 1.1: GACHA VÒNG QUAY (SC_09 -> SC_14) ---
    private IEnumerator RunRuneGachaRoutine()
    {
        if (Player.Instance != null) Player.Instance.TrySpendEnergy(25);

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Ui da! Cái gì cấn chân trẫm thế này?! Một phiến đá rune cổ phát sáng lập lòe!", true);
        yield return new WaitForSeconds(1.4f);

        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Tada! Bàn xoay ma thuật 6 ô hiện hình: 3 Buff 3 Debuff đan xen! Tốn 25 lõi quay đổi đời đi bạn êi!", false);
        yield return new WaitForSeconds(1.4f);

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Nhìn tỉ lệ 50/50 này tim đập chân run! Xin tổ tiên phù hộ độ trì quay trúng ô vàng kim!", true);
        yield return new WaitForSeconds(1.4f);

        // Quay ngẫu nhiên 50/50
        bool isBuff = UnityEngine.Random.value < 0.5f;
        if (isBuff)
        {
            BuffDebuffType buff = GetRandomBuff(false); // GDD: Gacha vòng quay chỉ gồm Buff Thường & Debuff
            ApplyBuffEffect(buff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Keng keng keng! Nổ hũ cực phẩm! Trúng ô kim cương: nhận ngay [{GetBuffName(buff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Ối dồi ôi uy tín luôn! Đã bảo tay này vàng ròng mà! Wave sau quái chuẩn bị quỳ rạp!", true);
        }
        else
        {
            BuffDebuffType debuff = GetRandomDebuff();
            ApplyDebuffEffect(debuff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Tèn ten... Mũi tên lệch sang ô tử thần! Nhận trọn hiệu ứng thốn: [{GetBuffName(debuff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Ủa alo?! Thánh Vật gian lận cài bẫy đúng không?! Dính quả debuff này thì gánh còng cả lưng!", true);
        }
        yield return new WaitForSeconds(1.4f);
    }

    // --- HOẠT ĐỘNG 1.2: ROLL THẺ BÀI (SC_15 -> SC_20) ---
    private IEnumerator RunRuneRollRoutine()
    {
        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Chào mừng đến sòng bài cổ ngữ! 3 thẻ bài ma pháp bí ẩn đang úp, lần roll đầu tiên hoàn toàn free nhé!", false);
        yield return new WaitForSeconds(1.4f);

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Để trẫm thẩm định nhân phẩm xem nào! Nếu bài phèn thì bỏ thêm 5 lõi đổi lượt săn buff Hiếm!", true);
        yield return new WaitForSeconds(1.4f);

        BuffDebuffType buff = GetRandomBuff(true);
        ApplyBuffEffect(buff);
        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Lật bài thần thánh! Ra ngay thẻ bài lấp lánh: bạn nhận được [{GetBuffName(buff)}]!", false);
        yield return new WaitForSeconds(1.4f);
        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Haha đúng là tay thơm có số! Thẻ xịn thế này wave sau quái chỉ có nước khóc thét!", true);
        yield return new WaitForSeconds(1.4f);
    }

    // --- HOẠT ĐỘNG 1.3: MỞ RƯƠNG BÍ ẨN (SC_21 -> SC_26) ---
    private IEnumerator RunRuneChestRoutine()
    {
        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Một chiếc rương đá phong ấn kèm một mảnh giấy tâm sự bí ẩn! Bên trong có hai phong thư: 1 phúc lành và 1 tai họa đấy!", false);
        yield return new WaitForSeconds(1.4f);

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Thư tay tiền nhân à? Để trẫm vận dụng IQ vô cực giải mã câu đố xem phong thư nào giấu kho báu!", true);
        yield return new WaitForSeconds(1.4f);

        bool isBuff = UnityEngine.Random.value < 0.5f;
        if (isBuff)
        {
            BuffDebuffType buff = GetRandomBuff(true);
            ApplyBuffEffect(buff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Chọn đúng phong thư định mệnh rồi! Kho báu tỏa sáng, nhận ngay: [{GetBuffName(buff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Thấy chưa, thiên tài suy luận không bao giờ đoán sai! Đúng là ao trình vũ trụ!", true);
        }
        else
        {
            BuffDebuffType debuff = GetRandomDebuff();
            ApplyDebuffEffect(debuff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Ái chà chà! Rương hóa quái Mimic thè lưỡi ngoạm trúng tay rồi! Ăn trọn bẫy độc: [{GetBuffName(debuff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Á á á cứu trẫm! Rương đểu lừa đảo! Vừa bị cắn ê tay vừa dính debuff!", true);
        }
        yield return new WaitForSeconds(1.4f);
    }

    // --- HOẠT ĐỘNG 1.4: TẾ ĐÀN (SC_27 -> SC_32) ---
    private IEnumerator RunRuneAltarRoutine()
    {
        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Hỡi pháp sư, bước lên đài tế cổ kính đi! Nơi đây lắng nghe tiếng vọng cổ xưa, ban phước hoặc thử thách linh hồn!", false);
        yield return new WaitForSeconds(1.4f);

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Cầu xin thần linh ban phát ánh sáng dẫn lối để thanh tẩy hầm ngục tăm tối này!", true);
        yield return new WaitForSeconds(1.4f);

        bool isBuff = UnityEngine.Random.value < 0.6f;
        if (isBuff)
        {
            BuffDebuffType buff = GetRandomBuff(true);
            ApplyBuffEffect(buff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Trời xanh thấu tỏ lòng thành! Cột sáng thiên giới giáng hạ Lời Chúc Phúc: [{GetBuffName(buff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Cảm tạ đấng tối cao! Năng lượng dâng trào, wave tới ta sẽ quét sạch bóng bầy quỷ!", true);
        }
        else
        {
            BuffDebuffType debuff = GetRandomDebuff();
            ApplyDebuffEffect(debuff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Nguy rồi! Tế đàn bị tà khí xâm chiếm, Lời Nguyền Hắc Ám giáng xuống: [{GetBuffName(debuff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Trời phạt thật rồi sao?! Người ta đi tu đắc đạo còn trẫm khấn vái lại rước họa vào thân!", true);
        }
        yield return new WaitForSeconds(1.4f);
    }

    // --- SỰ KIỆN NHÁNH 2: CÁ CƯỢC ĐẤU THÚ (SC_33 -> SC_40) ---
    private IEnumerator RunBeastBettingRoutine()
    {
        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Ê bạn hiền, bên kia có sới đấu thú ma thuật, cược 10 lõi thử vận may đổi đời không?", false);
        yield return new WaitForSeconds(1.4f);

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Người không chơi là người không bao giờ thắng! Chốt đơn, đặt luôn 10 lõi vào cửa rồng lửa!", true);
        yield return new WaitForSeconds(1.4f);

        if (Player.Instance != null) Player.Instance.TrySpendEnergy(10);

        bool isWin = UnityEngine.Random.value < 0.5f;
        if (isWin)
        {
            BuffDebuffType buff = GetRandomBuff(true);
            ApplyBuffEffect(buff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Hú hét vang dội! Cửa rồng lửa thắng áp đảo! Bạn trúng cược đậm, húp trọn siêu phẩm: [{GetBuffName(buff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Gáy to lên anh em ơi! Tay tổ cờ bạc là đây chứ đâu! Chấp cả họ nhà quái wave sau luôn!", true);
        }
        else
        {
            BuffDebuffType debuff = GetRandomDebuff();
            ApplyDebuffEffect(debuff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Toang rồi ông giáo ơi! Cửa rồng lửa bị lật kèo phút chót! Mất sạch lõi cược lại gánh thêm: [{GetBuffName(debuff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Ủa sao kỳ zợ?! Còn thở là còn gỡ mà gỡ hồi thấy hết thở luôn! Ra đê ở thật rồi!", true);
        }
        yield return new WaitForSeconds(1.4f);
    }

    // --- ĐOẠN THOẠI BÁO ĐỘNG QUỶ KÉO ĐẾN (SC_41 -> SC_44) ---
    private IEnumerator RunMonsterAlertRoutine()
    {
        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Hú còi khẩn cấp! Cảm biến ma năng rung lắc dữ dội, đàn quỷ khát máu đang tràn vào cửa hang kìa!", false);
        yield return new WaitForSeconds(1.4f);

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Đang nghỉ dở mà lũ quái vô duyên cũng không tha! Đã thế trẫm cho các ngươi nếm mùi ma thuật!", true);
        yield return new WaitForSeconds(1.4f);
    }

    public BuffDebuffType GetRandomBuff(bool allowRare = true)
    {
        if (BuffDebuffDatabase.Instance != null)
        {
            var info = BuffDebuffDatabase.Instance.GetRandomBuff(allowRare);
            if (info != null) return info.type;
        }

        return BuffDebuffType.Buff_CuongHoa_Atk;
    }

    public BuffDebuffType GetRandomDebuff()
    {
        if (BuffDebuffDatabase.Instance != null)
        {
            var info = BuffDebuffDatabase.Instance.GetRandomDebuff();
            if (info != null) return info.type;
        }

        return BuffDebuffType.Debuff_GiamAtk;
    }

    public List<BuffDebuffType> GetThreeRollCards()
    {
        if (BuffDebuffDatabase.Instance != null)
        {
            return BuffDebuffDatabase.Instance.GetThreeRollCards();
        }

        return new List<BuffDebuffType>
        {
            BuffDebuffType.Buff_CuongHoa_Atk,
            BuffDebuffType.Buff_CuongHoa_Def,
            BuffDebuffType.Buff_CuongHoa_MaxHP
        };
    }

    public void ApplyBuffEffect(BuffDebuffType buff)
    {
        bool isInstant = IsInstantBuff(buff);
        if (!isInstant)
        {
            _activeBuffsInLevel.Add(buff);
            OnBuffDebuffApplied?.Invoke(buff, true);
        }

        if (Player.Instance != null)
        {
            switch (buff)
            {
                case BuffDebuffType.Buff_CuongHoa_Atk:
                    Player.Instance.ModifyATK(1.1f);
                    break;

                case BuffDebuffType.Buff_CuongHoa_Def:
                    Player.Instance.ModifyDEF(5f);
                    break;

                case BuffDebuffType.Buff_CuongHoa_MaxHP:
                    Player.Instance.ModifyMaxHP(1.1f);
                    break;

                case BuffDebuffType.Buff_CuongHoa_Crit:
                    Player.Instance.ModifyCrit(0.05f, 0.1f);
                    break;

                case BuffDebuffType.Buff_TaiTao_Energy:
                    Player.Instance.AddEnergy(20);
                    if (DamageTextManager.Instance != null)
                    {
                        DamageTextManager.Instance.SpawnCustomText("+20 LÕI NĂNG LƯỢNG", new Color(0.2f, 0.9f, 1f), Player.Instance.transform.position + Vector3.up * 2f, 1.4f);
                    }
                    break;

                case BuffDebuffType.Buff_TaiTao_HealHP:
                    Player.Instance.Heal(Player.Instance.MaxHealth * 0.1f);
                    break;

                case BuffDebuffType.Buff_TaiTao_HealHP20:
                    Player.Instance.Heal(Player.Instance.MaxHealth * 0.2f);
                    break;

                case BuffDebuffType.Buff_Passive_Shield50:
                    Player.Instance.AddShield(Player.Instance.MaxHealth * 0.5f);
                    break;

                case BuffDebuffType.Buff_Passive_Shield25_Reflect:
                    Player.Instance.AddShield(Player.Instance.MaxHealth * 0.25f);
                    break;

                case BuffDebuffType.Buff_LamMoiSkillActive:
                    var activeSkills = FindObjectsByType<SkillActive>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    if (activeSkills != null && activeSkills.Length > 0)
                    {
                        var randSkill = activeSkills[UnityEngine.Random.Range(0, activeSkills.Length)];
                        if (randSkill != null) randSkill.ResetCooldownInstant();
                    }
                    if (DamageTextManager.Instance != null)
                    {
                        DamageTextManager.Instance.SpawnCustomText("LÀM MỚI KỸ NĂNG!", new Color(1f, 0.85f, 0.2f), Player.Instance.transform.position + Vector3.up * 2f, 1.4f);
                    }
                    break;
            }

            PlayerStatsUI statsUI = PlayerStatsUI.Instance != null ? PlayerStatsUI.Instance : FindFirstObjectByType<PlayerStatsUI>();
            if (statsUI != null) statsUI.UpdateStatsUI();
        }
    }

    public void ApplyDebuffEffect(BuffDebuffType debuff)
    {
        _activeDebuffsInLevel.Add(debuff);
        OnBuffDebuffApplied?.Invoke(debuff, false);

        if (Player.Instance != null)
        {
            switch (debuff)
            {
                case BuffDebuffType.Debuff_GiamAtk:
                    Player.Instance.ModifyATK(0.9f);
                    break;

                case BuffDebuffType.Debuff_GiamMaxHP:
                    Player.Instance.ModifyMaxHP(0.9f);
                    break;

                case BuffDebuffType.Debuff_KhoaSkillActive:
                    var activeSkills = FindObjectsByType<SkillActive>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    if (activeSkills != null && activeSkills.Length > 0)
                    {
                        var randSkill = activeSkills[UnityEngine.Random.Range(0, activeSkills.Length)];
                        if (randSkill != null) randSkill.SetLocked(true);
                    }
                    break;
            }

            PlayerStatsUI statsUI = PlayerStatsUI.Instance != null ? PlayerStatsUI.Instance : FindFirstObjectByType<PlayerStatsUI>();
            if (statsUI != null) statsUI.UpdateStatsUI();
        }
    }

    public string GetBuffName(BuffDebuffType b)
    {
        if (BuffDebuffDatabase.Instance != null)
        {
            var info = BuffDebuffDatabase.Instance.Get(b);
            if (info != null && !string.IsNullOrEmpty(info.displayName))
            {
                return info.displayName;
            }
        }

        return b.ToString();
    }
}
