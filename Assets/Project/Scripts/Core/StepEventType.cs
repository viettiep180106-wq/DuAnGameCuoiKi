using System;

public enum StepEventType
{
    None,                       // Không có sự kiện (chuyển thẳng sang combat)
    Relic_OpeningBuff,          // Thánh vật: Tặng Buff mở đầu màn chơi (Wave 1)
    RuneStone_Gacha,            // Phiến đá cổ: Vòng quay may mắn (3 Buff / 3 Debuff, phí 25 lõi)
    RuneStone_Roll,             // Phiến đá cổ: Lật 3 thẻ bài (Đổi lượt phí 5 lõi)
    RuneStone_Chest,            // Phiến đá cổ: Mở rương bí ẩn (2 phong thư 1 buff / 1 debuff)
    RuneStone_Altar,            // Phiến đá cổ: Tế đàn (Chúc phúc hoặc Nguyền rủa)
    BeastBetting,               // Làm 1 ván cá cược đấu thú (cược tối thiểu 10 lõi)
    RandomEvent                 // Ngẫu nhiên 1 trong các sự kiện trên
}

public enum BuffDebuffType
{
    // Buff Thường
    Buff_CuongHoa_Atk,          // +10% ATK (1 Level)
    Buff_CuongHoa_Def,          // +20% DEF (1 Level)
    Buff_CuongHoa_MaxHP,        // +10% Max HP (1 Level)
    Buff_CuongHoa_Crit,         // +5% Crit Rate, +10% Crit Dmg (1 Level)
    Buff_TaiTao_Energy,         // Hồi 20 lõi năng lượng
    Buff_TaiTao_HealHP,         // Hồi 10% HP
    Buff_TaiTao_HealHP20,       // Hồi 20% HP
    Buff_Passive_Shield50,      // Tạo lá chắn máu ảo bằng 50% HP (Toàn level/bị vỡ)
    Buff_Passive_Shield25_Reflect,// Tạo lá chắn 25% HP + phản 50% dmg
    Buff_Passive_DoubleSkill,   // 1 skill active/passive dùng 2 lần liên tiếp (CD 2 rounds)
    Buff_Passive_DoubleStoneEffect, // x2 hiệu quả skill stone (CD 2 rounds)
    Buff_Passive_Reflect100,    // 50% phản 100% sát thương nhận vào quái
    Buff_LamMoiSkillActive,     // Làm mới 1 skill active ngẫu nhiên

    // Buff Hiếm (5%)
    Buff_Hiem_StackAtkCrit,     // Tăng 5% atk, crit rate, crit dmg sau mỗi đòn đánh (1 Level)
    Buff_Hiem_DoubleNextWave,   // Tích lũy buff và x2 chỉ số wave sau
    Buff_Hiem_DmgToHeal,        // Chuyển 50% sát thương gây ra thành hồi HP (1 Level)
    Buff_Hiem_VampireDouble,    // 1 skill active dùng 2 lần + 10% hút máu & x2 dmg

    // Debuff (Hiệu lực: 1 Level)
    Debuff_XoaPassive,          // Tịch thu nội tại 1 lần, chờ 2 round tự thi triển lại (1 Level)
    Debuff_GiamMaxHP,           // Giảm 10% giới hạn HP (1 Level)
    Debuff_GiamAtk,             // Giảm 10% ATK (1 Level)
    Debuff_TangHieuUngQuai,     // Bị trúng đòn 50% x2 hiệu ứng quái (1 Level)
    Debuff_MatLoiKhiCrit,       // Khi có chí mạng tự mất 5% lõi năng lượng (1 Level)
    Debuff_KhoaSkillActive,     // Khóa 1 skill active ngẫu nhiên (1 Level)
    Debuff_KhoaSkillPassive,    // Khóa kỹ năng passive khi match trong 1 round (CD: 2 round, 1 Level)
    Debuff_GiamThoiGianStone    // Giảm thời gian đá nguyên tố (1 Level)
}
