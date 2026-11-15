using System.Collections.Generic;
using UnityEngine;

public enum EnemyRole
{
    DPS_FBS,  // Sát thương chủ lực
    Tank,     // Đỡ đòn (phòng thủ, khiên, phản đòn)
    Support,  // Hỗ trợ (hồi máu, buff, hồi sinh đồng minh)
    Mage      // Pháp sư (khống chế, khóa ngọc, trộm năng lượng)
}

public struct EnemyConfig
{
    public string Name;
    public ElementType Element;
    public EnemyRole Role;
    public EnemyRow DefaultRow;
    public float HP;
    public float ATK;
    public float DEF;
    public float CritRate;
    public float CritDmg;
    public string ActiveSkillName;
    public float SkillMultiplier;
    public int SkillCdRound;
    public string ActiveSkillDesc;
    public string PassiveSkillDesc;
    public float Speed;
    public float AttackCd;
    public float Reflect;
    public float Scale;
    public bool IsBoss;
    public EnemyCombatType CombatType;
    public float AttackRange;
}

public static class EnemyStatsDatabase
{
    private static readonly Dictionary<EnemyType, EnemyConfig> Configs = new Dictionary<EnemyType, EnemyConfig>()
    {
        // ================= HỆ HỎA (7 Quái) =================
        { EnemyType.TinhLinhLua, new EnemyConfig {
            Name = "Tinh Linh Lửa", Element = ElementType.Fire, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 160, ATK = 50, DEF = 50, CritRate = 0.10f, CritDmg = 1.50f,
            ActiveSkillName = "Tia Lửa Đỏ", SkillMultiplier = 1.50f, SkillCdRound = 2,
            ActiveSkillDesc = "Bắn tia lửa gây 150% ATK sát thương",
            PassiveSkillDesc = "Đòn đánh tăng thêm 5% sát thương khi mục tiêu thuộc hệ Mộc",
            Speed = 0.40f, AttackCd = 2f, Scale = 0.85f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.ChoSanDiaNguc, new EnemyConfig {
            Name = "Chó Săn Địa Ngục", Element = ElementType.Fire, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 160, ATK = 50, DEF = 50, CritRate = 0.15f, CritDmg = 1.50f,
            ActiveSkillName = "Cắn Xé Rực Lửa", SkillMultiplier = 1.60f, SkillCdRound = 2,
            ActiveSkillDesc = "Nhảy tới cắn xé gây 160% ATK sát thương",
            PassiveSkillDesc = "Tăng 10% Tỉ lệ chí mạng khi mục tiêu có HP > 50%",
            Speed = 0.55f, AttackCd = 2f, Scale = 0.90f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.HoaQuyNho, new EnemyConfig {
            Name = "Hỏa Quỷ Nhỏ", Element = ElementType.Fire, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 160, ATK = 50, DEF = 50, CritRate = 0.15f, CritDmg = 1.50f,
            ActiveSkillName = "Cầu Lửa Hắc Ám", SkillMultiplier = 1.80f, SkillCdRound = 2,
            ActiveSkillDesc = "Bắn cầu lửa tầm xa gây 180% ATK sát thương",
            PassiveSkillDesc = "Tăng 15% ATK nhưng giảm 20% DEF bản thân",
            Speed = 0.25f, AttackCd = 2f, Scale = 0.70f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.PhuThuyLua, new EnemyConfig {
            Name = "Phù Thủy Lửa", Element = ElementType.Fire, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 160, ATK = 50, DEF = 50, CritRate = 0.20f, CritDmg = 1.50f,
            ActiveSkillName = "Cột Lửa Thiêu Rụi", SkillMultiplier = 2.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Triệu hồi cột lửa thiêu đốt gây 200% ATK sát thương",
            PassiveSkillDesc = "Sát thương kỹ năng tăng thêm 10% nếu mục tiêu đang dính hiệu ứng bất lợi",
            Speed = 0.15f, AttackCd = 2f, Scale = 0.85f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.ChienBinhDungNham, new EnemyConfig {
            Name = "Chiến Binh Dung Nham", Element = ElementType.Fire, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 600, ATK = 100, DEF = 60, CritRate = 0.15f, CritDmg = 1.50f, IsBoss = true,
            ActiveSkillName = "Bổ Búa Dung Nham", SkillMultiplier = 2.20f, SkillCdRound = 2,
            ActiveSkillDesc = "Nện búa lửa gây 220% ATK sát thương + Thiêu đốt rút 2% Max HP/Round trong 2 Round",
            PassiveSkillDesc = "Giảm 15% sát thương nhận vào từ các đòn đánh thường",
            Speed = 0.25f, AttackCd = 2f, Scale = 1.30f, CombatType = EnemyCombatType.Melee, AttackRange = 1.2f
        }},
        { EnemyType.HoaLongNho, new EnemyConfig {
            Name = "Hỏa Long Nhỏ", Element = ElementType.Fire, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 600, ATK = 100, DEF = 60, CritRate = 0.20f, CritDmg = 1.80f, IsBoss = true,
            ActiveSkillName = "Hơi Thở Rồng Lửa", SkillMultiplier = 2.50f, SkillCdRound = 3,
            ActiveSkillDesc = "Phun lửa tầm xa gây 250% ATK sát thương + Nổ diện rộng gây thêm 5% sát thương",
            PassiveSkillDesc = "Tăng 10% ATK cho toàn bộ quái đồng minh hệ Hỏa",
            Speed = 0.40f, AttackCd = 3f, Scale = 1.40f, CombatType = EnemyCombatType.Ranged, AttackRange = 4.0f
        }},
        { EnemyType.HoaThanDietVong, new EnemyConfig {
            Name = "Hỏa Thần Diệt Vong", Element = ElementType.Fire, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 5000, ATK = 240, DEF = 60, CritRate = 0.25f, CritDmg = 2.00f, IsBoss = true,
            ActiveSkillName = "Mưa Sao Băng Tối Thượng", SkillMultiplier = 3.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Triệu hồi mưa thiên thạch gây 300% ATK sát thương toàn sàn + Thiêu đốt rút 3% HP/Round trong 3 Round",
            PassiveSkillDesc = "Đòn đánh thường và kỹ năng tự động phá hủy 50% DEF của Player suốt trận đấu",
            Speed = 0.25f, AttackCd = 2f, Scale = 1.80f, CombatType = EnemyCombatType.Ranged, AttackRange = 4.0f
        }},

        // ================= HỆ KIM (7 Quái) =================
        { EnemyType.GolemThep, new EnemyConfig {
            Name = "Golem Thép", Element = ElementType.Metal, Role = EnemyRole.Tank, DefaultRow = EnemyRow.Front,
            HP = 400, ATK = 30, DEF = 80, CritRate = 0.05f, CritDmg = 1.20f,
            ActiveSkillName = "Pháo Đài Kim Loại", SkillMultiplier = 1.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Kích hoạt giáp thép: Giảm 40% sát thương nhận vào trong 2 Round",
            PassiveSkillDesc = "Miễn nhiễm hoàn toàn với hiệu ứng Choáng",
            Speed = 0.15f, AttackCd = 2f, Scale = 1.10f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.SatThuBongToi, new EnemyConfig {
            Name = "Sát Thủ Bóng Tối", Element = ElementType.Metal, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 160, ATK = 50, DEF = 50, CritRate = 0.25f, CritDmg = 1.80f,
            ActiveSkillName = "Ám Sát Bất Ngờ", SkillMultiplier = 1.80f, SkillCdRound = 2,
            ActiveSkillDesc = "Đột kích chém nhanh gây 180% ATK sát thương, bỏ qua 20% DEF mục tiêu",
            PassiveSkillDesc = "Tăng 15% Tỉ lệ chí mạng khi tấn công Player",
            Speed = 0.40f, AttackCd = 2f, Scale = 0.85f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.KySiSat, new EnemyConfig {
            Name = "Kỵ Sĩ Sắt", Element = ElementType.Metal, Role = EnemyRole.Tank, DefaultRow = EnemyRow.Back,
            HP = 400, ATK = 30, DEF = 80, CritRate = 0.05f, CritDmg = 1.20f,
            ActiveSkillName = "Lá Chắn Máu Ảo", SkillMultiplier = 1.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Tạo lá chắn máu ảo (Shield) bằng 20% Max HP bản thân hấp thụ sát thương trong 2 Round",
            PassiveSkillDesc = "Tăng 10% DEF cho quái đồng minh đứng cạnh ở Hàng trước",
            Speed = 0.25f, AttackCd = 2f, Scale = 1.00f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.CungThuXuyenGiap, new EnemyConfig {
            Name = "Cung Thủ Xuyên Giáp", Element = ElementType.Metal, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 160, ATK = 50, DEF = 50, CritRate = 0.15f, CritDmg = 1.50f,
            ActiveSkillName = "Mũi Tên Xuyên Thấu", SkillMultiplier = 1.70f, SkillCdRound = 2,
            ActiveSkillDesc = "Bắn tên năng lượng tầm xa gây 170% ATK sát thương, bỏ qua 30% giáp của Player",
            PassiveSkillDesc = "Tăng 10% tầm bắn và độ chính xác đòn đánh xa",
            Speed = 0.15f, AttackCd = 2f, Scale = 0.85f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.CoMayXayThit, new EnemyConfig {
            Name = "Cỗ Máy Xay Thịt", Element = ElementType.Metal, Role = EnemyRole.Tank, DefaultRow = EnemyRow.Front,
            HP = 1200, ATK = 40, DEF = 120, CritRate = 0.30f, CritDmg = 2.00f, IsBoss = true,
            ActiveSkillName = "Giáp Gai Phản Pháo", SkillMultiplier = 1.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Kích hoạt khiên phản đòn trong 2 Round: Giảm 30% sát thương nhận vào và phản lại 25% cho Player",
            PassiveSkillDesc = "Đòn đánh và kỹ năng tự động xuyên 50% DEF của Player",
            Speed = 0.15f, AttackCd = 2f, Scale = 1.40f, CombatType = EnemyCombatType.Melee, AttackRange = 1.2f
        }},
        { EnemyType.BacThaySatThu, new EnemyConfig {
            Name = "Bậc Thầy Sát Thủ", Element = ElementType.Metal, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 600, ATK = 100, DEF = 60, CritRate = 0.40f, CritDmg = 2.00f, IsBoss = true,
            ActiveSkillName = "Trảm Huyết Tử Thần", SkillMultiplier = 2.50f, SkillCdRound = 2,
            ActiveSkillDesc = "Chém liên hoàn gây 250% ATK sát thương + Chảy Máu rút 2% Max HP/Round trong 3 Round",
            PassiveSkillDesc = "Khi HP xuống dưới 30%, tăng 30% Tỉ lệ chí mạng",
            Speed = 0.55f, AttackCd = 2f, Scale = 1.20f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.CoMayVinhCuu, new EnemyConfig {
            Name = "Cỗ Máy Vĩnh Cửu", Element = ElementType.Metal, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 5500, ATK = 280, DEF = 100, CritRate = 0.35f, CritDmg = 2.50f, IsBoss = true,
            ActiveSkillName = "Pháo Laser Xuyên Thấu Tối Thượng", SkillMultiplier = 3.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Bắn laser hủy diệt gây 300% ATK sát thương diện rộng, bỏ qua 100% khiên và thủ của Player",
            PassiveSkillDesc = "Tự tạo lá chắn máu ảo bằng 15% Max HP đầu trận; bỏ qua toàn bộ khiên thủ của Player",
            Speed = 0.15f, AttackCd = 2f, Scale = 1.80f, CombatType = EnemyCombatType.Melee, AttackRange = 1.5f
        }},

        // ================= HỆ MỘC (7 Quái) =================
        { EnemyType.QuyRung, new EnemyConfig {
            Name = "Quỷ Rừng", Element = ElementType.Wood, Role = EnemyRole.Support, DefaultRow = EnemyRow.Front,
            HP = 320, ATK = 20, DEF = 70, CritRate = 0.05f, CritDmg = 1.20f,
            ActiveSkillName = "Tiếng Gầm Tự Nhiên", SkillMultiplier = 1.00f, SkillCdRound = 2,
            ActiveSkillDesc = "Hô vang tăng 25% ATK cho toàn bộ quái đồng minh trong 2 Round",
            PassiveSkillDesc = "Tăng 10% tốc độ hành động cho bản thân quái",
            Speed = 0.25f, AttackCd = 2f, Scale = 0.90f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.BoRac, new EnemyConfig {
            Name = "Bọ Rác", Element = ElementType.Wood, Role = EnemyRole.Tank, DefaultRow = EnemyRow.Front,
            HP = 400, ATK = 30, DEF = 80, CritRate = 0.10f, CritDmg = 1.20f,
            ActiveSkillName = "Vỏ Bọc Gai Nhọn", SkillMultiplier = 1.00f, SkillCdRound = 2,
            ActiveSkillDesc = "Tự tạo lớp lá chắn máu ảo hấp thụ sát thương bằng 40% Max HP bản thân trong 2 Round",
            PassiveSkillDesc = "Khi tử trận, hồi phục 5% Max HP cho 1 đồng minh ngẫu nhiên",
            Speed = 0.70f, AttackCd = 2f, Scale = 0.60f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.CungThuAm, new EnemyConfig {
            Name = "Cung Thủ Ám", Element = ElementType.Wood, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Back,
            HP = 160, ATK = 50, DEF = 50, CritRate = 0.10f, CritDmg = 1.30f,
            ActiveSkillName = "Mưa Tên Độc Mộc", SkillMultiplier = 1.60f, SkillCdRound = 2,
            ActiveSkillDesc = "Bắn tên độc tầm xa gây 160% ATK sát thương lên Player",
            PassiveSkillDesc = "Đòn đánh có 15% tỉ lệ gây hiệu ứng độc nhẹ rút 1% HP",
            Speed = 0.15f, AttackCd = 2f, Scale = 0.85f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.TinhLinhHoiMau, new EnemyConfig {
            Name = "Tinh Linh Hồi Máu", Element = ElementType.Wood, Role = EnemyRole.Support, DefaultRow = EnemyRow.Front,
            HP = 320, ATK = 20, DEF = 70, CritRate = 0.05f, CritDmg = 1.20f,
            ActiveSkillName = "Mưa Phước Lành", SkillMultiplier = 1.00f, SkillCdRound = 2,
            ActiveSkillDesc = "Niệm chú hồi phục 15% Max HP cho toàn bộ đồng minh còn sống trên sân",
            PassiveSkillDesc = "Tăng 10% hiệu quả hồi máu của bản thân",
            Speed = 0.15f, AttackCd = 4f, Scale = 0.85f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.ReCayAnThit, new EnemyConfig {
            Name = "Rễ Cây Ăn Thịt", Element = ElementType.Wood, Role = EnemyRole.Support, DefaultRow = EnemyRow.Front,
            HP = 1000, ATK = 10, DEF = 100, CritRate = 0.05f, CritDmg = 1.20f, IsBoss = true,
            ActiveSkillName = "Rễ Mẹ Tái Sinh", SkillMultiplier = 1.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Hồi sinh 1 đồng minh vừa tử trận với 30% Max HP; nếu không có quái chết thì hồi 15% HP cho toàn đội",
            PassiveSkillDesc = "Mỗi đòn đánh hồi máu cho bản thân bằng 10% sát thương gây ra",
            Speed = 0.40f, AttackCd = 2f, Scale = 1.40f, CombatType = EnemyCombatType.Melee, AttackRange = 1.2f
        }},
        { EnemyType.PhapSuBuaChu, new EnemyConfig {
            Name = "Pháp Sư Bùa Chú", Element = ElementType.Wood, Role = EnemyRole.Support, DefaultRow = EnemyRow.Front,
            HP = 1000, ATK = 10, DEF = 100, CritRate = 0.10f, CritDmg = 1.50f, IsBoss = true,
            ActiveSkillName = "Bùa Chú Phục Sinh & Cương Giáp", SkillMultiplier = 1.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Hồi sinh 1 đồng minh tử trận với 40% HP + Buff tăng 30% DEF cho cả đội trong 2 Round",
            PassiveSkillDesc = "Đòn đánh thường gây hiệu ứng DoT rút 2% Max HP Player/Round trong 2 Round",
            Speed = 0.25f, AttackCd = 2f, Scale = 1.30f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.CoThuNganNam, new EnemyConfig {
            Name = "Cổ Thụ Ngàn Năm", Element = ElementType.Wood, Role = EnemyRole.Support, DefaultRow = EnemyRow.Front,
            HP = 8000, ATK = 80, DEF = 80, CritRate = 0.10f, CritDmg = 1.50f, IsBoss = true,
            ActiveSkillName = "Rễ Cây Phục Sinh Tối Thượng", SkillMultiplier = 1.00f, SkillCdRound = 4,
            ActiveSkillDesc = "Hồi sinh toàn bộ đồng minh tử trận với 50% HP + Hồi 20% HP cả đội + Tạo lá chắn bằng 10% Max HP Boss cho cả đội",
            PassiveSkillDesc = "Cứ sau 3 Round tự động triệu hồi 2 Bọ Rác (Hàng trước) để cản đòn bảo vệ bản thân",
            Speed = 0.15f, AttackCd = 2f, Scale = 1.80f, CombatType = EnemyCombatType.Melee, AttackRange = 1.5f
        }},

        // ================= HỆ THỔ (7 Quái) =================
        { EnemyType.SlimeBun, new EnemyConfig {
            Name = "Slime Bùn", Element = ElementType.Earth, Role = EnemyRole.Tank, DefaultRow = EnemyRow.Front,
            HP = 400, ATK = 30, DEF = 80, CritRate = 0.05f, CritDmg = 1.20f,
            ActiveSkillName = "Bọc Bùn Giảm Sát Thương", SkillMultiplier = 1.00f, SkillCdRound = 2,
            ActiveSkillDesc = "Tạo lớp màng bùn bảo vệ: Giảm 35% mọi sát thương nhận vào trong 2 Round",
            PassiveSkillDesc = "Tăng 10% DEF khi máu xuống dưới 50%",
            Speed = 0.15f, AttackCd = 2f, Scale = 0.90f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.LinhDat, new EnemyConfig {
            Name = "Lính Đất", Element = ElementType.Earth, Role = EnemyRole.Tank, DefaultRow = EnemyRow.Front,
            HP = 400, ATK = 30, DEF = 80, CritRate = 0.05f, CritDmg = 1.20f,
            ActiveSkillName = "Lá Chắn Đất Đá", SkillMultiplier = 1.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Tạo lá chắn máu ảo (Shield) bằng 25% Max HP bản thân trong 2 Round",
            PassiveSkillDesc = "Quái đồng minh đứng sau Lính Đất nhận thêm 15% DEF",
            Speed = 0.25f, AttackCd = 2f, Scale = 0.95f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.PhapSuPhongAn, new EnemyConfig {
            Name = "Pháp Sư Phong Ấn", Element = ElementType.Earth, Role = EnemyRole.Mage, DefaultRow = EnemyRow.Back,
            HP = 240, ATK = 40, DEF = 60, CritRate = 0.05f, CritDmg = 1.20f,
            ActiveSkillName = "Phong Ấn Ngọc Đá", SkillMultiplier = 0.60f, SkillCdRound = 2,
            ActiveSkillDesc = "Gây 60% ATK sát thương nhẹ + Khóa 2 viên đá ngẫu nhiên trên bàn Match-3 của Player trong 1 Round",
            PassiveSkillDesc = "Tăng 10% kháng sát thương phép thuật",
            Speed = 0.15f, AttackCd = 2f, Scale = 0.85f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.BoCapDa, new EnemyConfig {
            Name = "Bọ Cạp Đá", Element = ElementType.Earth, Role = EnemyRole.Tank, DefaultRow = EnemyRow.Front,
            HP = 400, ATK = 30, DEF = 80, CritRate = 0.10f, CritDmg = 1.30f,
            ActiveSkillName = "Giáp Gai Độc Thạch", SkillMultiplier = 1.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Kích hoạt giáp gai phản đòn trong 2 Round: Phản lại 20% sát thương nhận vào cho Player",
            PassiveSkillDesc = "Giảm 10% sát thương nhận vào từ các kỹ năng nguyên tố",
            Speed = 0.25f, AttackCd = 2f, Scale = 0.95f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.HatNhamThach, new EnemyConfig {
            Name = "Hạt Nhâm Thạch", Element = ElementType.Earth, Role = EnemyRole.Tank, DefaultRow = EnemyRow.Front,
            HP = 1200, ATK = 40, DEF = 120, CritRate = 0.05f, CritDmg = 1.20f, IsBoss = true, Reflect = 0.10f,
            ActiveSkillName = "Lá Chắn Dung Nham Bùng Nổ", SkillMultiplier = 1.00f, SkillCdRound = 3,
            ActiveSkillDesc = "Tạo lá chắn máu ảo bằng 20% Max HP; khi khiên vỡ phản lại 150% giá trị khiên thành sát thương cho Player",
            PassiveSkillDesc = "Luôn phản lại 10% sát thương nhận vào từ Player",
            Speed = 0.15f, AttackCd = 3f, Scale = 1.50f, CombatType = EnemyCombatType.Melee, AttackRange = 1.3f
        }},
        { EnemyType.PhuThuyKhoaNgoc, new EnemyConfig {
            Name = "Phù Thủy Khóa Ngọc", Element = ElementType.Earth, Role = EnemyRole.Mage, DefaultRow = EnemyRow.Back,
            HP = 800, ATK = 70, DEF = 80, CritRate = 0.10f, CritDmg = 1.30f, IsBoss = true,
            ActiveSkillName = "Đại Phong Ấn Trận Địa", SkillMultiplier = 0.70f, SkillCdRound = 3,
            ActiveSkillDesc = "Gây 70% ATK sát thương + Khóa 4 viên ngọc trên bàn Match-3 trong 2 Round + Giảm 20% ATK Player trong 2 Round",
            PassiveSkillDesc = "Giảm 10% tốc độ tích lũy Energy của Player",
            Speed = 0.25f, AttackCd = 3f, Scale = 1.30f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.BossDatNung, new EnemyConfig {
            Name = "Boss Đất Nung", Element = ElementType.Earth, Role = EnemyRole.Tank, DefaultRow = EnemyRow.Front,
            HP = 9000, ATK = 90, DEF = 100, CritRate = 0.10f, CritDmg = 1.50f, IsBoss = true,
            ActiveSkillName = "Đại Địa Chấn Hủy Diệt", SkillMultiplier = 2.00f, SkillCdRound = 4,
            ActiveSkillDesc = "Tạo cơn địa chấn gây 200% ATK sát thương + Làm Choáng Player 1 Round + Khóa 5 ô ngọc trên bàn Match-3",
            PassiveSkillDesc = "Đầu mỗi Round tự tạo lá chắn máu ảo bằng 10% Max HP Boss; phản lại 10% sát thương nhận vào",
            Speed = 0.15f, AttackCd = 2f, Scale = 1.80f, CombatType = EnemyCombatType.Melee, AttackRange = 1.5f
        }},

        // ================= HỆ THỦY (7 Quái) =================
        { EnemyType.SlimeNuoc, new EnemyConfig {
            Name = "Slime Nước", Element = ElementType.Water, Role = EnemyRole.Mage, DefaultRow = EnemyRow.Front,
            HP = 240, ATK = 40, DEF = 60, CritRate = 0.05f, CritDmg = 1.20f,
            ActiveSkillName = "Tia Nước Làm Chậm", SkillMultiplier = 0.60f, SkillCdRound = 2,
            ActiveSkillDesc = "Gây 60% ATK sát thương + Tăng chi phí cast skill của Player thêm 2 Energy (từ 5 lên 7) trong 1 Round",
            PassiveSkillDesc = "Tăng 10% né tránh đòn đánh thường",
            Speed = 0.40f, AttackCd = 2f, Scale = 0.85f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.ThuyQuaiNho, new EnemyConfig {
            Name = "Thủy Quái Nhỏ", Element = ElementType.Water, Role = EnemyRole.DPS_FBS, DefaultRow = EnemyRow.Front,
            HP = 160, ATK = 50, DEF = 50, CritRate = 0.10f, CritDmg = 1.40f,
            ActiveSkillName = "Cắn Xoáy Thủy Triều", SkillMultiplier = 1.60f, SkillCdRound = 2,
            ActiveSkillDesc = "Lao tới cắn xoáy mạnh mẽ gây 160% ATK sát thương",
            PassiveSkillDesc = "Giảm 10% sát thương vật lý nhận vào",
            Speed = 0.25f, AttackCd = 2f, Scale = 0.90f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.KeTromKyNang, new EnemyConfig {
            Name = "Kẻ Trộm Kỹ Năng", Element = ElementType.Water, Role = EnemyRole.Mage, DefaultRow = EnemyRow.Back,
            HP = 240, ATK = 40, DEF = 60, CritRate = 0.10f, CritDmg = 1.30f,
            ActiveSkillName = "Cướp Đoạt Nội Năng", SkillMultiplier = 0.50f, SkillCdRound = 3,
            ActiveSkillDesc = "Gây 50% ATK sát thương + Hút 5 Energy của Player chuyển hóa thành máu hồi phục cho bản thân",
            PassiveSkillDesc = "Có 15% tỉ lệ né tránh hoàn toàn đòn đánh của Player",
            Speed = 0.55f, AttackCd = 5f, Scale = 0.85f, CombatType = EnemyCombatType.Melee, AttackRange = 0.85f
        }},
        { EnemyType.PhapSuBang, new EnemyConfig {
            Name = "Pháp Sư Băng", Element = ElementType.Water, Role = EnemyRole.Mage, DefaultRow = EnemyRow.Back,
            HP = 240, ATK = 40, DEF = 60, CritRate = 0.10f, CritDmg = 1.40f,
            ActiveSkillName = "Mũi Băng Hàn Giá", SkillMultiplier = 0.70f, SkillCdRound = 2,
            ActiveSkillDesc = "Bắn mũi tên băng gây 70% ATK sát thương + Đóng băng 2 viên ngọc ngẫu nhiên trên bàn Match-3 trong 2 Round",
            PassiveSkillDesc = "Tăng 10% sát thương khi mục tiêu dính hiệu ứng Băng",
            Speed = 0.15f, AttackCd = 2f, Scale = 0.85f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.QuaiVatDamLay, new EnemyConfig {
            Name = "Quái Vật Đầm Lầy", Element = ElementType.Water, Role = EnemyRole.Mage, DefaultRow = EnemyRow.Back,
            HP = 800, ATK = 70, DEF = 80, CritRate = 0.15f, CritDmg = 1.50f, IsBoss = true,
            ActiveSkillName = "Đầm Lầy Ăn Mòn", SkillMultiplier = 0.80f, SkillCdRound = 3,
            ActiveSkillDesc = "Phun bùn lầy diện rộng gây 80% ATK sát thương + Giảm 30% DEF Player trong 2 Round + Giảm 10 Energy khởi đầu của Player",
            PassiveSkillDesc = "Khi HP dưới 50%, tự hồi phục 5% Max HP mỗi Round",
            Speed = 0.25f, AttackCd = 3f, Scale = 1.30f, CombatType = EnemyCombatType.Melee, AttackRange = 1.2f
        }},
        { EnemyType.TinhLinhBangGia, new EnemyConfig {
            Name = "Tinh Linh Băng Giá", Element = ElementType.Water, Role = EnemyRole.Mage, DefaultRow = EnemyRow.Back,
            HP = 800, ATK = 70, DEF = 80, CritRate = 0.20f, CritDmg = 1.80f, IsBoss = true,
            ActiveSkillName = "Đóng Băng Hàng Ngũ", SkillMultiplier = 0.80f, SkillCdRound = 3,
            ActiveSkillDesc = "Niệm phép băng cực hàn gây 80% ATK sát thương + Đóng băng toàn bộ hàng đá dưới cùng của bàn Match-3 trong 2 Round",
            PassiveSkillDesc = "Giảm 15% sát thương nhận vào từ các kỹ năng hệ Hỏa",
            Speed = 0.40f, AttackCd = 3f, Scale = 1.30f, CombatType = EnemyCombatType.Ranged, AttackRange = 3.8f
        }},
        { EnemyType.ThuyThanSaNga, new EnemyConfig {
            Name = "Thủy Thần Sa Ngã", Element = ElementType.Water, Role = EnemyRole.Mage, DefaultRow = EnemyRow.Back,
            HP = 6000, ATK = 100, DEF = 50, CritRate = 0.25f, CritDmg = 1.80f, IsBoss = true,
            ActiveSkillName = "Sóng Thần Diệt Tuyệt Tối Thượng", SkillMultiplier = 2.20f, SkillCdRound = 3,
            ActiveSkillDesc = "Tạo sóng thần quét sạch sàn đấu gây 220% ATK sát thương + Choáng Player 1 Round + Triệt tiêu toàn bộ Energy về 0",
            PassiveSkillDesc = "Đòn đánh thường tự động đánh cắp 5 Energy của Player. Giảm 20% sát thương nhận vào khi còn quái hệ Thủy",
            Speed = 0.25f, AttackCd = 2f, Scale = 1.80f, CombatType = EnemyCombatType.Ranged, AttackRange = 4.0f
        }}
    };

    public static IReadOnlyDictionary<EnemyType, EnemyConfig> HardcodedConfigs => Configs;

    public static EnemyConfig GetConfig(EnemyType type)
    {
        // 1. Ưu tiên tải từ ScriptableObject asset riêng lẻ của từng quái (GDD Individual SO)
        EnemyDataSO individualSO = EnemyDataSO.Get(type);
        if (individualSO != null)
        {
            return individualSO.ToEnemyConfig();
        }

        // 2. Ưu tiên tải từ Database SO
        if (EnemyDatabaseSO.Instance != null && EnemyDatabaseSO.Instance.TryGetConfig(type, out var soConfig))
        {
            return soConfig;
        }

        // 3. Fallback về từ điển C# tĩnh nếu asset chưa được tạo
        if (Configs.TryGetValue(type, out var config)) return config;

        return new EnemyConfig
        {
            Name = "Quái Thường",
            Element = ElementType.Wood,
            Role = EnemyRole.DPS_FBS,
            DefaultRow = EnemyRow.Front,
            HP = 160,
            ATK = 50,
            DEF = 50,
            CritRate = 0.1f,
            CritDmg = 1.5f,
            ActiveSkillName = "Đòn Đánh Năng Lượng",
            SkillMultiplier = 1.5f,
            SkillCdRound = 2,
            Speed = 0.25f,
            AttackCd = 2f,
            Scale = 0.85f,
            CombatType = EnemyCombatType.Melee,
            AttackRange = 0.85f
        };
    }

    /// <summary>
    /// Tính toán hệ số nhân độ khó của Level theo GDD:
    /// Dễ = 1.2, Trung Bình (Thường) = 1.35, Khó / Cực Khó = 1.5
    /// </summary>
    public static float GetDifficultyMultiplier(StepDifficulty difficulty)
    {
        return difficulty switch
        {
            StepDifficulty.Easy => 1.20f,
            StepDifficulty.Normal => 1.35f,
            StepDifficulty.Hard => 1.50f,
            StepDifficulty.VeryHard => 1.50f,
            _ => 1.20f
        };
    }

    /// <summary>
    /// Công thức tự động tăng chỉ số quái theo độ khó và cấp độ Level từ GDD:
    /// Chỉ số = Chỉ số gốc * (Hệ số ^ n) với n = (Level - 1) / 15.0
    /// </summary>
    public static EnemyConfig GetScaledConfig(EnemyType type, int level, StepDifficulty difficulty)
    {
        EnemyConfig baseConfig = GetConfig(type);
        float mult = GetDifficultyMultiplier(difficulty);
        float n = Mathf.Max(0f, (level - 1) / 15.0f);
        float scaleFactor = Mathf.Pow(mult, n);

        baseConfig.HP = Mathf.Round(baseConfig.HP * scaleFactor);
        baseConfig.ATK = Mathf.Round(baseConfig.ATK * scaleFactor);
        baseConfig.DEF = Mathf.Round(baseConfig.DEF * scaleFactor);

        return baseConfig;
    }

    public static List<EnemyType> GetEnemiesByElement(ElementType element)
    {
        if (EnemyDatabaseSO.Instance != null && EnemyDatabaseSO.Instance.Enemies.Count > 0)
        {
            return EnemyDatabaseSO.Instance.GetEnemiesByElement(element);
        }

        List<EnemyType> list = new List<EnemyType>();
        foreach (var kvp in Configs)
        {
            if (kvp.Value.Element == element)
            {
                list.Add(kvp.Key);
            }
        }
        return list;
    }

    public static ElementType GetEnemyElement(EnemyType type)
    {
        if (EnemyDatabaseSO.Instance != null && EnemyDatabaseSO.Instance.TryGetConfig(type, out var soConfig))
        {
            return soConfig.Element;
        }

        if (Configs.TryGetValue(type, out var config))
        {
            return config.Element;
        }
        return ElementType.Wood;
    }
}
