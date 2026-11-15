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

    // Danh sách Buff và Debuff tồn tại xuyên suốt toàn bộ Level
    private readonly List<BuffDebuffType> _activeBuffsInLevel = new List<BuffDebuffType>();
    private readonly List<BuffDebuffType> _activeDebuffsInLevel = new List<BuffDebuffType>();

    public IReadOnlyList<BuffDebuffType> ActiveBuffsInLevel => _activeBuffsInLevel;
    public IReadOnlyList<BuffDebuffType> ActiveDebuffsInLevel => _activeDebuffsInLevel;

    public void ResetLevelBuffsDebuffs()
    {
        _activeBuffsInLevel.Clear();
        _activeDebuffsInLevel.Clear();
        OnBuffDebuffReset?.Invoke();
    }

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }
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
            // 50% Phiến đá cổ, 50% Cá cược theo bảng GDD
            eventType = (UnityEngine.Random.value < 0.5f) 
                ? StepEventType.RuneStone_Gacha 
                : StepEventType.BeastBetting;
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
        // Trừ 25 lõi năng lượng mở phiến đá
        if (Player.Instance != null) Player.Instance.TrySpendEnergy(25);

        // Đẩy thoại mở đầu
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
            BuffDebuffType buff = GetRandomBuff();
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

        bool isBuff = UnityEngine.Random.value < 0.75f; // Tỉ lệ roll ra buff cao hơn
        if (isBuff)
        {
            BuffDebuffType buff = GetRandomBuff();
            ApplyBuffEffect(buff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Lật bài thần thánh! Ra ngay thẻ bài SSR lấp lánh: bạn nhận được [{GetBuffName(buff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Haha đúng là tay thơm có số! Thẻ xịn thế này wave sau quái chỉ có nước khóc thét!", true);
        }
        else
        {
            BuffDebuffType debuff = GetRandomDebuff();
            ApplyDebuffEffect(debuff);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Ối dồi ôi bài rác dính bẫy rồi! Rút nhầm thẻ Nguyền Rủa cổ đại: [{GetBuffName(debuff)}]!", false);
            yield return new WaitForSeconds(1.4f);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Trời ơi đất hỡi! Tưởng bớ được buff hiếm ai dè bốc trúng cục nợ cay xé lòng!", true);
        }
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
            BuffDebuffType buff = GetRandomBuff();
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
            BuffDebuffType buff = GetRandomBuff();
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
            BuffDebuffType buff = GetRandomBuff();
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

    public BuffDebuffType GetRandomBuff()
    {
        if (BuffDebuffDatabase.Instance != null)
        {
            var info = BuffDebuffDatabase.Instance.GetRandomBuff(true);
            if (info != null) return info.type;
        }

        bool isRare = UnityEngine.Random.value < 0.05f; // 5% Buff Hiếm
        if (isRare)
        {
            BuffDebuffType[] rareBuffs = new BuffDebuffType[] {
                BuffDebuffType.Buff_Hiem_StackAtkCrit,
                BuffDebuffType.Buff_Hiem_DoubleNextWave,
                BuffDebuffType.Buff_Hiem_DmgToHeal,
                BuffDebuffType.Buff_Hiem_VampireDouble
            };
            return rareBuffs[UnityEngine.Random.Range(0, rareBuffs.Length)];
        }
        else
        {
            BuffDebuffType[] normalBuffs = new BuffDebuffType[] {
                BuffDebuffType.Buff_CuongHoa_Atk,
                BuffDebuffType.Buff_CuongHoa_Def,
                BuffDebuffType.Buff_CuongHoa_MaxHP,
                BuffDebuffType.Buff_CuongHoa_Crit,
                BuffDebuffType.Buff_TaiTao_Energy,
                BuffDebuffType.Buff_TaiTao_HealHP,
                BuffDebuffType.Buff_Passive_Shield50,
                BuffDebuffType.Buff_Passive_DoubleSkill,
                BuffDebuffType.Buff_Passive_Reflect100,
                BuffDebuffType.Buff_LamMoiSkillActive
            };
            return normalBuffs[UnityEngine.Random.Range(0, normalBuffs.Length)];
        }
    }

    public BuffDebuffType GetRandomDebuff()
    {
        if (BuffDebuffDatabase.Instance != null)
        {
            var info = BuffDebuffDatabase.Instance.GetRandomDebuff();
            if (info != null) return info.type;
        }

        BuffDebuffType[] debuffs = new BuffDebuffType[] {
            BuffDebuffType.Debuff_GiamAtk,
            BuffDebuffType.Debuff_GiamMaxHP,
            BuffDebuffType.Debuff_TangHieuUngQuai,
            BuffDebuffType.Debuff_MatLoiKhiCrit,
            BuffDebuffType.Debuff_KhoaSkillActive,
            BuffDebuffType.Debuff_XoaPassive
        };
        return debuffs[UnityEngine.Random.Range(0, debuffs.Length)];
    }

    public void ApplyBuffEffect(BuffDebuffType buff)
    {
        _activeBuffsInLevel.Add(buff);
        OnBuffDebuffApplied?.Invoke(buff, true);

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
                    break;

                case BuffDebuffType.Buff_TaiTao_HealHP:
                    Player.Instance.Heal(Player.Instance.MaxHealth * 0.1f);
                    break;
            }

            PlayerStatsUI statsUI = FindFirstObjectByType<PlayerStatsUI>();
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

                case BuffDebuffType.Debuff_MatLoiKhiCrit:
                    Player.Instance.TrySpendEnergy(5);
                    break;
            }

            PlayerStatsUI statsUI = FindFirstObjectByType<PlayerStatsUI>();
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

        return b switch
        {
            BuffDebuffType.Buff_CuongHoa_Atk => "+10% Sát Thương (Toàn Level)",
            BuffDebuffType.Buff_CuongHoa_Def => "+20% Giáp (Toàn Level)",
            BuffDebuffType.Buff_CuongHoa_MaxHP => "+10% Giới Hạn Máu (Toàn Level)",
            BuffDebuffType.Buff_CuongHoa_Crit => "+5% Tỉ Lệ CM & +10% ST CM",
            BuffDebuffType.Buff_TaiTao_Energy => "+20 Lõi Năng Lượng",
            BuffDebuffType.Buff_TaiTao_HealHP => "Hồi Phục 10% Máu",
            BuffDebuffType.Buff_Passive_Shield50 => "Lá Chắn Hộ Thể 50% HP (Toàn Level)",
            BuffDebuffType.Buff_Passive_Shield25_Reflect => "Lá Chắn 25% HP & Phản 50% ST",
            BuffDebuffType.Buff_Passive_Reflect100 => "50% Phản 100% Sát Thương",
            BuffDebuffType.Buff_Passive_DoubleSkill => "Nhân Đôi Kỹ Năng Active",
            BuffDebuffType.Buff_Passive_DoubleStoneEffect => "x2 Hiệu Quả Kỹ Năng Đá",
            BuffDebuffType.Buff_LamMoiSkillActive => "Làm Mới 1 Skill Active",
            BuffDebuffType.Buff_Hiem_StackAtkCrit => "BUFF HIẾM: Tăng Tiến ATK & Chí Mạng",
            BuffDebuffType.Buff_Hiem_DoubleNextWave => "BUFF HIẾM: Tích Lũy & x2 Buff Wave Sau",
            BuffDebuffType.Buff_Hiem_DmgToHeal => "BUFF HIẾM: Chuyển 50% ST Thành Hồi Máu",
            BuffDebuffType.Buff_Hiem_VampireDouble => "BUFF HIẾM: Huyết Kỹ Cuồng Nộ",
            BuffDebuffType.Debuff_GiamAtk => "DEBUFF: Giảm 10% Sát Thương",
            BuffDebuffType.Debuff_GiamMaxHP => "DEBUFF: Giảm 10% Giới Hạn Máu",
            BuffDebuffType.Debuff_TangHieuUngQuai => "DEBUFF: Tăng Hiệu Ứng Bất Lợi Của Quái",
            BuffDebuffType.Debuff_MatLoiKhiCrit => "DEBUFF: Mất 5% Lõi Năng Lượng Khi Chí Mạng",
            BuffDebuffType.Debuff_KhoaSkillActive => "DEBUFF: Khóa 1 Kỹ Năng Active",
            BuffDebuffType.Debuff_XoaPassive => "DEBUFF: Xóa 1 Kỹ Năng Passive",
            _ => "Hiệu Ứng Nguyên Tố"
        };
    }
}