# Báo cáo nghiên cứu tách asset UI HomeScene

Phạm vi gồm 13 mockup PNG đính kèm. Yêu cầu áp dụng là ưu tiên asset gốc, lấy một mẫu cho các ô cùng kiểu trong cùng mục UI, bỏ chữ khỏi ảnh xuất và tách icon hoặc avatar khỏi ô hay khung chứa. Tên tab, số liệu, giá và các nhãn xuất hiện trong mockup là dữ liệu tham chiếu để nhận diện vùng ảnh. Báo cáo chốt phương án trước khi tách.

## Chọn nguồn và loại bỏ bản lặp

1. Kiểm tra asset đã phân lớp hoặc sprite trong dự án trước. Nguồn phù hợp cần đủ nét ở kích thước hiển thị, có viền sạch và cho phép thay nội dung trong khung. Nếu thiếu, chọn vùng nguyên vẹn, ít bị che nhất trong 13 ảnh làm mẫu phục dựng.
2. Mỗi kiểu khung trong một nhóm giao diện dùng một asset gốc. Nhiều dòng, nhiều ô hoặc nhiều mốc sẽ dùng lại cùng mẫu, kể cả giữa các màn nếu hình dáng và vai trò tương đương. Kích thước khác nhau có thể dùng khung co giãn khi góc và viền giữ được hình dạng.
3. Đổi màu, chọn, khóa, hoàn thành và thông báo nên lắp bằng màu hoặc lớp phủ dùng chung nếu giữ được hình ảnh. Tạo thêm sprite khi đường viền, chất liệu hoặc hình minh họa khác rõ. Ví dụ, sao đầy và sao rỗng là hai hình dùng chung, còn năm vị trí sao chỉ là năm lần đặt lại các hình đó.
4. Xuất icon và avatar riêng với nền trong suốt. Khung, nền và viền chọn đứng riêng để thay avatar, đổi độ hiếm hoặc tái sử dụng ô. Chữ, giá, số lượng, cấp độ, phần trăm, thời gian, thứ hạng và các ký tự như C/B/A/S, II, dấu cộng hoặc dấu hỏi được gỡ khỏi ảnh và dựng lại ở lớp UI khi cần. Biểu tượng hình học như dấu tích, khóa, mũi tên và tia sét có thể dùng sprite riêng.

### Nguồn dùng chung ưu tiên

| Nhóm | Asset gốc và nguồn đề xuất | Cách dùng lại |
| --- | --- | --- |
| Nền | Kiểm tra `BackGroundHome.png` trong dự án. Popup dùng lớp phủ tối riêng. | Dùng nền sạch làm cảnh chung. Nếu cần biến thể sáng hoặc tối, xử lý trên lớp hiển thị. [Nền Home hiện có](../../Assets/Project/Sprite/AssetHome/BackGroundHome.png) |
| HUD | Đối chiếu `bar_player_stat_wood.png` và `avatar_circle_capybara.png`. Lấy `shop-currency` làm tham chiếu cho một nền ô tài nguyên chuẩn và bộ icon vàng, ngọc, thể lực nếu kho hiện tại thiếu. | Một mẫu ô lặp ba lần. Khung avatar tròn, chân dung, nút thêm và nhãn cấp là các lớp riêng. Sprite avatar có sẵn cần kiểm tra xem chân dung đã dính khung hay chưa. [Kho sprite hiện có](../../Assets/Project/Sprite/GeneratedUI) · [HUD tham chiếu](shop-currency.png) |
| Thanh điều hướng | Dùng dock của `start-main` làm mẫu cho nền gỗ, lớp chọn vàng và năm icon quầy hàng, khiên, nhà, tháp, cúp. | Mỗi icon lấy một bản chuẩn cho dock. Lớp chọn có thể chuyển vị trí. Nhãn và tab đang chọn lấy theo màn thực tế, vì các mockup thể hiện khác nhau. [Dock tham chiếu](start-main.png) |
| Tab, nút và panel | Dùng tab vàng và tab giấy ở `dungeon-tower` làm mẫu chính. Đối chiếu sprite nút và giấy cuộn có sẵn với các panel tham chiếu. | Dùng chung theo kiểu hình và chất liệu. Nền nút để trống, icon thao tác và chấm báo là lớp riêng. Chỉ giữ biến thể riêng khi đổi màu hoặc co giãn làm mất nét đặc trưng. [Tab tham chiếu](dungeon-tower.png) · [Kho sprite hiện có](../../Assets/Project/Sprite/GeneratedUI) |
| Tiến độ, trạng thái và chỉ số | Kiểm tra `track_progressbar_bg.png`, `track_progressbar_fill.png`, `milestone_completed_check.png` và icon kiếm, tim, khiên, khóa ở `GeneratedUI`. | Thanh tiến độ gồm nền và phần lấp có chiều dài thay đổi. Mốc, chấm báo và dấu tích dùng lại. Phân biệt kiếm đơn chỉ số và cặp kiếm khiêu chiến nếu cả hai được dùng. [Kho sprite hiện có](../../Assets/Project/Sprite/GeneratedUI) |

## Danh mục theo từng màn

Mỗi dòng chỉ liệt kê phần cần nghiên cứu ở màn đó. HUD, dock và các thành phần chung lấy theo bảng trên.

| Màn tham chiếu | Lấy mẫu gì | Gộp và tách lớp thế nào |
| --- | --- | --- |
| [Trang chủ](start-main.png) | Capy pháp sư toàn thân, rương chờ, cụm giấy chương, biểu tượng lối tắt và các mẫu rương mốc có hình riêng. | Một đế lối tắt cho quà, lễ hội, thẻ, vòng quay và tiết kiệm. Đồng hồ tách khỏi bong bóng giờ. Mốc rương dùng chung thanh, nhãn số và dấu tích. Capy toàn thân và avatar là hai cách thể hiện cần đánh giá riêng. |
| [Gói tân thủ](start-iap.png) | Khung popup giấy, ruy băng đỏ, minh họa rương mở, icon kiếm cà rốt và vé tím. | Một ô thưởng cho cả ba phần quà. Dùng lại icon cụm ngọc nếu cùng hình. Tách minh họa khỏi giấy và tia sáng, tách đồng hồ khỏi nền timer và icon vàng khỏi nền nút giá. |
| [Cửa hàng tiền tệ](shop-currency.png) | Mái bạt và khung gian hàng, banner quỹ khởi đầu, ba mẫu ô gồm quà ngày, gói ngọc và dòng đổi vàng. | Ba ô quà ngày dùng một mẫu, bốn gói ngọc dùng một mẫu, ba dòng đổi dùng một mẫu. Gói ngọc giữ riêng hình cụm ngọc, túi và các kiểu rương nếu khác hình. Nút giá, nút nhận và chấm báo dùng lại tại mọi ô cùng kiểu. |
| [Gacha](shop-gacha.png) | Mâm vòng quay, kim chỉ, cụm tâm, bảng bảo đảm, bảng tỷ lệ và icon phần thưởng đặc trưng. | Mâm quay sạch icon để ghép icon vào từng cung, kim đứng riêng, rương tâm riêng. Một mẫu dòng tỷ lệ cho bốn hạng, một mẫu ô thưởng nổi bật cho bốn món, một nền nút cho hai nút quay nếu co giãn phù hợp. Các mốc bảo đảm lặp nền tròn và dấu tích. |
| [Nâng cấp nhân vật](upgrade-character.png) | Capy pháp sư, giấy cuộn chỉ số và banner sự kiện. Đối chiếu hình Capy với Trang chủ trước khi giữ thêm ảnh. | Bốn dòng chỉ số dùng một nền và các icon riêng. Tách lớp nhân vật, nền sự kiện và ruy băng khỏi chữ. Dùng lại một hình mũi tên lật ngang nếu cặp nút chuyển nhân vật đối xứng. |
| [Nâng cấp trang phục](upgrade-costume.png) | Vịt Hoàng Gia toàn thân, bệ, chân dung dùng ở ô nâng cấp, icon cuộn chỉ và các icon chức năng trang phục. | Chân dung tách khỏi ô vuông, bệ tách khỏi nhân vật. Dùng lại sao đầy và sao rỗng ở cả hàng sao hiện tại và hàng dự kiến. Bốn tab ghép nền chuẩn với icon riêng. |
| [Mua thú cưng](upgrade-pet-buy.png) | Một mẫu thẻ pet, viền chọn, biểu tượng độ hiếm, tám hình pet, minh họa Hồ Ly Lửa và icon Bão Lửa. | Tám thẻ lắp từ một cấu trúc gồm khung, chân dung, nhãn và đá độ hiếm. Giữ đường nét viền thẻ theo mẫu, tạo màu riêng khi cần. Kiểm tra hình hồ ly trong thẻ và hình lớn theo góc vẽ, chi tiết và kích thước trước khi giữ hai bản. |
| [Vườn thú cưng](upgrade-pet-garden.png) | Mẫu slot xuất chiến, viền nét đứt, lớp xích và khóa, hình pet trong vườn, mẫu khung chân dung và ô dự bị. | Hai ô dự bị trống dùng chung một mẫu. Ghép ô thêm pet và ô khóa từ nền kèm lớp phủ phù hợp. Tái dùng chân dung Rồng Lửa Nhỏ giữa bảng chi tiết và hàng dự bị nếu hình khớp. Đối chiếu cáo, rồng, thỏ và rùa trong vườn với chân dung trước khi tách thêm. |
| [Đá kỹ năng](upgrade-skill-stone.png) | Năm icon hệ, đá Mộc cỡ lớn, bệ, hiệu ứng lá phát sáng, bốn icon kỹ năng. | Năm nút hệ dùng một mẫu ô và lớp chọn. Bốn dòng kỹ năng dùng một mẫu dòng kèm nút nâng cấp hoặc trạng thái khóa. Đá lớn và bệ tách riêng để đổi hệ. |
| [Tháp](dungeon-tower.png) | Năm icon tháp, minh họa Cây Cổ Thụ, icon Sương Độc và các rương chưa có trong bộ chung. | Năm tháp dùng một đế nhãn với lớp chọn riêng. Ba rương dùng một ô thưởng. Tên tháp, số tầng, mô tả và chi phí thể lực là UI. |
| [Đấu trường](dungeon-arena.png) | Khung đối thủ, ba chân dung, huy hiệu hạng, minh họa rồng boss và ruy băng boss. | Ba đối thủ dùng một dòng, một khung avatar và một nút khiêu chiến. Tách chữ II và nhãn hạng khỏi huy hiệu. Boss, icon tim, nền thanh máu, phần lấp và số phần trăm là các lớp riêng. |
| [Nhiệm vụ](achievement-missions.png) | Một mẫu thẻ nhiệm vụ, một ô thưởng, cờ hạng và ba hình nhiệm vụ nếu là ba minh họa riêng. | Ba nhiệm vụ dùng một thẻ và một bộ thanh tiến độ. Cờ S dùng lại, ký tự đặt ở lớp chữ. Tiền và ngọc thưởng dùng icon chung nếu cùng kiểu. Dòng hoàn thành ghép dấu tích và nút nhận từ bộ chung. |
| [Xếp hạng](achievement-ranking.png) | Một mẫu dòng xếp hạng, nền bậc bục, các chân dung top 3, nền nhấn hạng cá nhân và icon khu vực. | Bảy dòng hạng 4–10 dùng một mẫu. Tách avatar, điểm, cúp và hạng. Bục có thể dùng một hình cơ sở nếu đổi màu và kích thước giữ được thiết kế, chi tiết vòng lá hoặc bậc khác hình thì giữ biến thể riêng. Nhãn dưới bục dùng cùng kiểu và lớp nhấn cho hạng nhất. |

## Phục dựng vùng bị ghép trong mockup

- Với ô đã phủ icon và chữ, tìm cùng kiểu ở màn khác để chọn cạnh và góc nguyên vẹn. Nếu thiếu vùng sạch, phục dựng phần nền hoặc khung và ghi rõ nguồn trong danh mục. Trường hợp dễ gặp là nền thẻ pet, khung chân dung và mâm Gacha.
- Với avatar bị khung cắt, chỉ dùng phần thấy được cho kiểu chân dung có mặt nạ tương ứng. Nếu cần hiện ngoài khung hoặc ở tỷ lệ lớn, ưu tiên hình gốc phân lớp hoặc phục dựng phần khuất. Tránh dùng một ảnh crop nhỏ cho cả chân dung và minh họa toàn thân.
- Giữ bóng, hiệu ứng và đồ trang trí đi cùng hình khi chúng luôn được dùng chung. Tách riêng khi cần chuyển động, đổi trạng thái hoặc lắp lại, như kim Gacha, tia sáng đá Mộc và viền chọn thẻ pet.

## Trình tự tách và tiêu chí duyệt

1. So khớp các sprite trong dự án với ảnh tham chiếu. Ghi cho mỗi asset một tên, một nguồn, nhóm UI dùng nó và quyết định dùng lại, tách hoặc phục dựng. Các sprite có sẵn trong báo cáo là ứng viên cần đối chiếu chất lượng và cấu trúc lớp.
2. Làm bộ dùng chung trước. Dùng các nhóm nhiều phần lặp để thử ghép, gồm dòng nhiệm vụ, thẻ pet và ô cửa hàng. Khi thay icon, chữ và trạng thái trên cùng một nền mà bố cục vẫn đúng, dùng mẫu đó cho các ô tương đương.
3. Tách minh họa và icon riêng của từng màn sau khi bộ dùng chung đã được đối chiếu. Kiểm tra chéo các cặp Trang chủ và Nâng cấp nhân vật, Mua pet và Vườn pet, Tháp và Đấu trường, Nhiệm vụ và Xếp hạng trước khi tạo sprite mới.
4. Duyệt ảnh dự kiến xuất dưới dạng PNG RGBA đối với thành phần cần nền trong suốt. Soi viền trên cả nền sáng và tối, kiểm tra phần chữ đã được xóa sạch và thử ghép avatar với khung độc lập. Với slot hoặc nút co giãn, thử mẫu ở kích thước nhỏ nhất và lớn nhất dự kiến. Chốt danh mục và số lượng sau khi xác nhận một mẫu cho mỗi kiểu lặp.
