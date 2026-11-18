using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GenerateIndividualEnemyDataAssets
{
    private struct EnemyTemplate
    {
        public EnemyType Type;
        public string Name;
        public ElementType Element;
        public EnemyCategory Category;
        public EnemyRole Role;
        public EnemyRow Row;
        public float HP;
        public float ATK;
        public float DEF;
        public float CritRate;
        public float CritDmg;
        public string SkillName;
        public float SkillMult;
        public int SkillCd;
        public string SkillDesc;
        public string PassiveDesc;
        public string NormalDesc;
        public float Speed;
        public float AttackCd;
        public float Reflect;
        public float Scale;
        public bool IsBoss;
        public EnemyCombatType CombatType;
        public EnemySkillActionType NormalAction;
        public EnemySkillActionType SkillAction;
        public float Range;
        public string Folder;
    }

    [MenuItem("Tools/Database/Generate 35 Individual Enemy Data Assets")]
    public static void Generate35Assets()
    {
        string rootDir = "Assets/Project/Resources/ScriptObject/Character/Enemies";
        EnsureFolderExists(rootDir);
        EnsureFolderExists($"{rootDir}/Fire");
        EnsureFolderExists($"{rootDir}/Metal");
        EnsureFolderExists($"{rootDir}/Wood");
        EnsureFolderExists($"{rootDir}/Earth");
        EnsureFolderExists($"{rootDir}/Water");

        var templates = GetTemplates();
        int createdCount = 0;

        EnemyBase defaultEnemyPrefab = AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/Project/Resources/Prefab/Base/Enemy/Enemy.prefab");

        foreach (var t in templates)
        {
            string assetPath = $"{rootDir}/{t.Folder}/Enemy_{t.Type}.asset";
            EnemyDataSO so = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(assetPath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<EnemyDataSO>();
                AssetDatabase.CreateAsset(so, assetPath);
            }

            so.enemyPrefab = defaultEnemyPrefab;
            so.enemyType = t.Type;
            so.enemyName = t.Name;
            so.element = t.Element;
            so.category = t.Category;
            so.role = t.Role;
            so.defaultRow = t.Row;
            so.maxHP = t.HP;
            so.atk = t.ATK;
            so.def = t.DEF;
            so.critRate = t.CritRate;
            so.critDmg = t.CritDmg;
            so.speed = t.Speed;
            so.attackCd = t.AttackCd;
            so.reflect = t.Reflect;
            so.scale = t.Scale;
            so.isBoss = t.IsBoss || (t.Category != EnemyCategory.Normal);
            so.combatType = t.CombatType;
            so.normalAttackActionType = t.NormalAction;
            so.activeSkillActionType = t.SkillAction;
            so.attackRange = t.Range;
            so.activeSkillName = t.SkillName;
            so.skillMultiplier = t.SkillMult;
            so.skillCdRound = t.SkillCd;
            so.activeSkillDesc = t.SkillDesc;
            so.passiveSkillDesc = t.PassiveDesc;
            so.normalAttackDesc = t.NormalDesc;

            EditorUtility.SetDirty(so);
            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=#00ff88><b>[ENEMY ASSET GENERATOR]</b> Successfully created/updated {createdCount} individual EnemyDataSO assets in Resources/ScriptObject/Character/Enemies/</color>");
    }

    private static List<EnemyTemplate> GetTemplates()
    {
        return new List<EnemyTemplate>
        {
            // ================= 1. HỆ HỎA (FIRE) =================
            new EnemyTemplate {
                Type = EnemyType.TinhLinhLua, Name = "Tinh Linh Lửa", Element = ElementType.Fire, Category = EnemyCategory.Normal,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 160, ATK = 50, DEF = 50, CritRate = 0.10f, CritDmg = 1.50f,
                SkillName = "Tia Lửa Đỏ", SkillMult = 1.50f, SkillCd = 2,
                SkillDesc = "Bắn tia lửa gây 150% ATK sát thương (24 ST)",
                PassiveDesc = "Đòn đánh tăng thêm 5% sát thương khi mục tiêu thuộc hệ Mộc",
                NormalDesc = "Đòn đánh cơ bản gây 100% ATK (16 ST)",
                Speed = 0.40f, AttackCd = 2f, Reflect = 0f, Scale = 0.85f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 3.8f, Folder = "Fire"
            },
            new EnemyTemplate {
                Type = EnemyType.ChoSanDiaNguc, Name = "Chó Săn Địa Ngục", Element = ElementType.Fire, Category = EnemyCategory.Normal,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 160, ATK = 50, DEF = 50, CritRate = 0.15f, CritDmg = 1.50f,
                SkillName = "Cắn Xé Rực Lửa", SkillMult = 1.60f, SkillCd = 2,
                SkillDesc = "Nhảy tới cắn xé gây 160% ATK sát thương (38 ST)",
                PassiveDesc = "Tăng 10% Tỉ lệ chí mạng khi mục tiêu có HP > 50%",
                NormalDesc = "Cào cắn cơ bản gây 100% ATK (24 ST)",
                Speed = 0.55f, AttackCd = 2f, Reflect = 0f, Scale = 0.90f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.MeleeDash, Range = 0.85f, Folder = "Fire"
            },
            new EnemyTemplate {
                Type = EnemyType.HoaQuyNho, Name = "Hỏa Quỷ Nhỏ", Element = ElementType.Fire, Category = EnemyCategory.Normal,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 160, ATK = 50, DEF = 50, CritRate = 0.15f, CritDmg = 1.50f,
                SkillName = "Cầu Lửa Hắc Ám", SkillMult = 1.80f, SkillCd = 2,
                SkillDesc = "Bắn cầu lửa tầm xa gây 180% ATK sát thương (54 ST)",
                PassiveDesc = "Tăng 15% ATK nhưng giảm 20% DEF bản thân",
                NormalDesc = "Bắn tia nhiệt gây 100% ATK (30 ST)",
                Speed = 0.25f, AttackCd = 2f, Reflect = 0f, Scale = 0.70f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 3.8f, Folder = "Fire"
            },
            new EnemyTemplate {
                Type = EnemyType.PhuThuyLua, Name = "Phù Thủy Lửa", Element = ElementType.Fire, Category = EnemyCategory.Normal,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 160, ATK = 50, DEF = 50, CritRate = 0.20f, CritDmg = 1.50f,
                SkillName = "Cột Lửa Thiêu Rụi", SkillMult = 2.00f, SkillCd = 3,
                SkillDesc = "Triệu hồi cột lửa thiêu đốt gây 200% ATK sát thương (72 ST)",
                PassiveDesc = "Sát thương kỹ năng tăng thêm 10% nếu mục tiêu đang dính hiệu ứng bất lợi",
                NormalDesc = "Bắn cầu ma pháp gây 100% ATK (36 ST)",
                Speed = 0.30f, AttackCd = 2f, Reflect = 0f, Scale = 0.85f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 3.8f, Folder = "Fire"
            },
            new EnemyTemplate {
                Type = EnemyType.ChienBinhDungNham, Name = "Chiến Binh Dung Nham", Element = ElementType.Fire, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 600, ATK = 100, DEF = 60, CritRate = 0.15f, CritDmg = 1.50f,
                SkillName = "Bổ Búa Dung Nham", SkillMult = 2.20f, SkillCd = 2,
                SkillDesc = "Nện búa lửa gây 220% ATK sát thương (220 ST) + Thiêu đốt rút 2% Max HP Player/Round trong 2 Round",
                PassiveDesc = "Giảm 15% sát thương nhận vào từ các đòn đánh thường",
                NormalDesc = "Bổ búa rực lửa gây 100% ATK (100 ST)",
                Speed = 0.45f, AttackCd = 2f, Reflect = 0f, Scale = 1.20f, IsBoss = true, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.MeleeDash, Range = 0.95f, Folder = "Fire"
            },
            new EnemyTemplate {
                Type = EnemyType.HoaLongNho, Name = "Hỏa Long Nhỏ", Element = ElementType.Fire, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 600, ATK = 100, DEF = 60, CritRate = 0.20f, CritDmg = 1.80f,
                SkillName = "Hơi Thở Rồng Lửa", SkillMult = 2.50f, SkillCd = 3,
                SkillDesc = "Phun lửa tầm xa gây 250% ATK sát thương (300 ST) + Nổ diện rộng gây thêm 5% sát thương hỏa",
                PassiveDesc = "Tăng 10% ATK cho toàn bộ quái đồng minh hệ Hỏa",
                NormalDesc = "Khạc lửa cơ bản gây 100% ATK (120 ST)",
                Speed = 0.35f, AttackCd = 2f, Reflect = 0f, Scale = 1.30f, IsBoss = true, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 4.0f, Folder = "Fire"
            },
            new EnemyTemplate {
                Type = EnemyType.HoaThanDietVong, Name = "Hỏa Thần Diệt Vong", Element = ElementType.Fire, Category = EnemyCategory.FinalBoss,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 5000, ATK = 240, DEF = 60, CritRate = 0.25f, CritDmg = 2.00f,
                SkillName = "Mưa Sao Băng Tối Thượng", SkillMult = 3.00f, SkillCd = 3,
                SkillDesc = "Triệu hồi mưa thiên thạch lửa xanh lam gây 300% ATK sát thương toàn sàn (720 ST) + Thiêu đốt rút 3% Max HP Player/Round trong 3 Round",
                PassiveDesc = "[Thiêu Rụi Giáp] Đòn đánh thường và kỹ năng tự động phá hủy 50% DEF của Player suốt trận đấu",
                NormalDesc = "Trảm diệt vong tầm xa gây 100% ATK (240 ST)",
                Speed = 0.20f, AttackCd = 2f, Reflect = 0f, Scale = 1.80f, IsBoss = true, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 4.0f, Folder = "Fire"
            },

            // ================= 2. HỆ KIM (METAL) =================
            new EnemyTemplate {
                Type = EnemyType.GolemThep, Name = "Golem Thép", Element = ElementType.Metal, Category = EnemyCategory.Normal,
                Role = EnemyRole.Tank, Row = EnemyRow.Front, HP = 400, ATK = 30, DEF = 80, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Pháo Đài Kim Loại", SkillMult = 1.20f, SkillCd = 3,
                SkillDesc = "Kích hoạt giáp thép: Tạo khiên giảm 40% sát thương nhận vào trong 2 Round",
                PassiveDesc = "Miễn nhiễm hoàn toàn với hiệu ứng Choáng",
                NormalDesc = "Đấm thép nặng nề gây 100% ATK (24 ST)",
                Speed = 0.15f, AttackCd = 2f, Reflect = 0f, Scale = 1.00f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.85f, Folder = "Metal"
            },
            new EnemyTemplate {
                Type = EnemyType.SatThuBongToi, Name = "Sát Thủ Bóng Tối", Element = ElementType.Metal, Category = EnemyCategory.Normal,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 160, ATK = 50, DEF = 50, CritRate = 0.25f, CritDmg = 1.80f,
                SkillName = "Ám Sát Bất Ngờ", SkillMult = 1.80f, SkillCd = 2,
                SkillDesc = "Đột kích chém nhanh gây 180% ATK sát thương (54 ST), bỏ qua 20% DEF của mục tiêu",
                PassiveDesc = "Tăng 15% Tỉ lệ chí mạng khi tấn công Player",
                NormalDesc = "Chém song kiếm gây 100% ATK (30 ST)",
                Speed = 0.60f, AttackCd = 2f, Reflect = 0f, Scale = 0.80f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.MeleeDash, Range = 0.85f, Folder = "Metal"
            },
            new EnemyTemplate {
                Type = EnemyType.KySiSat, Name = "Kỵ Sĩ Sắt", Element = ElementType.Metal, Category = EnemyCategory.Normal,
                Role = EnemyRole.Tank, Row = EnemyRow.Back, HP = 400, ATK = 30, DEF = 80, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Lá Chắn Máu Ảo", SkillMult = 1.20f, SkillCd = 3,
                SkillDesc = "Tạo lá chắn máu ảo (Shield) bằng 20% Max HP bản thân (36 Shield) hấp thụ sát thương trong 2 Round",
                PassiveDesc = "Tăng 10% DEF cho quái đồng minh đứng cạnh ở Hàng trước",
                NormalDesc = "Đâm thương sắt gây 100% ATK (20 ST)",
                Speed = 0.35f, AttackCd = 2f, Reflect = 0f, Scale = 0.95f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.85f, Folder = "Metal"
            },
            new EnemyTemplate {
                Type = EnemyType.CungThuXuyenGiap, Name = "Cung Thủ Xuyên Giáp", Element = ElementType.Metal, Category = EnemyCategory.Normal,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 160, ATK = 50, DEF = 50, CritRate = 0.15f, CritDmg = 1.50f,
                SkillName = "Mũi Tên Xuyên Thấu", SkillMult = 1.70f, SkillCd = 2,
                SkillDesc = "Bắn tên năng lượng tầm xa gây 170% ATK sát thương (48 ST), bỏ qua 30% giáp của Player",
                PassiveDesc = "Tăng 10% tầm bắn và độ chính xác đòn đánh xa",
                NormalDesc = "Bắn tên thép gây 100% ATK (28 ST)",
                Speed = 0.30f, AttackCd = 2f, Reflect = 0f, Scale = 0.85f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 4.2f, Folder = "Metal"
            },
            new EnemyTemplate {
                Type = EnemyType.CoMayXayThit, Name = "Cỗ Máy Xay Thịt", Element = ElementType.Metal, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.Tank, Row = EnemyRow.Front, HP = 1200, ATK = 40, DEF = 120, CritRate = 0.30f, CritDmg = 2.00f,
                SkillName = "Giáp Gai Phản Pháo", SkillMult = 1.50f, SkillCd = 3,
                SkillDesc = "Kích hoạt khiên phản đòn trong 2 Round: Giảm 30% sát thương nhận vào và phản lại 25% sát thương nhận vào cho Player",
                PassiveDesc = "Đòn đánh và kỹ năng tự động xuyên 50% DEF của Player",
                NormalDesc = "Cưa xích máy xay càn quét gây 100% ATK (110 ST)",
                Speed = 0.40f, AttackCd = 2f, Reflect = 0.25f, Scale = 1.35f, IsBoss = true, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.95f, Folder = "Metal"
            },
            new EnemyTemplate {
                Type = EnemyType.BacThaySatThu, Name = "Bậc Thầy Sát Thủ", Element = ElementType.Metal, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 600, ATK = 100, DEF = 60, CritRate = 0.40f, CritDmg = 2.00f,
                SkillName = "Trảm Huyết Tử Thần", SkillMult = 2.50f, SkillCd = 2,
                SkillDesc = "Chém liên hoàn gây 250% ATK sát thương (325 ST) + Hiệu ứng Chảy Máu rút 2% Max HP Player/Round trong 3 Round",
                PassiveDesc = "Khi HP xuống dưới 30%, tăng 30% Tỉ lệ chí mạng",
                NormalDesc = "Phi dao găm độc gây 100% ATK (130 ST)",
                Speed = 0.70f, AttackCd = 2f, Reflect = 0f, Scale = 1.10f, IsBoss = true, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.MeleeDash, Range = 0.90f, Folder = "Metal"
            },
            new EnemyTemplate {
                Type = EnemyType.CoMayVinhCuu, Name = "Cỗ Máy Vĩnh Cửu", Element = ElementType.Metal, Category = EnemyCategory.FinalBoss,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 5500, ATK = 280, DEF = 100, CritRate = 0.35f, CritDmg = 2.50f,
                SkillName = "Pháo Laser Xuyên Thấu Tối Thượng", SkillMult = 3.00f, SkillCd = 3,
                SkillDesc = "Bắn laser hủy diệt gây 300% ATK sát thương diện rộng (840 ST), bỏ qua 100% khiên và thủ của Player",
                PassiveDesc = "[Thân Thể Bất Diệt] Tự tạo lá chắn máu ảo bằng 15% Max HP (825 Shield) đầu trận; bỏ qua toàn bộ khiên thủ của Player",
                NormalDesc = "Bắn đạn pháo tầm xa gây 100% ATK (280 ST)",
                Speed = 0.15f, AttackCd = 2f, Reflect = 0f, Scale = 1.90f, IsBoss = true, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 4.0f, Folder = "Metal"
            },

            // ================= 3. HỆ MỘC (WOOD) =================
            new EnemyTemplate {
                Type = EnemyType.QuyRung, Name = "Quỷ Rừng", Element = ElementType.Wood, Category = EnemyCategory.Normal,
                Role = EnemyRole.Support, Row = EnemyRow.Front, HP = 320, ATK = 20, DEF = 70, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Tiếng Gầm Tự Nhiên", SkillMult = 1.25f, SkillCd = 2,
                SkillDesc = "Hô vang tăng 25% ATK cho toàn bộ quái đồng minh trong 2 Round",
                PassiveDesc = "Tăng 10% tốc độ hành động cho bản thân quái",
                NormalDesc = "Cào móng gỗ gây 100% ATK (20 ST)",
                Speed = 0.35f, AttackCd = 2f, Reflect = 0f, Scale = 0.85f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.85f, Folder = "Wood"
            },
            new EnemyTemplate {
                Type = EnemyType.BoRac, Name = "Bọ Rác", Element = ElementType.Wood, Category = EnemyCategory.Normal,
                Role = EnemyRole.Tank, Row = EnemyRow.Front, HP = 400, ATK = 30, DEF = 80, CritRate = 0.10f, CritDmg = 1.20f,
                SkillName = "Vỏ Bọc Gai Nhọn", SkillMult = 1.40f, SkillCd = 2,
                SkillDesc = "Tự tạo lớp lá chắn máu ảo hấp thụ sát thương bằng 40% Max HP bản thân (20 Shield) trong 2 Round",
                PassiveDesc = "Khi tử trận, hồi phục 5% Max HP cho 1 đồng minh ngẫu nhiên",
                NormalDesc = "Cắn lao tới gây 100% ATK (20 ST)",
                Speed = 0.45f, AttackCd = 2f, Reflect = 0f, Scale = 0.75f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.85f, Folder = "Wood"
            },
            new EnemyTemplate {
                Type = EnemyType.CungThuAm, Name = "Cung Thủ Ám", Element = ElementType.Wood, Category = EnemyCategory.Normal,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Back, HP = 160, ATK = 50, DEF = 50, CritRate = 0.10f, CritDmg = 1.30f,
                SkillName = "Mưa Tên Độc Mộc", SkillMult = 1.60f, SkillCd = 2,
                SkillDesc = "Bắn tên độc tầm xa gây 160% ATK sát thương (48 ST) lên Player",
                PassiveDesc = "Đòn đánh có 15% tỉ lệ gây hiệu ứng độc nhẹ rút 1% HP",
                NormalDesc = "Bắn tên gỗ mục gây 100% ATK (30 ST)",
                Speed = 0.30f, AttackCd = 2f, Reflect = 0f, Scale = 0.85f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 4.0f, Folder = "Wood"
            },
            new EnemyTemplate {
                Type = EnemyType.TinhLinhHoiMau, Name = "Tinh Linh Hồi Máu", Element = ElementType.Wood, Category = EnemyCategory.Normal,
                Role = EnemyRole.Support, Row = EnemyRow.Front, HP = 320, ATK = 20, DEF = 70, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Mưa Phước Lành", SkillMult = 1.15f, SkillCd = 2,
                SkillDesc = "Niệm chú hồi phục 15% Max HP cho toàn bộ đồng minh còn sống trên sân",
                PassiveDesc = "[Hương Thảo Mộc] Tăng 10% hiệu quả hồi máu của bản thân",
                NormalDesc = "Phun phấn hoa gây 100% ATK (10 ST)",
                Speed = 0.25f, AttackCd = 2f, Reflect = 0f, Scale = 0.75f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 3.5f, Folder = "Wood"
            },
            new EnemyTemplate {
                Type = EnemyType.ReCayAnThit, Name = "Rễ Cây Ăn Thịt", Element = ElementType.Wood, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.Support, Row = EnemyRow.Front, HP = 1000, ATK = 10, DEF = 100, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Rễ Mẹ Tái Sinh", SkillMult = 1.30f, SkillCd = 3,
                SkillDesc = "Hồi sinh 1 đồng minh vừa tử trận với 30% Max HP; nếu không có quái chết thì hồi 15% HP cho toàn đội",
                PassiveDesc = "[Hút Máu Ma Thuật] Mỗi đòn đánh hồi máu cho bản thân bằng 10% sát thương gây ra",
                NormalDesc = "Quất rễ gai góc gây 100% ATK (60 ST)",
                Speed = 0.20f, AttackCd = 2f, Reflect = 0f, Scale = 1.20f, IsBoss = true, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.90f, Folder = "Wood"
            },
            new EnemyTemplate {
                Type = EnemyType.PhapSuBuaChu, Name = "Pháp Sư Bùa Chú", Element = ElementType.Wood, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.Support, Row = EnemyRow.Front, HP = 1000, ATK = 10, DEF = 100, CritRate = 0.10f, CritDmg = 1.50f,
                SkillName = "Bùa Chú Phục Sinh & Cương Giáp", SkillMult = 1.40f, SkillCd = 3,
                SkillDesc = "Hồi sinh 1 đồng minh tử trận với 40% HP + Buff tăng 30% DEF cho cả đội trong 2 Round",
                PassiveDesc = "[Cổ Thuật Rút Máu] Đòn đánh thường gây hiệu ứng DoT rút 2% Max HP Player/Round trong 2 Round",
                NormalDesc = "Phóng bùa chú tầm xa gây 100% ATK (80 ST)",
                Speed = 0.25f, AttackCd = 2f, Reflect = 0f, Scale = 1.15f, IsBoss = true, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 4.0f, Folder = "Wood"
            },
            new EnemyTemplate {
                Type = EnemyType.CoThuNganNam, Name = "Cổ Thụ Ngàn Năm", Element = ElementType.Wood, Category = EnemyCategory.FinalBoss,
                Role = EnemyRole.Support, Row = EnemyRow.Front, HP = 8000, ATK = 80, DEF = 80, CritRate = 0.10f, CritDmg = 1.50f,
                SkillName = "Rễ Cây Phục Sinh Tối Thượng", SkillMult = 1.50f, SkillCd = 4,
                SkillDesc = "Hồi sinh toàn bộ đồng minh tử trận với 50% HP + Hồi 20% HP cả đội + Tạo lá chắn bằng 10% Max HP Boss cho cả đội",
                PassiveDesc = "[Mầm Sống Vĩnh Cửu] Cứ sau 3 Round tự động triệu hồi 2 Bọ Rác (Hàng trước) để cản đòn bảo vệ bản thân",
                NormalDesc = "Rễ ngầm đâm nát cận chiến gây 100% ATK (80 ST)",
                Speed = 0.10f, AttackCd = 2f, Reflect = 0f, Scale = 2.00f, IsBoss = true, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 1.2f, Folder = "Wood"
            },

            // ================= 4. HỆ THỔ (EARTH) =================
            new EnemyTemplate {
                Type = EnemyType.SlimeBun, Name = "Slime Bùn", Element = ElementType.Earth, Category = EnemyCategory.Normal,
                Role = EnemyRole.Tank, Row = EnemyRow.Front, HP = 400, ATK = 30, DEF = 80, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Bọc Bùn Giảm Sát Thương", SkillMult = 1.35f, SkillCd = 2,
                SkillDesc = "Tạo lớp màng bùn bảo vệ: Giảm 35% mọi sát thương nhận vào trong 2 Round",
                PassiveDesc = "Tăng 10% DEF khi máu xuống dưới 50%",
                NormalDesc = "Đập bùn quánh gây 100% ATK (24 ST)",
                Speed = 0.25f, AttackCd = 2f, Reflect = 0f, Scale = 0.80f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.85f, Folder = "Earth"
            },
            new EnemyTemplate {
                Type = EnemyType.LinhDat, Name = "Lính Đất", Element = ElementType.Earth, Category = EnemyCategory.Normal,
                Role = EnemyRole.Tank, Row = EnemyRow.Front, HP = 400, ATK = 30, DEF = 80, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Lá Chắn Đất Đá", SkillMult = 1.25f, SkillCd = 3,
                SkillDesc = "Tạo lá chắn máu ảo (Shield) bằng 25% Max HP bản thân (45 Shield) trong 2 Round",
                PassiveDesc = "Quái đồng minh đứng sau Lính Đất nhận thêm 15% DEF",
                NormalDesc = "Chém giáo đất nung gây 100% ATK (18 ST)",
                Speed = 0.30f, AttackCd = 2f, Reflect = 0f, Scale = 0.90f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.85f, Folder = "Earth"
            },
            new EnemyTemplate {
                Type = EnemyType.PhapSuPhongAn, Name = "Pháp Sư Phong Ấn", Element = ElementType.Earth, Category = EnemyCategory.Normal,
                Role = EnemyRole.Mage, Row = EnemyRow.Back, HP = 240, ATK = 40, DEF = 60, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Phong Ấn Ngọc Đá", SkillMult = 0.60f, SkillCd = 2,
                SkillDesc = "Gây 60% ATK sát thương nhẹ (12 ST) + Khóa 2 viên đá ngẫu nhiên trên bàn Match-3 của Player trong 1 Round",
                PassiveDesc = "Tăng 10% kháng sát thương phép thuật",
                NormalDesc = "Bắn vụn đá ma thuật gây 100% ATK (20 ST)",
                Speed = 0.25f, AttackCd = 2f, Reflect = 0f, Scale = 0.85f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 3.8f, Folder = "Earth"
            },
            new EnemyTemplate {
                Type = EnemyType.BoCapDa, Name = "Bọ Cạp Đá", Element = ElementType.Earth, Category = EnemyCategory.Normal,
                Role = EnemyRole.Tank, Row = EnemyRow.Front, HP = 400, ATK = 30, DEF = 80, CritRate = 0.10f, CritDmg = 1.30f,
                SkillName = "Giáp Gai Độc Thạch", SkillMult = 1.20f, SkillCd = 3,
                SkillDesc = "Kích hoạt giáp gai phản đòn trong 2 Round: Phản lại 20% sát thương nhận vào cho Player",
                PassiveDesc = "Giảm 10% sát thương nhận vào từ các kỹ năng nguyên tố",
                NormalDesc = "Chích đuôi đá nhọn gây 100% ATK (28 ST)",
                Speed = 0.35f, AttackCd = 2f, Reflect = 0.20f, Scale = 0.85f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.85f, Folder = "Earth"
            },
            new EnemyTemplate {
                Type = EnemyType.HatNhamThach, Name = "Hạt Nham Thạch", Element = ElementType.Earth, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.Tank, Row = EnemyRow.Front, HP = 1200, ATK = 40, DEF = 120, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Lá Chắn Dung Nham Bùng Nổ", SkillMult = 1.50f, SkillCd = 3,
                SkillDesc = "Tạo lá chắn máu ảo bằng 20% Max HP (400 Shield); khi khiên vỡ phản lại 150% giá trị khiên thành sát thương cho Player",
                PassiveDesc = "[Phản Đòn Thường Trực] Luôn phản lại 10% sát thương nhận vào từ Player",
                NormalDesc = "Lăn khối đá nóng đỏ gây 100% ATK (70 ST)",
                Speed = 0.30f, AttackCd = 2f, Reflect = 0.10f, Scale = 1.35f, IsBoss = true, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.DefensiveSelf, Range = 0.95f, Folder = "Earth"
            },
            new EnemyTemplate {
                Type = EnemyType.PhuThuyKhoaNgoc, Name = "Phù Thủy Khóa Ngọc", Element = ElementType.Earth, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.Mage, Row = EnemyRow.Back, HP = 800, ATK = 70, DEF = 80, CritRate = 0.10f, CritDmg = 1.30f,
                SkillName = "Đại Phong Ấn Trận Địa", SkillMult = 0.70f, SkillCd = 2,
                SkillDesc = "Gây 70% ATK sát thương (56 ST) + Khóa 4 viên ngọc trên bàn Match-3 trong 2 Round + Giảm 20% ATK Player trong 2 Round",
                PassiveDesc = "[Áp Chế Ma Pháp] Giảm 10% tốc độ tích lũy Energy của Player",
                NormalDesc = "Bắn quả cầu địa chấn gây 100% ATK (80 ST)",
                Speed = 0.25f, AttackCd = 2f, Reflect = 0f, Scale = 1.15f, IsBoss = true, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 4.0f, Folder = "Earth"
            },
            new EnemyTemplate {
                Type = EnemyType.BossDatNung, Name = "Boss Đất Nung", Element = ElementType.Earth, Category = EnemyCategory.FinalBoss,
                Role = EnemyRole.Tank, Row = EnemyRow.Front, HP = 9000, ATK = 90, DEF = 100, CritRate = 0.10f, CritDmg = 1.50f,
                SkillName = "Đại Địa Chấn Hủy Diệt", SkillMult = 2.00f, SkillCd = 4,
                SkillDesc = "Tạo cơn địa chấn gây 200% ATK sát thương (180 ST) + Làm Choáng Player 1 Round + Khóa 5 ô ngọc trên bàn Match-3",
                PassiveDesc = "[Giáp Thần Cương Thạch] Đầu mỗi Round tự tạo lá chắn máu ảo bằng 10% Max HP Boss (900 Shield); phản lại 10% sát thương nhận vào",
                NormalDesc = "Nện cột đá thần thánh gây 100% ATK (90 ST)",
                Speed = 0.15f, AttackCd = 2f, Reflect = 0.10f, Scale = 1.90f, IsBoss = true, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.RangedSpell, Range = 1.2f, Folder = "Earth"
            },

            // ================= 5. HỆ THỦY (WATER) =================
            new EnemyTemplate {
                Type = EnemyType.SlimeNuoc, Name = "Slime Nước", Element = ElementType.Water, Category = EnemyCategory.Normal,
                Role = EnemyRole.Mage, Row = EnemyRow.Front, HP = 240, ATK = 40, DEF = 60, CritRate = 0.05f, CritDmg = 1.20f,
                SkillName = "Tia Nước Làm Chậm", SkillMult = 0.60f, SkillCd = 2,
                SkillDesc = "Gây 60% ATK sát thương (10 ST) + Tăng chi phí cast skill của Player thêm 2 Energy (từ 5 lên 7) trong 1 Round",
                PassiveDesc = "Tăng 10% né tránh đòn đánh thường",
                NormalDesc = "Bắn tia nước áp lực tầm xa gây 100% ATK (16 ST)",
                Speed = 0.30f, AttackCd = 2f, Reflect = 0f, Scale = 0.80f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 3.8f, Folder = "Water"
            },
            new EnemyTemplate {
                Type = EnemyType.ThuyQuaiNho, Name = "Thủy Quái Nhỏ", Element = ElementType.Water, Category = EnemyCategory.Normal,
                Role = EnemyRole.DPS_FBS, Row = EnemyRow.Front, HP = 160, ATK = 50, DEF = 50, CritRate = 0.10f, CritDmg = 1.40f,
                SkillName = "Cắn Xoáy Thủy Triều", SkillMult = 1.60f, SkillCd = 2,
                SkillDesc = "Lao tới cắn xoáy mạnh mẽ gây 160% ATK sát thương (35 ST)",
                PassiveDesc = "Giảm 10% sát thương vật lý nhận vào",
                NormalDesc = "Quật vây cắn cận chiến gây 100% ATK (22 ST)",
                Speed = 0.50f, AttackCd = 2f, Reflect = 0f, Scale = 0.85f, IsBoss = false, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.MeleeDash, Range = 0.85f, Folder = "Water"
            },
            new EnemyTemplate {
                Type = EnemyType.KeTromKyNang, Name = "Kẻ Trộm Kỹ Năng", Element = ElementType.Water, Category = EnemyCategory.Normal,
                Role = EnemyRole.Mage, Row = EnemyRow.Back, HP = 240, ATK = 40, DEF = 60, CritRate = 0.10f, CritDmg = 1.30f,
                SkillName = "Cướp Đoạt Nội Năng", SkillMult = 0.50f, SkillCd = 3,
                SkillDesc = "Gây 50% ATK sát thương (5 ST) + Hút 5 Energy của Player chuyển hóa thành máu hồi phục cho bản thân",
                PassiveDesc = "[Thân Pháp Ảo Ảnh] Có 15% tỉ lệ né tránh hoàn toàn đòn đánh của Player",
                NormalDesc = "Phi dao nước tầm xa gây 100% ATK (10 ST)",
                Speed = 0.45f, AttackCd = 2f, Reflect = 0f, Scale = 0.80f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 3.8f, Folder = "Water"
            },
            new EnemyTemplate {
                Type = EnemyType.PhapSuBang, Name = "Pháp Sư Băng", Element = ElementType.Water, Category = EnemyCategory.Normal,
                Role = EnemyRole.Mage, Row = EnemyRow.Back, HP = 240, ATK = 40, DEF = 60, CritRate = 0.10f, CritDmg = 1.40f,
                SkillName = "Mũi Băng Hàn Giá", SkillMult = 0.70f, SkillCd = 2,
                SkillDesc = "Bắn mũi tên băng gây 70% ATK sát thương (20 ST) + Đóng băng 2 viên ngọc ngẫu nhiên trên bàn Match-3 trong 2 Round",
                PassiveDesc = "Tăng 10% sát thương khi mục tiêu dính hiệu ứng Băng",
                NormalDesc = "Bắn vụn băng lạnh giá gây 100% ATK (28 ST)",
                Speed = 0.30f, AttackCd = 2f, Reflect = 0f, Scale = 0.85f, IsBoss = false, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 3.8f, Folder = "Water"
            },
            new EnemyTemplate {
                Type = EnemyType.QuaiVatDamLay, Name = "Quái Vật Đầm Lầy", Element = ElementType.Water, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.Mage, Row = EnemyRow.Back, HP = 800, ATK = 70, DEF = 80, CritRate = 0.15f, CritDmg = 1.50f,
                SkillName = "Đầm Lầy Ăn Mòn", SkillMult = 0.80f, SkillCd = 3,
                SkillDesc = "Phun bùn lầy diện rộng gây 80% ATK sát thương (64 ST) + Giảm 30% DEF Player trong 2 Round + Giảm 10 Energy khởi đầu của Player",
                PassiveDesc = "[Bùn Sống Dai Dẳng] Khi HP dưới 50%, tự hồi phục 5% Max HP mỗi Round",
                NormalDesc = "Quất xúc tu cận chiến gây 100% ATK (80 ST)",
                Speed = 0.30f, AttackCd = 2f, Reflect = 0f, Scale = 1.30f, IsBoss = true, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.RangedSpell, Range = 1.2f, Folder = "Water"
            },
            new EnemyTemplate {
                Type = EnemyType.TinhLinhBangGia, Name = "Tinh Linh Băng Giá", Element = ElementType.Water, Category = EnemyCategory.WaveBoss,
                Role = EnemyRole.Mage, Row = EnemyRow.Back, HP = 800, ATK = 70, DEF = 80, CritRate = 0.20f, CritDmg = 1.80f,
                SkillName = "Đóng Băng Hàng Ngũ", SkillMult = 0.80f, SkillCd = 3,
                SkillDesc = "Niệm phép băng cực hàn gây 80% ATK sát thương (72 ST) + Đóng băng toàn bộ hàng đá dưới cùng của bàn Match-3 trong 2 Round",
                PassiveDesc = "Giảm 15% sát thương nhận vào từ các kỹ năng hệ Hỏa",
                NormalDesc = "Phóng lốc băng giá tầm xa gây 100% ATK (90 ST)",
                Speed = 0.35f, AttackCd = 2f, Reflect = 0f, Scale = 1.20f, IsBoss = true, CombatType = EnemyCombatType.Ranged,
                NormalAction = EnemySkillActionType.RangedSpell, SkillAction = EnemySkillActionType.RangedSpell, Range = 4.0f, Folder = "Water"
            },
            new EnemyTemplate {
                Type = EnemyType.ThuyThanSaNga, Name = "Thủy Thần Sa Ngã", Element = ElementType.Water, Category = EnemyCategory.FinalBoss,
                Role = EnemyRole.Mage, Row = EnemyRow.Back, HP = 6000, ATK = 100, DEF = 50, CritRate = 0.25f, CritDmg = 1.80f,
                SkillName = "Sóng Thần Diệt Tuyệt Tối Thượng", SkillMult = 2.20f, SkillCd = 3,
                SkillDesc = "Tạo sóng thần quét sạch sàn đấu gây 220% ATK sát thương (220 ST) + Choáng Player 1 Round + Triệt tiêu toàn bộ Energy của Player về 0",
                PassiveDesc = "[Thao Túng Dòng Chảy] Đòn đánh thường tự động đánh cắp 5 Energy của Player. Giảm 20% sát thương nhận vào khi còn quái hệ Thủy",
                NormalDesc = "Trảm đao nước cận chiến gây 100% ATK (100 ST)",
                Speed = 0.20f, AttackCd = 2f, Reflect = 0f, Scale = 1.90f, IsBoss = true, CombatType = EnemyCombatType.Melee,
                NormalAction = EnemySkillActionType.MeleeDash, SkillAction = EnemySkillActionType.RangedSpell, Range = 1.2f, Folder = "Water"
            }
        };
    }

    private static void EnsureFolderExists(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath)) return;

        int lastSlash = fullPath.LastIndexOf('/');
        if (lastSlash > 0)
        {
            string parent = fullPath.Substring(0, lastSlash);
            string folderName = fullPath.Substring(lastSlash + 1);
            EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
