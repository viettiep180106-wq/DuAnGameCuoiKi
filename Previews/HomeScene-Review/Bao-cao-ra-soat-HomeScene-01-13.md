# Rà soát HomeScene và kế hoạch sửa 13 màn

HomeScene đã có nội dung của 13 màn. Hai mục Nâng cấp và Phó bản đang mở nhầm nhóm, thanh tab con của Nâng cấp sai cấp điều hướng, và một số màn mới dừng ở ảnh nền, icon và nhãn tĩnh. Đợt sửa nên xử lý liên kết điều hướng trước, rồi chỉnh bố cục và các thao tác xem trước bằng bộ sprite đã import.

Phạm vi lần này là báo cáo và kế hoạch. Đối chiếu dựa trên HomeScene đang lưu, các tham chiếu nút trong scene, mô tả Markdown và đủ 13 ảnh gốc mà Markdown trỏ tới. Unity Editor đang mở HomeScene ở Edit Mode khi kiểm tra. Kết luận về nút dựa trên component và sự kiện lưu trong scene. Kiểm thử đường bấm chuột/chạm là tiêu chí của đợt sửa. [File thiết kế](<C:/Users/HP/Downloads/home_scene_ui_ux_design.md>) · [HomeScene](../../Assets/Project/Scene/HomeScene.unity) · [Ảnh tham chiếu trong dự án](Layout-v2/Tham-chieu)

## 1. Điều hướng

| Mục trên dock | Hiện đang mở | Cần mở sau khi sửa |
| --- | --- | --- |
| Cửa hàng | `ShopBG`, Scene 03–04 | Tiền tệ mặc định, chuyển qua Gacha |
| Nâng cấp | `UpgradeBG` chứa Scene 10–11 | Scene 05–09, mở Nhân vật mặc định |
| Trang chủ | `HomeBG`, Scene 01 và popup 02 | Trang chủ mặc định khi vào HomeScene, mở popup bằng nút Gói Đầu |
| Phó bản | `Phó_Bản` chứa Scene 05–09 | Scene 10–11, mở Leo Tháp mặc định |
| Thành tích | `RewardBG`, Scene 12–13 | Nhiệm vụ mặc định, chuyển qua Xếp hạng |

**Lỗi gắn ngược đã được xác nhận.** `SwitchPageUI.pages` nối nút Nâng cấp tới `UpgradeBG` và nút Phó bản tới `Phó_Bản`. Chính nội dung con đang nằm sai nhóm. Giữ thứ tự, nhãn và icon dock. Chuyển Scene 05–09 về `UpgradeBG`, chuyển Scene 10–11 về `Phó_Bản`, rồi kiểm tra cả mặc định và nút con của từng nhóm. [Tham chiếu dock trong scene](../../Assets/Project/Scene/HomeScene.unity#L925) · [SwitchPageUI](../../Assets/Project/Scripts/UI/ManagerGameUI/SwitchPageUI.cs)

**Nút chuyển trang con hiện có nhưng tổ chức sai.** Có 11 bản thanh tab được đặt bên trong 11 màn con. Các tab chưa chọn có `Button` gọi `SetActive(false)` cho màn hiện tại và `SetActive(true)` cho màn đích. Tab đang chọn chỉ là ảnh. Vì hai nhóm bị gắn ngược, người dùng phải vào nhầm mục mới thấy thanh mình cần. Bản kiểm tra trước gọi sự kiện trực tiếp theo tên container nên bỏ sót cả việc dock hiển thị sai nội dung lẫn đường raycast từ thao tác chạm.

**Cấu trúc đề xuất.** Mỗi nhóm lớn có một thanh tab dùng chung đặt cạnh vùng nội dung. Một bộ điều khiển giữ trạng thái chọn và bật đúng một màn con. Nâng cấp có bốn mục ngang cấp **Nhân vật, Trang phục, Pet Thú Cưng, Đá Kỹ Năng**. Chỉ khi chọn Pet mới hiện hàng thứ hai **Vườn Nuôi Thú / Lưới Mua Pet**. Mỗi nút có vùng bấm trên cả nền và nhãn. HUD dùng chung để một lần sửa có hiệu lực trên cả các trang. Bản hiện tại lưu 12 cụm HUD. Các nút lọc, slot hoặc thẻ chọn trong nội dung có trạng thái riêng với tab chuyển màn.

## 2. Đối chiếu từng màn

| Scene | Hiện trạng và phần cần bổ sung |
| --- | --- |
| **01 · Trang chủ** | Đã có HUD, hai cột lối tắt, Capy, rương AFK, bốn mốc và nút Xuất Phát. Nút thu gọn hiện là hình. Thêm trạng thái thu gọn/mở lại và rà khoảng cách theo mockup. Bản scene hiện đã gắn `SceneTransitionButton` vào Xuất Phát, cần giữ component và kiểm tra lại luồng sau khi sửa UI. Rương AFK hiện là hình tĩnh, có thể thêm động nhẹ khi duyệt phần trình bày. |
| **02 · Gói Tân Thủ** | Popup có lớp tối và nút mở/đóng. Hiện có ba ô thưởng trên một hàng. Ảnh gốc dùng lưới 2×2 và thêm 50.000 Vàng, còn đoạn Markdown chỉ liệt kê ba món. Đề xuất theo lưới 2×2 để đúng bố cục ảnh, dùng icon vàng đã có cho ô thứ tư và giữ số liệu hiển thị ở dạng mẫu. |
| **03 · Tiền tệ** | Đã có bốn nhóm nội dung và đủ số thẻ. Nút quà hằng ngày đang là chữ, thẻ nạp thiếu vùng giá riêng, hàng đổi vàng thiếu chi phí Ngọc theo từng mức. Căn lại banner và từng hàng, dùng chung nền nút, giữ trạng thái miễn phí và xem quảng cáo rõ ràng trong bản duyệt UI. |
| **04 · Gacha** | Có mâm, kim, tâm, cam kết và bốn hình phần thưởng. `Xem tỷ lệ` là ảnh và chữ. Bổ sung nút mở/đóng bảng tỷ lệ mẫu, khung riêng cho cụm vòng quay và khu xem trước 2×2 có tên vật phẩm. Dùng dữ liệu minh họa để duyệt bố cục. |
| **05 · Nhân vật** | Dựng Capy bên trái và chỉ số bên phải cùng hàng. Mockup đặt khung nhân vật **toàn chiều ngang ở trên**, Capy giữa, mũi tên hai bên, tên và thiên phú phía dưới. Bảng chỉ số là khối độc lập bên dưới với bốn dòng, trị số hiện tại, mức tăng, nguyên liệu và nút xanh. Banner nhân vật sự kiện đặt cuối. Bổ sung nút mũi tên xem nhân vật mẫu và các dòng thông tin còn thiếu. |
| **06 · Trang phục** | Đã có Vịt Hoàng Gia, bệ, năm sao và nút tím. Mũi tên chưa có `Button`. Cần khung thử đồ có tên bộ và hiệu ứng, khung nâng sao riêng với trạng thái từng cấp sao, chi phí, và thẻ trang phục chưa sở hữu ở cuối. Cho phép đổi preview bằng hai mũi tên. Giữ Vịt Hoàng Gia theo mô tả Markdown và sprite dự án, dùng ảnh gốc để căn bố cục vì tên bộ trong ảnh khác. |
| **07 · Mua Pet** | Có lưới tám hình và chi tiết Cáo Lửa. Các thẻ chưa bấm được, viền chọn và nội dung chi tiết cố định. Chuyển lưới sang tầng con dưới Pet, hiển thị tên, giá và huy hiệu phẩm cấp từng thẻ. Khi chọn thẻ, cập nhật viền, avatar, chỉ số và kỹ năng trong khối chi tiết từ dữ liệu mẫu. |
| **08 · Vườn Pet** | Có ba slot, pet minh họa và panel dự bị. Slot 2 chưa có `Button` và panel đang hiện sẵn. Tách hàng đội hình, khu vườn và panel hoán đổi. Mặc định ẩn panel, bấm slot 2 mở, đóng được, chọn pet dự bị để cập nhật thẻ đang chọn. Hiển thị rõ slot 3 khóa cấp 35. |
| **09 · Đá Kỹ Năng** | Có năm ô hệ, bệ Mộc và bốn kỹ năng. Bộ chọn hệ là ảnh tĩnh. Mockup có khối đá đang trang bị riêng với biểu tượng, cấp, tên hiệu ứng và mô tả hồi phục. Thêm khối đó và mô tả ngắn từng kỹ năng. Mộc là mặc định, các hệ còn lại có trạng thái chọn và dữ liệu mẫu nếu cần duyệt chuyển trạng thái. |
| **10 · Leo Tháp** | Có đủ năm tháp, boss, khí độc, ba rương và nút đỏ. Thêm tầng và trạng thái mở/khóa trên từng thẻ. Chọn Mộc phải làm rõ tháp đang chọn, tầng 35F, boss, điều kiện môi trường và phần thưởng trong một panel. Chuyển cả màn về Phó bản. |
| **11 · Đấu Trường** | Đã có hạng, ba đối thủ và BOSS 48,5%. Bổ sung phần thông tin trận trên banner, phân cấp nút khiêu chiến theo độ khó và thêm phần thưởng/điểm trong khối boss theo mockup. Chuyển màn về Phó bản. |
| **12 · Nhiệm vụ** | Có bốn nhãn hạng, ba thẻ và thanh tiến độ. C/B/A/S chưa là nút lọc. Thêm thanh ngữ cảnh khu vực/hạng nếu theo ảnh gốc, chuyển trạng thái chọn và cập nhật danh sách mẫu khi lọc. Thẻ thể hiện rõ mục tiêu, tiến độ, thưởng và trạng thái chưa xong/nhận quà. |
| **13 · Xếp hạng** | Có ba bục, bảy dòng hạng và dòng cá nhân. Khu vực đang là nền và chữ, danh sách chưa có vùng cuộn độc lập, rương cá nhân chưa có nút. Thêm dropdown khu vực mẫu, vùng cuộn cho hạng 4–10, ghim dòng #28 ngoài vùng cuộn và đặt nút Xem Thưởng. |

Quy ước đề xuất khi duyệt lại là dùng ảnh gốc để chốt thứ tự và kích thước tương đối các khối, dùng phần mô tả Markdown cho điều hướng và trạng thái, dùng sprite hiện có cho phần minh họa. Với chỗ ảnh gốc và lời mô tả khác nhau, ghi rõ lựa chọn như Scene 02 và Scene 06. Các giá trị dùng để trưng bày UI. [Thiết kế Markdown](<C:/Users/HP/Downloads/home_scene_ui_ux_design.md>) · [AssetHome](../../Assets/Project/Sprite/AssetHome)

## 3. Thứ tự sửa và tiêu chí duyệt

1. **Sửa liên kết dock và tab con.** Đưa đúng Scene 05–09 và 10–11 về hai container. Dựng một thanh điều hướng mỗi nhóm, chia Pet thành hai cấp và giữ HUD/dock cố định. Khi bấm Nâng cấp phải thấy Nhân vật; khi bấm Phó bản phải thấy Leo Tháp. Bấm đi và về qua toàn bộ tab con, kể cả Mua Pet ↔ Vườn Pet. Mỗi lần chỉ có một màn nội dung hiển thị, nút được chọn đổi trạng thái ngay.
2. **Duyệt Nâng cấp trước.** Sửa Scene 05 theo chuỗi khung nhân vật ở trên → chỉ số → banner. Áp dụng thanh tab đã chốt cho Scene 06–09. Kiểm tra đổi preview trang phục, chọn thẻ pet, mở và đóng panel hoán đổi bằng slot 2 và chọn hệ đá. Kiểm tra tab và panel ở cả khổ 9:16 và màn dọc dài hơn để tránh chữ tràn hoặc dock che nội dung.
3. **Chỉnh các nhóm còn lại theo bảng.** Rà Trang chủ và popup, bố cục Cửa hàng, sau đó Phó bản và Thành tích. Dùng lại sprite, nền nút, thẻ, slot và panel đã có. Ưu tiên luồng xem trước có thể kiểm tra được như xem tỷ lệ mẫu, lọc nhiệm vụ, chọn khu vực và xem thưởng.
4. **Kiểm thử bằng đường bấm thật.** Chạy Play Mode từ Trang chủ, dùng Game View click trực tiếp vào nhãn/icon dock và các tab, kiểm tra trạng thái hiển thị, quay lại từ từng nhóm và thử bấm liên tiếp. Mở/đóng popup, kiểm tra lớp tối chặn bấm xuyên, cuộn bảng xếp hạng rồi xác nhận dòng #28 vẫn đứng yên. Chụp các màn 05–09 và các luồng đã sửa để đối chiếu lại với ảnh gốc. Kiểm tra nút Xuất Phát giữ đích đã cấu hình.
