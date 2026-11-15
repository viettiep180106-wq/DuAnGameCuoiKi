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
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Passive_DoubleSkill,
                displayName = "Nhân Đôi Kỹ Năng",
                description = "1 skill active ngẫu nhiên sẽ được dùng 2 lần liên tiếp.",
                icon = LoadIcon("1 skill active ngẫu nhiên sẽ được dùng 2 lần liên tiếp.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Passive_Reflect100,
                displayName = "Phản Đòn Toàn Diện",
                description = "50% phản 100% sát thương nhận vào quái.",
                icon = LoadIcon("50% phản 100% sát thương nhận vào quái.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Hiem_DmgToHeal,
                displayName = "Chuyển Hóa Huyết Lực",
                description = "Chuyển 50% sát thương kỹ bản thân gây ra thành hồi HP.",
                icon = LoadIcon("Chuyển 50% sát thương kĩ bản thân gây ra thành hồi hp.png"),
                category = BuffCategory.RareBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Hiem_VampireDouble,
                displayName = "Huyết Kỹ Cuồng Nộ",
                description = "Chọn 1 skill active dùng 2 lần liên tiếp +10% hút máu và x2 ST ở lần dùng liên tiếp thứ 2.",
                icon = LoadIcon("Chọn 1 skill active dùng 2 lần liên tiếp +10% hút máu và x2 st ở lần dùng liên tiếp thứ 2.png"),
                category = BuffCategory.RareBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_TangHieuUngQuai,
                displayName = "Yếu Nhược Cơ Thể",
                description = "Khi bị trúng đòn có tỉ lệ 50% x2 hiệu quả từ hiệu ứng bất lợi của quái.",
                icon = LoadIcon("Khi bị chúng đòn có tỉ lệ 50% x2 hiệu quả từ hiệu ứng.png"),
                category = BuffCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_KhoaSkillActive,
                displayName = "Niêm Ấn Kỹ Năng",
                description = "Khóa 1 skill active ngẫu nhiên.",
                icon = LoadIcon("Khóa 1 skill active ngẫu nhiên.png"),
                category = BuffCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_LamMoiSkillActive,
                displayName = "Hồi Tâm Chuyển Ý",
                description = "Làm mới 1 skill active ngẫu nhiên.",
                icon = LoadIcon("Làm mới 1 skill active ngẫu nhiên.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Hiem_DoubleNextWave,
                displayName = "Tích Lũy Sức Mạnh",
                description = "Tích lũy lại các buff ở wave này và x2 chỉ số buff ở wave sau.",
                icon = LoadIcon("Tích lũy lại các buff ở wave và x2 chỉ số buff ở wave sau.png"),
                category = BuffCategory.RareBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_CuongHoa_MaxHP,
                displayName = "Cường Hóa Sinh Lực",
                description = "Tăng 10% giới hạn HP.",
                icon = LoadIcon("Tăng 10% giới hạn hp.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_CuongHoa_Atk,
                displayName = "Cường Hóa Công Kích",
                description = "Tăng 10% tấn công.",
                icon = LoadIcon("Tăng 10% tấn công.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_CuongHoa_Def,
                displayName = "Cường Hóa Phòng Thủ",
                description = "Tăng 20% DEF.",
                icon = LoadIcon("Tăng 20% def.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_CuongHoa_Crit,
                displayName = "Cường Hóa Chí Mạng",
                description = "Tăng 5% tỉ lệ CM, 10% ST chí mạng.",
                icon = LoadIcon("Tăng 5 % tỉ lệ cm, 10% st chí mạng.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Hiem_StackAtkCrit,
                displayName = "Chiến Ý Dâng Trào",
                description = "Tăng 5% ATK, tỉ lệ CM, ST chí mạng sau mỗi lần tung ra đòn đánh.",
                icon = LoadIcon("Tăng 5% atk, tỉ lệ cm, st chí mạng sau mỗi lần tung ra đòn đánh.png"),
                category = BuffCategory.RareBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_GiamAtk,
                displayName = "Suy Giảm Công Kích",
                description = "Giảm 10% ATK.",
                icon = LoadIcon("giảm 10% atk.png"),
                category = BuffCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_GiamMaxHP,
                displayName = "Suy Giảm Sinh Lực",
                description = "Giảm 10% giới hạn HP.",
                icon = LoadIcon("giảm 10% giới hạn hp.png"),
                category = BuffCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_TaiTao_HealHP,
                displayName = "Hồi Phục Sinh Lực",
                description = "Hồi 10% HP.",
                icon = LoadIcon("hồi 10% hp.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_TaiTao_Energy,
                displayName = "Hồi Nạp Năng Lượng",
                description = "Hồi 20 lõi năng lượng.",
                icon = LoadIcon("hồi 20 lõi năng lượng.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_MatLoiKhiCrit,
                displayName = "Quá Tải Năng Lượng",
                description = "Mỗi khi có chí mạng người chơi sẽ tự mất đi 5% lõi năng lượng.",
                icon = LoadIcon("mỗi khi có chí mạng người chơi sẽ tự mất đi 5% lõi năng lượng.png"),
                category = BuffCategory.Debuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Passive_Shield50,
                displayName = "Hộ Thuẫn Kiên Cố",
                description = "Tạo 1 lá chắn máu ảo bằng 50% HP bản thân.",
                icon = LoadIcon("tạo 1 là chắn máu ảo bằng 50% hp bản thân.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Buff_Passive_Shield25_Reflect,
                displayName = "Hộ Thuẫn Phản Chấn",
                description = "Tạo 1 lá chắn máu ảo bằng 25% HP bản thân và phản lại 50% ST nhận vào trong thời gian lá chắn tồn tại.",
                icon = LoadIcon("tạo 1 lá chắn máu ảo bằng 25% hp bản thân  và phản lại 50% st nhận vào trong thời gian lá chắn còn tồn tại.png"),
                category = BuffCategory.CommonBuff
            },
            new BuffDebuffInfo
            {
                type = BuffDebuffType.Debuff_XoaPassive,
                displayName = "Tịch Thu Nội Tại",
                description = "Xóa 1 skill passive.",
                icon = LoadIcon("xóa 1 skill passive.png"),
                category = BuffCategory.Debuff
            }
        };

        db.SetEntries(list);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BuffDebuffDatabaseBuilder] Successfully built BuffDebuffDatabase with {list.Count} entries at {assetPath}");
    }
}
