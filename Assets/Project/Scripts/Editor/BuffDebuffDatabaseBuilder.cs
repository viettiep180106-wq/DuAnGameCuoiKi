using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuffDebuffDatabaseBuilder
{
    [MenuItem("Tools/Build Buff Debuff Database")]
    public static void BuildDatabase()
    {
        string dirPath = "Assets/Project/Resources/Database";
        if (!AssetDatabase.IsValidFolder("Assets/Project/Resources"))
        {
            AssetDatabase.CreateFolder("Assets/Project", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(dirPath))
        {
            AssetDatabase.CreateFolder("Assets/Project/Resources", "Database");
        }

        string assetPath = "Assets/Project/Resources/Database/BuffDebuffDatabase.asset";
        BuffDebuffDatabase db = AssetDatabase.LoadAssetAtPath<BuffDebuffDatabase>(assetPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<BuffDebuffDatabase>();
            AssetDatabase.CreateAsset(db, assetPath);
        }

        string iconFolder = "Assets/Project/Sprite/BuffDebuffIcons";

        Sprite LoadIcon(string filename)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{iconFolder}/{filename}");
        }

        List<BuffDebuffInfo> list = new List<BuffDebuffInfo>
        {
            // === NHÓM 1: CƯỜNG HÓA (40% Buff Thường - Hiệu lực 1 Level) ===
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_CuongHoa_Atk,
                displayName = "Cường Hóa Công Kích",
                description = "Tăng 10% tấn công (Hiệu lực: 1 Level).",
                icon = LoadIcon("Tăng 10% tấn công.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.CuongHoa
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_CuongHoa_Def,
                displayName = "Cường Hóa Phòng Thủ",
                description = "Tăng 20% DEF (Hiệu lực: 1 Level).",
                icon = LoadIcon("Tăng 20% def.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.CuongHoa
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_CuongHoa_MaxHP,
                displayName = "Cường Hóa Sinh Lực",
                description = "Tăng 10% giới hạn HP (Hiệu lực: 1 Level).",
                icon = LoadIcon("Tăng 10% giới hạn hp.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.CuongHoa
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_CuongHoa_Crit,
                displayName = "Cường Hóa Chí Mạng",
                description = "Tăng 5% tỉ lệ CM, +10% ST chí mạng (Hiệu lực: 1 Level).",
                icon = LoadIcon("Tăng 5 % tỉ lệ cm, 10% st chí mạng.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.CuongHoa
            },

            // === NHÓM 2: TÁI TẠO (35% Buff Thường - Tức thì) ===
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_TaiTao_Energy,
                displayName = "Hồi Nạp Năng Lượng",
                description = "Hồi ngay 20 lõi năng lượng.",
                icon = LoadIcon("hồi 20 lõi năng lượng.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.TaiTao
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_TaiTao_HealHP,
                displayName = "Hồi Phục Sinh Lực",
                description = "Hồi phục 10% lượng máu tối đa.",
                icon = LoadIcon("hồi 10% hp.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.TaiTao
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_TaiTao_HealHP20,
                displayName = "Đại Hồi Sinh Lực",
                description = "Hồi phục 20% lượng máu tối đa.",
                icon = LoadIcon("hồi 10% hp.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.TaiTao
            },

            // === NHÓM 3: PASSIVE THƯỜNG (25% Buff Thường) ===
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Passive_Shield50,
                displayName = "Hộ Thuẫn Kiên Cố",
                description = "Tạo 1 lá chắn máu ảo bằng 50% HP bản thân.",
                icon = LoadIcon("tạo 1 là chắn máu ảo bằng 50% hp bản thân.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.PassiveThuong
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Passive_Shield25_Reflect,
                displayName = "Hộ Thuẫn Phản Chấn",
                description = "Tạo lá chắn 25% HP bản thân và phản lại 50% ST nhận vào khi khiên tồn tại.",
                icon = LoadIcon("tạo 1 lá chắn máu ảo bằng 25% hp bản thân  và phản lại 50% st nhận vào trong thời gian lá chắn còn tồn tại.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.PassiveThuong
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Passive_Reflect100,
                displayName = "Phản Đòn Toàn Diện",
                description = "50% cơ hội phản 100% sát thương nhận vào về kẻ tấn công.",
                icon = LoadIcon("50% phản 100% sát thương nhận vào quái.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.PassiveThuong
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Passive_DoubleSkill,
                displayName = "Nhân Đôi Kỹ Năng",
                description = "1 kỹ năng ngẫu nhiên kích hoạt 2 lần liên tiếp (CD: 2 Round).",
                icon = LoadIcon("1 skill active ngẫu nhiên sẽ được dùng 2 lần liên tiếp.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.PassiveThuong
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Passive_DoubleStoneEffect,
                displayName = "Cộng Hưởng Nguyên Tố",
                description = "x2 hiệu quả kỹ năng đá nguyên tố (CD: 2 Round).",
                icon = LoadIcon("1 skill active ngẫu nhiên sẽ được dùng 2 lần liên tiếp.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.PassiveThuong
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_LamMoiSkillActive,
                displayName = "Hồi Tâm Chuyển Ý",
                description = "Làm mới 1 skill active ngẫu nhiên.",
                icon = LoadIcon("Làm mới 1 skill active ngẫu nhiên.png"),
                category = BuffCategory.CommonBuff,
                subCategory = BuffSubCategory.PassiveThuong
            },

            // === BUFF HIẾM (5% tổng) ===
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Hiem_StackAtkCrit,
                displayName = "Chiến Ý Dâng Trào",
                description = "Tăng 5% ATK, tỉ lệ CM, ST chí mạng sau mỗi lần tung đòn đánh (1 Level).",
                icon = LoadIcon("Tăng 5% atk, tỉ lệ cm, st chí mạng sau mỗi lần tung ra đòn đánh.png"),
                category = BuffCategory.RareBuff,
                subCategory = BuffSubCategory.Hiem
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Hiem_DoubleNextWave,
                displayName = "Tích Lũy Sức Mạnh",
                description = "Tích lũy lại các buff ở wave này và x2 chỉ số buff ở wave sau.",
                icon = LoadIcon("Tích lũy lại các buff ở wave và x2 chỉ số buff ở wave sau.png"),
                category = BuffCategory.RareBuff,
                subCategory = BuffSubCategory.Hiem
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Hiem_DmgToHeal,
                displayName = "Chuyển Hóa Huyết Lực",
                description = "Chuyển 50% sát thương kỹ bản thân gây ra thành hồi HP (1 Level).",
                icon = LoadIcon("Chuyển 50% sát thương kĩ bản thân gây ra thành hồi hp.png"),
                category = BuffCategory.RareBuff,
                subCategory = BuffSubCategory.Hiem
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Hiem_VampireDouble,
                displayName = "Huyết Kỹ Cuồng Nộ",
                description = "1 skill active dùng 2 lần liên tiếp +10% hút máu và x2 ST ở lần dùng thứ 2.",
                icon = LoadIcon("Chọn 1 skill active dùng 2 lần liên tiếp +10% hút máu và x2 st ở lần dùng liên tiếp thứ 2.png"),
                category = BuffCategory.RareBuff,
                subCategory = BuffSubCategory.Hiem
            },

            // === DEBUFF (Có hiệu lực xuyên suốt 1 Level) ===
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_GiamAtk,
                displayName = "Suy Giảm Công Kích",
                description = "Giảm 10% ATK (Hiệu lực: 1 Level).",
                icon = LoadIcon("giảm 10% atk.png"),
                category = BuffCategory.Debuff,
                subCategory = BuffSubCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_GiamMaxHP,
                displayName = "Suy Giảm Sinh Lực",
                description = "Giảm 10% giới hạn HP (Hiệu lực: 1 Level).",
                icon = LoadIcon("giảm 10% giới hạn hp.png"),
                category = BuffCategory.Debuff,
                subCategory = BuffSubCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_TangHieuUngQuai,
                displayName = "Yếu Nhược Cơ Thể",
                description = "Khi bị trúng đòn 50% x2 hiệu ứng bất lợi của quái (Hiệu lực: 1 Level).",
                icon = LoadIcon("Khi bị chúng đòn có tỉ lệ 50% x2 hiệu quả từ hiệu ứng.png"),
                category = BuffCategory.Debuff,
                subCategory = BuffSubCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_MatLoiKhiCrit,
                displayName = "Quá Tải Năng Lượng",
                description = "Mỗi khi chí mạng người chơi tự mất đi 5% lõi năng lượng (Hiệu lực: 1 Level).",
                icon = LoadIcon("mỗi khi có chí mạng người chơi sẽ tự mất đi 5% lõi năng lượng.png"),
                category = BuffCategory.Debuff,
                subCategory = BuffSubCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_KhoaSkillActive,
                displayName = "Niêm Ấn Chủ Động",
                description = "Khóa 1 skill active ngẫu nhiên (Hiệu lực: 1 Level).",
                icon = LoadIcon("Khóa 1 skill active ngẫu nhiên.png"),
                category = BuffCategory.Debuff,
                subCategory = BuffSubCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_KhoaSkillPassive,
                displayName = "Khóa Kỹ Năng Nội Tại",
                description = "Không nhận 1 skill passive khi match đá trong 1 round (CD: 2 Round, Hiệu lực: 1 Level).",
                icon = LoadIcon("xóa 1 skill passive.png"),
                category = BuffCategory.Debuff,
                subCategory = BuffSubCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_GiamThoiGianStone,
                displayName = "Phong Hóa Nguyên Tố",
                description = "Giảm thời gian hiệu lực đá nguyên tố (Hiệu lực: 1 Level).",
                icon = LoadIcon("Khi bị chúng đòn có tỉ lệ 50% x2 hiệu quả từ hiệu ứng.png"),
                category = BuffCategory.Debuff,
                subCategory = BuffSubCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_XoaPassive,
                displayName = "Tịch Thu Nội Tại",
                description = "1 skill passive tạo từ match bị tịch thu tạm hoãn, hết CD 2 Round tự động thi triển lại (Hiệu lực: 1 Level).",
                icon = LoadIcon("xóa 1 skill passive.png"),
                category = BuffCategory.Debuff,
                subCategory = BuffSubCategory.Debuff
            }
        };

        db.SetEntries(list);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BuffDebuffDatabaseBuilder] Successfully built BuffDebuffDatabase with {list.Count} entries at {assetPath}");
    }
}
