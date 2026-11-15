using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueEntry
{
    public string ID;
    public string Category;
    public string Context;
    public string Speaker;
    public string Text;
    public string AnimationNote;
    public bool IsPlayer;

    public DialogueEntry(string id, string cat, string ctx, string speaker, string text, string anim, bool isPlayer)
    {
        ID = id;
        Category = cat;
        Context = ctx;
        Speaker = speaker;
        Text = text;
        AnimationNote = anim;
        IsPlayer = isPlayer;
    }

    public DialogueLine ToDialogueLine()
    {
        return new DialogueLine
        {
            Speaker = Speaker,
            Message = Text,
            IsPlayer = IsPlayer
        };
    }
}

public static class DialogueDatabase
{
    private static readonly Dictionary<string, DialogueEntry> _entries = new Dictionary<string, DialogueEntry>();

    static DialogueDatabase()
    {
        var list = new DialogueEntry[]
        {
        new DialogueEntry("SC_01", "Mở đầu Wave (Pre-Wave)", "1. Di chuyển sang Wave mới (Bay 3 giây)", "Pháp Sư", "Bật chế độ lướt gió lẹ làng nào! Đang bay mà gió táp vào mặt mát rượi, tiến sang wave tiếp theo thôi!", "Bay lượn chibi, áo choàng tung bay, thanh tiến trình lấp đầy từ 0 -> 1 bám theo icon nhân vật.", true),
        new DialogueEntry("SC_02", "Mở đầu Wave (Pre-Wave)", "1. Di chuyển sang Wave mới (Bay 3 giây)", "Thánh Vật", "Định vị radar đang quét khu vực phía trước! 3 giây tốc biến, giữ chặt mũ bay kẻo rớt kìa bạn êi!", "Xoay tròn phát sáng neon, hiện icon mũi tên tiến lên dẫn đường, nền cuộn ngược 3.0s.", false),
        new DialogueEntry("SC_03", "Mở đầu Trong Wave", "2. Banner Wave Khám Phá xuất hiện", "Thánh Vật", "Ting ting! Radar báo phía trước hoàn toàn yên ả, chỉ nồng nặc mùi thơm của cơ hội thôi! Một không gian bí ẩn đang vẫy gọi!", "Chiếu đèn neon lấp lánh, hiện biểu tượng xúc xắc và rương báu, mở lối vào khu vực may rủi.", false),
        new DialogueEntry("SC_04", "Mở đầu Trong Wave", "2. Banner Wave Khám Phá xuất hiện", "Pháp Sư", "Ồ hố! Trúng quả wave thư giãn rồi! Để trẫm vào thám hiểm xem có món bảo vật hay thần may mắn nào độ không nào!", "Hăm hở xoa hai bàn tay, hai mắt sáng hình ngôi sao, lướt vào khu vực sự kiện khám phá.", true),
        new DialogueEntry("SC_05", "Sự kiện Nhánh", "2. Nhánh 1: Phát hiện Phiến đá cổ", "Pháp Sư", "Ui da! Cái gì cấn chân trẫm thế này?! Ối dồi ôi, một phiến đá rune cổ phát sáng lập lòe dưới đống rêu!", "Vấp chân té dúi dụi, ôm ngón chân xuýt xoa rồi trố mắt kinh ngạc chỉ tay vào phiến đá.", true),
        new DialogueEntry("SC_06", "Sự kiện Nhánh", "2. Nhánh 1: Phát hiện Phiến đá cổ", "Thánh Vật", "Úi chà, mù mờ thế mà vớ được kho báu cổ đại! Nhưng khoan... dòng chữ rune đòi 25 lõi năng lượng để kích hoạt kìa!", "Phóng to thu nhỏ hào quang tím huyền bí, quét luồng sáng laser soi vào dòng cổ ngữ khắc trên đá.", false),
        new DialogueEntry("SC_07", "Sự kiện Nhánh", "2. Nhánh 1: Phát hiện Phiến đá cổ", "Pháp Sư", "Cái gì?! Đã núp trong hang sâu tăm tối mà còn gặp ma pháp tư bản thu phí cầu đường à?! Đúng là cạn lời!", "Ôm đầu trợn tròn mắt hét toang, tay vung vẩy chỉ vào khe nhét đá rune bực tức.", true),
        new DialogueEntry("SC_08", "Sự kiện Nhánh", "2. Nhánh 1: Phát hiện Phiến đá cổ", "Thánh Vật", "Thời buổi này làm gì có bữa trưa miễn phí! Có 25 lõi thôi mà, nạp năng lượng đi để kích hoạt cơ quan cổ ngữ!", "Nhấp nháy đèn neon liên hồi, giơ icon túi tiền lấp lánh mời gọi nhiệt tình.", false),
        new DialogueEntry("SC_09", "Hoạt động Nhánh", "Hoạt động 1.1: Gacha (Vòng quay)", "Thánh Vật", "Tada! Bàn xoay ma thuật 6 ô hiện hình: 3 Buff 3 Debuff đan xen định mệnh! Quay một vòng đổi đời đi bạn êi!", "Hóa thân thành kim quay vòng xoay ma thuật neon lấp lánh, tiếng nhạc xổ số 'tè te te' xập xình.", false),
        new DialogueEntry("SC_10", "Hoạt động Nhánh", "Hoạt động 1.1: Gacha (Vòng quay)", "Pháp Sư", "Nhìn cái tỉ lệ 50/50 này mà tim đập chân run! Xin chư vị thần linh phù hộ độ trì, quay trúng ô vàng chói lọi giùm con!", "Hai tay nắm chặt cần gạt cổ ngữ kéo mạnh hết sức, mắt nhắm tịt lẩm bẩm cầu khấn.", true),
        new DialogueEntry("SC_11", "Hoạt động Nhánh", "Hoạt động 1.1: Gacha (Vòng quay)", "Thánh Vật", "Keng keng keng! Nổ hũ cực phẩm! Bàn xoay dừng đúng ô kim cương: nhận ngay hiệu ứng [Nội dung Buff]!", "Bắn pháo hoa ma thuật tưng bừng, nhạc chuông chiến thắng vang dội, hào quang vàng kim bao phủ Pháp Sư.", false),
        new DialogueEntry("SC_12", "Hoạt động Nhánh", "Hoạt động 1.1: Gacha (Vòng quay)", "Pháp Sư", "Ối dồi ôi uy tín luôn! Đã bảo tay này vàng ròng mà! Có quả buff này thì quái wave sau cứ chuẩn bị quỳ rạp dưới chân trẫm!", "Nhảy cẫng lên hò reo sung sướng, hai mắt biến thành hình ngôi sao lấp lánh, chibi tạo dáng siêu ngầu.", true),
        new DialogueEntry("SC_13", "Hoạt động Nhánh", "Hoạt động 1.1: Gacha (Vòng quay)", "Thánh Vật", "Tèn ten... Mũi tên lệch sang ô tử thần rồi! Chia buồn cùng thí sinh, ôm trọn hiệu ứng thốn: [Nội dung Debuff]!", "Toàn thân chuyển màu tím đen ảm đạm, rơi icon đầu lâu xương chéo, nốt nhạc rè rè méo xẹo.", false),
        new DialogueEntry("SC_14", "Hoạt động Nhánh", "Hoạt động 1.1: Gacha (Vòng quay)", "Pháp Sư", "Ủa alo?! Thánh Vật gian lận cài bẫy đúng không?! Dính quả debuff này thì gánh còng cả lưng, đúng là kiếp nạn thứ 82!", "Ngã ngửa ra sàn hang ôm đầu than khóc, mặt mày tái mét, linh hồn chibi bay lơ lửng khỏi mồm.", true),
        new DialogueEntry("SC_15", "Hoạt động Nhánh", "Hoạt động 1.2: Roll (Lật thẻ / Đổ xúc xắc)", "Thánh Vật", "Chào mừng đến sòng bài cổ ngữ! 3 thẻ bài ma pháp bí ẩn đang úp trên bàn, lần roll đầu tiên hoàn toàn 'free' nhé!", "Biến ra 3 lá bài ma thuật lơ lửng xoay vòng tròn, xòe bài điệu nghệ như ảo thuật gia Las Vegas.", false),
        new DialogueEntry("SC_16", "Hoạt động Nhánh", "Hoạt động 1.2: Roll (Lật thẻ / Đổ xúc xắc)", "Pháp Sư", "Để trẫm thẩm định nhân phẩm xem nào! Nếu bài phèn thì bỏ thêm 5 lõi đổi lượt, quyết săn bằng được buff Hiếm!", "Xoa cằm đăm chiêu suy nghĩ, ngón tay chỉ trỏ qua lại giữa các lá bài ma pháp rực sáng.", true),
        new DialogueEntry("SC_17", "Hoạt động Nhánh", "Hoạt động 1.2: Roll (Lật thẻ / Đổ xúc xắc)", "Thánh Vật", "Lật bài thần thánh! Ra ngay thẻ bài SSR lấp lánh hào quang: bạn nhận được hiệu ứng [Nội dung Buff]!", "Thẻ bài bốc cháy ánh sáng hoàng kim rực rỡ, Thánh Vật vỗ tay tán thưởng reo hò phấn khích.", false),
        new DialogueEntry("SC_18", "Hoạt động Nhánh", "Hoạt động 1.2: Roll (Lật thẻ / Đổ xúc xắc)", "Pháp Sư", "Haha đúng là tay thơm có số! Thẻ xịn thế này không gánh team thì hơi phí, wave sau quái chỉ có nước khóc thét!", "Giơ cao thẻ bài khoe khoang, ngửa mặt lên trời cười ha hả kiểu đắc thắng.", true),
        new DialogueEntry("SC_19", "Hoạt động Nhánh", "Hoạt động 1.2: Roll (Lật thẻ / Đổ xúc xắc)", "Thánh Vật", "Ối dồi ôi bài rác dính bẫy rồi! Rút nhầm thẻ Nguyền Rủa cổ đại, kích hoạt hiệu ứng: [Nội dung Debuff]!", "Thẻ bài xì khói đen kịt, biến hình thành icon mặt hề trollface toe toét cười cợt.", false),
        new DialogueEntry("SC_20", "Hoạt động Nhánh", "Hoạt động 1.2: Roll (Lật thẻ / Đổ xúc xắc)", "Pháp Sư", "Trời ơi đất hỡi! Tưởng bớ được buff hiếm ai dè bốc trúng cục nợ! Lại còn mất thêm lõi năng lượng nữa, cay xé lòng!", "Ném thẻ bài xuống đất giậm chân bình bịch, hai má phồng lên tức tối phụng phịu.", true),
        new DialogueEntry("SC_21", "Hoạt động Nhánh", "Hoạt động 1.3: Mở rương bí ẩn", "Thánh Vật", "Một chiếc rương đá phong ấn kèm một mảnh giấy tâm sự bí ẩn! Bên trong có hai phong thư: 1 phúc lành và 1 tai họa đấy!", "Chiếu luồng sáng vào chiếc rương cổ bằng đá bám đầy rêu phong, chỉ vào bức thư niêm phong sáp đỏ.", false),
        new DialogueEntry("SC_22", "Hoạt động Nhánh", "Hoạt động 1.3: Mở rương bí ẩn", "Pháp Sư", "Thư tay của tiền nhân à? Để trẫm vận dụng IQ vô cực giải mã câu đố xem phong thư nào giấu kho báu nào!", "Cầm mảnh giấy đọc say sưa, tay kia cầm xà beng ma thuật hăm hở cạy nắp rương kêu cọt kẹt.", true),
        new DialogueEntry("SC_23", "Hoạt động Nhánh", "Hoạt động 1.3: Mở rương bí ẩn", "Thánh Vật", "Chọn đúng phong thư định mệnh rồi! Kho báu tỏa sáng rực rỡ, trao tặng hiệu ứng thần thánh: [Nội dung Buff]!", "Nắp rương bật mở tung tóe ánh sáng vàng kim lấp lánh, châu báu và ngọc ma thuật tuôn trào rộn rã.", false),
        new DialogueEntry("SC_24", "Hoạt động Nhánh", "Hoạt động 1.3: Mở rương bí ẩn", "Pháp Sư", "Thấy chưa, thiên tài suy luận không bao giờ đoán sai! Nhận [Nội dung Buff] thế này thì đúng là ao trình vũ trụ!", "Ôm lấy đống ngọc sáng vào lòng cọ má sung sướng, chibi mắt cười híp mí tít mắt.", true),
        new DialogueEntry("SC_25", "Hoạt động Nhánh", "Hoạt động 1.3: Mở rương bí ẩn", "Thánh Vật", "Ái chà chà! Rương hóa quái Mimic thè lưỡi ngoạm trúng tay rồi! Bẫy độc phát nổ, dính ngay: [Nội dung Debuff]!", "Chiếc rương mọc răng nanh nhọn hoắt há họng cắn 'ngoạm', phun ra làn khói tím độc hại mù mịt.", false),
        new DialogueEntry("SC_26", "Hoạt động Nhánh", "Hoạt động 1.3: Mở rương bí ẩn", "Pháp Sư", "Á á á cứu trẫm! Rương đểu lừa đảo! Vừa bị ngoạm ê cả tay vừa ăn trọn bẫy độc, kiếp nạn này sao né hoài không thoát!", "Nhảy loi choi một chân vung vẩy bàn tay bị sưng vù đỏ ửng, nước mắt lưng tròng khóc ròng rã.", true),
        new DialogueEntry("SC_27", "Hoạt động Nhánh", "Hoạt động 1.4: Tế đàn Chúc phúc & Nguyền rủa", "Thánh Vật", "Hỡi pháp sư, bước lên đài tế cổ kính đi! Nơi đây lắng nghe tiếng vọng cổ xưa, ban phước hoặc thử thách linh hồn!", "Bay lên không trung, phát hào quang linh thiêng trang trọng, tế đàn đá bắt đầu phát sáng các ký tự rune cổ.", false),
        new DialogueEntry("SC_28", "Hoạt động Nhánh", "Hoạt động 1.4: Tế đàn Chúc phúc & Nguyền rủa", "Pháp Sư", "Bình thường hơi nghịch tí thôi chứ lúc này trẫm cực kỳ nghiêm túc! Cầu xin thần linh ban phát ánh sáng dẫn lối!", "Hai tay chắp trước ngực thành tâm cúi đầu, áo choàng khẽ lay động trong luồng gió ma thuật thanh tịnh.", true),
        new DialogueEntry("SC_29", "Hoạt động Nhánh", "Hoạt động 1.4: Tế đàn Chúc phúc & Nguyền rủa", "Thánh Vật", "Trời xanh thấu tỏ lòng thành! Cột sáng thiên giới giáng hạ, ban tặng Lời Chúc Phúc vĩnh cửu: [Nội dung Buff]!", "Cột ánh sáng trắng tinh khiết rọi thẳng từ trần hang xuống, những cánh hoa ánh sáng lơ lửng rơi nhẹ nhàng.", false),
        new DialogueEntry("SC_30", "Hoạt động Nhánh", "Hoạt động 1.4: Tế đàn Chúc phúc & Nguyền rủa", "Pháp Sư", "Cảm tạ đấng tối cao! Năng lượng dâng trào khắp kinh mạch, wave tới ta sẽ thanh tẩy sạch bóng lũ quỷ dữ!", "Dang rộng hai tay đón nhận hào quang, ánh mắt kiên định rạng ngời, thần thái uy nghiêm như đấng cứu thế.", true),
        new DialogueEntry("SC_31", "Hoạt động Nhánh", "Hoạt động 1.4: Tế đàn Chúc phúc & Nguyền rủa", "Thánh Vật", "Nguy rồi! Tế đàn bị vấy bẩn bởi tà khí cổ xưa, Lời Nguyền Hắc Ám đã giáng xuống: [Nội dung Debuff]!", "Tế đàn chuyển sang màu đỏ máu rực lửa, khói đen bốc lên cuồn cuộn, sấm sét tím xé toạc không gian.", false),
        new DialogueEntry("SC_32", "Hoạt động Nhánh", "Hoạt động 1.4: Tế đàn Chúc phúc & Nguyền rủa", "Pháp Sư", "Trời phạt thật rồi sao?! Người ta đi tu đắc đạo còn trẫm khấn vái lại rước họa vào thân! Đắng lòng thực sự!", "Quỳ sụp xuống đất, hai tay ôm trán run rẩy, sau lưng hiện bóng ma hắc ám đè nặng đôi vai.", true),
        new DialogueEntry("SC_33", "Sự kiện Nhánh", "3. Nhánh 2: Sự kiện Cá cược", "Thánh Vật", "Ê bạn hiền, chơi an toàn chán lắm! Bên kia có sới đấu thú ma thuật, cược tối thiểu 10 lõi thử vận may không?", "Nháy mắt tinh quái, bay lượn vòng quanh, hóa thân thành chiếc cân tiểu ly ma thuật cân đo đong đếm.", false),
        new DialogueEntry("SC_34", "Sự kiện Nhánh", "3. Nhánh 2: Sự kiện Cá cược", "Pháp Sư", "Lại tính dụ trẫm vào con đường đỏ đen à? Cơ mà cược thắng được gì ngon không? Nói nghe xem nào!", "Nheo mắt nghi ngờ, tay xoa cằm suy nghĩ, hai tai chibi giật giật tò mò đầy phấn khích.", true),
        new DialogueEntry("SC_35", "Sự kiện Nhánh", "3. Nhánh 2: Sự kiện Cá cược", "Thánh Vật", "Cược càng nhiều lõi tỉ lệ rinh Buff Hiếm càng cao! Thắng thì bay nóc nhà, còn thua thì... nhận Debuff bựa nha!", "Bùng nổ hai ngọn lửa xanh đỏ trên hai tay, nhe răng cười nham hiểm kích thích máu liều của người chơi.", false),
        new DialogueEntry("SC_36", "Sự kiện Nhánh", "3. Nhánh 2: Sự kiện Cá cược", "Pháp Sư", "Người không chơi là người không bao giờ thắng! Chốt đơn, đặt luôn 20 lõi vào cửa rồng lửa! Tổ tiên độ con!", "Mắt bốc hỏa quyết tâm, đập mạnh nắm đấm vào lòng bàn tay, dốc cạn túi lõi năng lượng ném vào bàn cược.", true),
        new DialogueEntry("SC_37", "Sự kiện Nhánh", "3. Nhánh 2: Sự kiện Cá cược", "Thánh Vật", "Hú hét vang dội! Cửa rồng lửa đại thắng áp đảo! Bạn trúng cược cực đậm, húp trọn siêu phẩm: [Nội dung Buff]!", "Nhảy múa ăn mừng điên cuồng, pháo kim tuyến bắn tung tóe, icon túi tiền vàng rơi lộp độp như mưa.", false),
        new DialogueEntry("SC_38", "Sự kiện Nhánh", "3. Nhánh 2: Sự kiện Cá cược", "Pháp Sư", "Gáy to lên anh em ơi! Tay tổ cờ bạc là đây chứ đâu! Có [Nội dung Buff] thì chấp cả họ nhà quái wave sau luôn!", "Vung gậy chỉ tay lên trời múa may ngạo nghễ, cười ha hả rung cả hang động, phong thái bất bại.", true),
        new DialogueEntry("SC_39", "Sự kiện Nhánh", "3. Nhánh 2: Sự kiện Cá cược", "Thánh Vật", "Toang rồi ông giáo ơi! Cửa rồng lửa bị lật kèo phút 89! Mất sạch lõi cược lại còn gánh thêm: [Nội dung Debuff]!", "Hóa thành màu đen trắng ảm đạm, giơ biển báo 'Thua Trắng', phát tiếng còi xe cấp cứu 'èn en èn en'.", false),
        new DialogueEntry("SC_40", "Sự kiện Nhánh", "3. Nhánh 2: Sự kiện Cá cược", "Pháp Sư", "Ủa sao kỳ zợ?! Đúng là 'còn thở là còn gỡ' mà gỡ hồi thấy hết thở luôn! Ra đê ở thật rồi bà con ơi!", "Ngã quỵ đập đầu xuống đất, hai chân đạp loạn xạ ăn vạ, linh hồn chibi hóa mây bay thẳng lên trần hang.", true),
        new DialogueEntry("SC_41", "Mở đầu Trong Wave", "4. Banner Wave Chiến Đấu: Đội hình Quái Thường", "Thánh Vật", "Cảnh báo quái vật! Đội hình 5 quái chia 2 hàng đang án ngữ! Hàng trước che chắn cho hàng sau, phải dứt điểm hàng trước mới đánh được hàng sau đấy!", "Lia camera sang phải đặt tâm giữa sân; quét viền đỏ 2 quái hàng trước cảnh báo rào cản.", false),
        new DialogueEntry("SC_42", "Mở đầu Trong Wave", "4. Banner Wave Chiến Đấu: Đội hình Quái Thường", "Pháp Sư", "Rõ như ban ngày! Match đá chưởng ngay lập tức, tốn 5 mana mỗi phát. Hết mana trẫm bấm Skip đổi lượt liền, chuẩn bị nghênh chiến!", "Thủ thế trượng phép, mắt bốc lửa quyết tâm; Banner 'Bắt đầu' sáng rực, bàn Match Stone trượt lên.", true),
        new DialogueEntry("SC_43", "Mở đầu Trong Wave", "5. Banner Wave Chiến Đấu: Chạm trán Boss Wave", "Thánh Vật", "Ái chà chà! Boss Wave xuất hiện tỏa aura tím rực rỡ! Coi chừng nó dùng kỹ năng sát thương lớn và có hỗ trợ hồi sinh quái cùng hệ phía sau!", "Camera lia cận cảnh Boss Wave; hiệu ứng sấm sét chớp nháy đe dọa, còi báo động khẩn cấp.", false),
        new DialogueEntry("SC_44", "Mở đầu Trong Wave", "5. Banner Wave Chiến Đấu: Chạm trán Boss Wave", "Pháp Sư", "Căng đấy nhưng trẫm thích! Tập trung hỏa lực hạ gục hàng đỡ đòn rồi dồn combo tiễn Boss về vườn! Bật banner Bắt đầu chiến nào!", "Vung gậy ma pháp vẽ ấn chú uy lực; chibi nghiến răng tự tin; Banner 'Bắt đầu' bùng cháy sẵn sàng vào trận.", true),
        new DialogueEntry("SC_45", "Mở đầu Trong Wave", "6. Banner Wave Chiến Đấu: Chạm trán Final Boss (Trùm Cuối)", "Thánh Vật", "BÁO ĐỘNG CẤP MAX! Trùm cuối Final Boss ngự trị chiến trường! Lực chiến áp đảo, kỹ năng tối thượng hủy diệt toàn sàn đấu!", "Màn hình rung chuyển dữ dội, mây đen cuộn trào sấm sét đỏ rực; Thánh Vật dựng khiên bảo hộ run rẩy.", false),
        new DialogueEntry("SC_46", "Mở đầu Trong Wave", "6. Banner Wave Chiến Đấu: Chạm trán Final Boss (Trùm Cuối)", "Pháp Sư", "Trận chiến sinh tử là đây! Không còn đường lui nữa, bung hết mana match đá tuyệt kỹ tối thượng! Quyết chiến tới cùng!", "Toàn thân bốc cháy linh lực rực rỡ; tiếng nhạc chiến đấu hoành tráng vang dội; Banner 'Bắt đầu' kích hoạt vào trận.", true),
        // 7. Chiến thắng Quái Thường: Thắng áp đảo (Ao chình / Out trình)
        new DialogueEntry("SC_47", "Chiến thắng Wave (Wave Victory)", "7. Chiến thắng Quái Thường: Thắng áp đảo (Ao chình / Out trình)", "Pháp Sư", "Ủa alo? Chưa kịp giãn gân cốt mà cả lò nhà quái đã 'bay màu' hết rồi á? Đúng là ao trình không lối thoát, đánh đấm gì tầm này nữa!", "Vung gậy xoay một vòng điệu nghệ, thổi khói nơi đầu trượng phép; chibi khoanh tay hất cằm cười nhếch mép cực ngầu, icon kính râm đen rơi xuống mắt.", true),
        new DialogueEntry("SC_48", "Chiến thắng Wave (Wave Victory)", "7. Chiến thắng Quái Thường: Thắng áp đảo (Ao chình / Out trình)", "Thánh Vật", "Bình tĩnh bớt gáy lại bạn êi! Quái tép riu khởi động thôi mà làm như đoạt cúp thế giới. Nhưng công nhận quả combo ngọc vừa rồi nuột nà thật sự!", "Bay giật lùi đảo mắt trollface, sau đó giơ ngón cái tán thưởng; hiệu ứng chữ 'OUTPLAYED' neon nhấp nháy trên đầu Pháp Sư.", false),
        new DialogueEntry("SC_49", "Chiến thắng Wave (Wave Victory)", "7. Chiến thắng Quái Thường: Thắng áp đảo (Ao chình / Out trình)", "Pháp Sư", "Nội tại thiên tài nó ăn vào máu rồi! Trẫm mà match ngọc thì chuẩn chỉ từng mi-li-mét, quái nhìn thấy chỉ có nước 'trầm cảm'!", "Chibi tạo dáng idol Kpop bắn tim, hào quang lấp lánh quanh người, gậy ma thuật phát sáng rực rỡ.", true),
        new DialogueEntry("SC_50", "Chiến thắng Wave (Wave Victory)", "7. Chiến thắng Quái Thường: Thắng áp đảo (Ao chình / Out trình)", "Thánh Vật", "Thôi bớt flex lại giùm! Dọn chiến trường lẹ lẹ còn chuẩn bị lướt wave tiếp theo, radar đang báo phía trước còn nhiều kèo căng lắm đấy!", "Hóa thân thành chiếc chổi lông gà ma thuật quét bụi quét sàn; nhấp nháy đèn tín hiệu giục giã di chuyển sang wave mới.", false),
        // 8. Chiến thắng Quái Thường: Thắng chật vật (Thở oxi / Lật kèo 1 chấm máu)
        new DialogueEntry("SC_51", "Chiến thắng Wave (Wave Victory)", "8. Chiến thắng Quái Thường: Thắng chật vật (Thở oxi / Lật kèo 1 chấm máu)", "Pháp Sư", "Hú hồn chim én! Tưởng đâu quả này trẫm đăng xuất khỏi trái đất rồi chứ! Còn đúng 1 chấm máu đỏ lòm, tim đập thình thịch như đánh trống trường!", "Ngồi phệt xuống đất ôm ngực thở dốc hồng hộc; thanh máu nhấp nháy đỏ báo động ở mức 1% HP, mồ hôi chibi rơi thành suối.", true),
        new DialogueEntry("SC_52", "Chiến thắng Wave (Wave Victory)", "8. Chiến thắng Quái Thường: Thắng chật vật (Thở oxi / Lật kèo 1 chấm máu)", "Thánh Vật", "Thần may mắn vừa gánh còng cả lưng đấy nhé! Cú match ngọc 5 viên phút chót lật kèo thần thánh, suýt tí nữa là về với ông bà bu rồi!", "Thở phào nhẹ nhõm biến thành hình bong bóng xì hơi; giơ bình cứu thương mini xịt băng gạc quấn quanh đầu Pháp Sư.", false),
        new DialogueEntry("SC_53", "Chiến thắng Wave (Wave Victory)", "8. Chiến thắng Quái Thường: Thắng chật vật (Thở oxi / Lật kèo 1 chấm máu)", "Pháp Sư", "Người ta gọi đấy là bản lĩnh sinh tồn đỉnh chóp! 'Còn thở là còn gỡ', một giọt máu cũng đủ tiễn cả đội hình địch về nơi chín suối!", "Chống gậy gượng đứng dậy, một mắt nhắm tịt một mắt mở to, giơ nắm đấm run rẩy tỏ vẻ kiên cường bất khuất.", true),
        new DialogueEntry("SC_54", "Chiến thắng Wave (Wave Victory)", "8. Chiến thắng Quái Thường: Thắng chật vật (Thở oxi / Lật kèo 1 chấm máu)", "Thánh Vật", "Gáy to thì giỏi lắm! Tí nữa nhớ ghé đài tế đàn hồi máu hoặc săn buff giáp đi, chứ wave sau mà dính 1 hắt hơi là bốc hơi luôn đó!", "Chiếu đèn laser đỏ cảnh báo nguy hiểm; hiện icon bình HP lớn nhấp nháy nhắc nhở hồi phục khẩn cấp.", false),
        // 9. Chiến thắng Boss Wave: Hạ gục Quái Tinh Anh (Vượt ải gay cấn)
        new DialogueEntry("SC_55", "Chiến thắng Wave (Wave Victory)", "9. Chiến thắng Boss Wave: Hạ gục Quái Tinh Anh (Vượt ải gay cấn)", "Thánh Vật", "Keng keng keng! Boss Wave đã quỵ ngã! Đỡ đòn trâu bò lẫn phép thuật hồi sinh cũng không đỡ nổi cơn mưa kỹ năng vừa rồi! Uy tín 100 điểm!", "Bắn pháo bông giấy ma thuật tung tóe; camera rung nhẹ ăn mừng; Boss Wave biến thành tượng đá rồi nứt vỡ tan tành.", false),
        new DialogueEntry("SC_56", "Chiến thắng Wave (Wave Victory)", "9. Chiến thắng Boss Wave: Hạ gục Quái Tinh Anh (Vượt ải gay cấn)", "Pháp Sư", "Haha! Tinh anh với chả thủ lĩnh, đứng trước dàn combo match-3 của trẫm thì cũng thành bia đỡ đạn hết! Gáy khét lẹt cả góc rừng luôn nhé!", "Nhảy cẫng lên đạp chân lên bệ đá vỡ của Boss, giơ cao trượng ma thuật phóng tia sét chiến thắng lên trời; chibi cười toe toét đắc thắng.", true),
        new DialogueEntry("SC_57", "Chiến thắng Wave (Wave Victory)", "9. Chiến thắng Boss Wave: Hạ gục Quái Tinh Anh (Vượt ải gay cấn)", "Thánh Vật", "Công nhận quả dồn mana dứt điểm hàng trước rồi hạ gục boss đỉnh chóp thật! Chiến thuật 5v1 này áp dụng mượt mà không tì vết!", "Bay vòng quanh Pháp Sư vỗ tay bôm bốp; hào quang MVP màu vàng kim chiếu rọi toàn thân nhân vật.", false),
        new DialogueEntry("SC_58", "Chiến thắng Wave (Wave Victory)", "9. Chiến thắng Boss Wave: Hạ gục Quái Tinh Anh (Vượt ải gay cấn)", "Pháp Sư", "Phải thế chứ! Cứ đà phong độ này thì quái cấp nào cũng phải quỳ gối xin tha. Tiếp tục hành trình thôi, trẫm đang nóng máy lắm rồi!", "Hất áo choàng bay phấp phới, mắt rực lửa nhiệt huyết; hiệu ứng Buff ATK tăng tạm thời lóe sáng rực rỡ.", true),
        // 10. Chiến thắng Final Boss: Tiễn Trùm Cuối (Đại thắng màn chơi)
        new DialogueEntry("SC_59", "Chiến thắng Wave (Wave Victory)", "10. Chiến thắng Final Boss: Tiễn Trùm Cuối (Đại thắng màn chơi)", "Thánh Vật", "CHẤN ĐỘNG VŨ TRỤ! FINAL BOSS ĐÃ GỤC NGÃ HOÀN TOÀN! Tà khí tiêu tan, hắc ám rút lui, chúng ta đã phá đảo chiến trường sinh tử này rồi bạn êi!", "Toàn màn hình bùng nổ hiệu ứng ánh sáng hoàng kim rực rỡ; tiếng chuông vàng ngân vang rộn rã; Thánh Vật nhảy múa hiphop tưng bừng trên không.", false),
        new DialogueEntry("SC_60", "Chiến thắng Wave (Wave Victory)", "10. Chiến thắng Final Boss: Tiễn Trùm Cuối (Đại thắng màn chơi)", "Pháp Sư", "Trùm cuối cũng chỉ là muỗi thôi! Máu trâu dam to cỡ mấy mà gặp combo tối thượng của trẫm thì cũng phải về làng dưỡng sức! Đỉnh của chóp!", "Tạo dáng siêu anh hùng hạ cánh (Superhero Landing); áo choàng rực sáng ma pháp 7 màu; chibi chống nạnh ngửa mặt cười lớn.", true),
        new DialogueEntry("SC_61", "Chiến thắng Wave (Wave Victory)", "10. Chiến thắng Final Boss: Tiễn Trùm Cuối (Đại thắng màn chơi)", "Thánh Vật", "Phá đảo ngoạn mục không tì vết! Danh hiệu Bậc Thầy Phép Thuật Match-3 chính thức thuộc về bạn! Giờ thì gom sạch bảo vật ăn mừng thôi!", "Cầm cúp vô địch vàng chóe trao cho Pháp Sư; dàn pháo hoa ma thuật bay lượn trên nền trời, chữ 'VICTORY' 3D xoay tít.", false),
        new DialogueEntry("SC_62", "Chiến thắng Wave (Wave Victory)", "10. Chiến thắng Final Boss: Tiễn Trùm Cuối (Đại thắng màn chơi)", "Pháp Sư", "Đỉnh nóc kịch trần bay phấp phới! Chiến thắng này xin dành tặng cho những cú match 5 viên thần thánh! Chúng ta là bất khả chiến bại!", "Nâng cúp vàng lên cao, chibi nháy mắt giơ hai ngón tay chữ V; toàn bộ thú cưng và linh hồn xuất hiện cùng vỗ tay chúc mừng.", true),
        // 11. Thu thập Lõi Năng Lượng (Energy Core Loot)
        new DialogueEntry("SC_63", "Thu thập Lõi Năng Lượng (Energy Core Loot)", "11. Rơi và Thu Thập Lõi Sau Trận Đánh", "Thánh Vật", "Mưa năng lượng rơi như mưa rào mùa hạ kìa! Gom lẹ gom lẹ, ví tiền đang rủng rỉnh reo vui!", "Hóa thân thành chiếc nam châm ma thuật hút lõi năng lượng vèo vèo; hiệu ứng tia sáng xanh vàng bay rợp trời về phía túi đồ.", false),
        new DialogueEntry("SC_64", "Thu thập Lõi Năng Lượng (Energy Core Loot)", "11. Rơi và Thu Thập Lõi Sau Trận Đánh", "Pháp Sư", "Haha, phát tài phát lộc rồi! Lõi sáng lấp lánh nhìn sướng cả mắt, đếm mỏi cả tay luôn!", "Mở to túi thần kỳ hứng lõi, hai mắt biến thành hình viên đá rune xoay tròn; chibi nhảy chân sáo lượm đồ.", true),
        new DialogueEntry("SC_65", "Thu thập Lõi Năng Lượng (Energy Core Loot)", "11. Rơi và Thu Thập Lõi Sau Trận Đánh", "Thánh Vật", "Tích tiểu thành đại, có mớ lõi này ghé đài tế đàn mua đồ nâng cấp skill thì quái chỉ có nước khóc thét!", "Hiện bảng thống kê lõi vừa nhặt với số lượng nhảy số ting ting; Thánh Vật làm động tác 'đếm tiền' cực nhộn.", false),
        new DialogueEntry("SC_66", "Thu thập Lõi Năng Lượng (Energy Core Loot)", "11. Rơi và Thu Thập Lõi Sau Trận Đánh", "Pháp Sư", "Chuẩn bài con gà tây! Nạp đầy ví rồi thì ngại gì vết bẩn, wave tiếp theo thẳng tiến thôi!", "Vỗ bôm bốp vào túi tiền căng phồng bên hông, chỉ gậy trượng về phía trước đầy hào khí; sẵn sàng bước vào wave mới.", true),
        };

        foreach (var e in list)
        {
            _entries[e.ID] = e;
        }
    }

    public static IReadOnlyDictionary<string, DialogueEntry> HardcodedEntries => _entries;

    public static DialogueEntry Get(string id)
    {
        // 1. Ưu tiên tải từ ScriptableObject asset (cho phép designer/writer chỉnh trên Inspector)
        if (DialogueDatabaseSO.Instance != null)
        {
            var soEntry = DialogueDatabaseSO.Instance.GetEntry(id);
            if (soEntry != null) return soEntry;
        }

        // 2. Fallback về từ điển C# tĩnh
        return _entries.TryGetValue(id, out var entry) ? entry : null;
    }

    public static List<DialogueLine> GetLines(params string[] ids)
    {
        var lines = new List<DialogueLine>();
        foreach (var id in ids)
        {
            var e = Get(id);
            if (e != null)
            {
                lines.Add(e.ToDialogueLine());
            }
        }
        return lines;
    }

    // Pre-Wave (3s Fly)
    public static List<DialogueLine> GetPreWaveLines() => GetLines("SC_01", "SC_02");

    // Discovery Banner
    public static List<DialogueLine> GetDiscoveryBannerLines() => GetLines("SC_03", "SC_04");

    // Discovery: Ancient Rune Stone (25 Cores)
    public static List<DialogueLine> GetRuneDiscoveryLines() => GetLines("SC_05", "SC_06", "SC_07", "SC_08");

    // Gacha Wheel
    public static List<DialogueLine> GetGachaIntroLines() => GetLines("SC_09", "SC_10");
    public static List<DialogueLine> GetGachaResultLines(bool isBuff) => isBuff ? GetLines("SC_11", "SC_12") : GetLines("SC_13", "SC_14");

    // Roll 3 Cards
    public static List<DialogueLine> GetRollIntroLines() => GetLines("SC_15", "SC_16");
    public static List<DialogueLine> GetRollResultLines(bool isBuff) => isBuff ? GetLines("SC_17", "SC_18") : GetLines("SC_19", "SC_20");

    // Ancient Chest
    public static List<DialogueLine> GetChestIntroLines() => GetLines("SC_21", "SC_22");
    public static List<DialogueLine> GetChestResultLines(bool isGood) => isGood ? GetLines("SC_23", "SC_24") : GetLines("SC_25", "SC_26");

    // Blessing Altar
    public static List<DialogueLine> GetAltarIntroLines() => GetLines("SC_27", "SC_28");
    public static List<DialogueLine> GetAltarResultLines(bool isBuff = true) => isBuff ? GetLines("SC_29", "SC_30") : GetLines("SC_31", "SC_32");

    // Beast Betting
    public static List<DialogueLine> GetBeastBettingIntroLines() => GetLines("SC_33", "SC_34");
    public static List<DialogueLine> GetBeastBettingBetLines() => GetLines("SC_35", "SC_36");
    public static List<DialogueLine> GetBeastBettingResultLines(bool isWin) => isWin ? GetLines("SC_37", "SC_38") : GetLines("SC_39", "SC_40");

    // Combat Intro
    public static List<DialogueLine> GetCombatIntroLines(StepDifficulty diff, bool isFinalBoss = false, bool isBossWave = false)
    {
        if (isFinalBoss) return GetLines("SC_45", "SC_46");
        if (isBossWave || diff == StepDifficulty.VeryHard || diff == StepDifficulty.Hard) return GetLines("SC_43", "SC_44");
        return GetLines("SC_41", "SC_42");
    }

    // Combat Results
    public static List<DialogueLine> GetCombatVictoryLines() => GetLines("SC_47", "SC_48", "SC_49", "SC_50");

    public static List<DialogueLine> GetCombatVictoryLines(bool isBoss, bool isFinalBoss, float hpRatio)
    {
        if (isFinalBoss) return GetLines("SC_59", "SC_60", "SC_61", "SC_62");
        if (isBoss) return GetLines("SC_55", "SC_56", "SC_57", "SC_58");
        if (hpRatio < 0.30f) return GetLines("SC_51", "SC_52", "SC_53", "SC_54");
        return GetLines("SC_47", "SC_48", "SC_49", "SC_50");
    }

    public static List<DialogueLine> GetEnergyLootLines() => GetLines("SC_63", "SC_64", "SC_65", "SC_66");

    public static List<DialogueLine> GetCombatDefeatLines() => GetLines("SC_40");
}
