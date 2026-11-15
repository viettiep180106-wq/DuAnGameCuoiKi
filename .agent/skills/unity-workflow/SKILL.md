---
name: unity-workflow
description: Bộ quy trình làm việc chuẩn cho dự án Unity. Cung cấp các lệnh (/spec, /plan, /build, /test, /review, /ship) để kiểm soát Agent lập kế hoạch, code và kiểm thử từng bước.
---

# Unity Project Workflow

Khi người dùng gọi các lệnh sau, hãy tuân thủ nghiêm ngặt quy trình:

- **`/spec` (Định nghĩa):** Đọc Game Design Document (GDD). Phân tích yêu cầu, xác định các lớp (classes) và cấu trúc kiến trúc cần thiết*(phân tích xem project hiện tại đã có những tính năng gì liên quan đến việc đang task đang cần làm unity hỗ trợ gì để tôi ưu việc viết code hơn không)=> MỤC TIÊU NGOÀI TRỰC QUAN HÓA THÌ CÒN CẦN TÌM CÁCH LÀM TỐI ƯU NHẤT. KHÔNG viết code ở bước này.
- **`/plan` (Lên Kế Hoạch):** Chia nhỏ yêu cầu từ `/spec` thành các task nhỏ (atomic tasks). Mỗi task chỉ giải quyết 1 vấn đề cụ thể (ví dụ: Task 1 - Tạo UI Root, Task 2 - Viết Controller).
- **`/build` (Xây Dựng):** Viết code VÀ LÀM VFX HAY HIỆU ỨNG UI/UX(MỤC THIẾT KẾ NÀY HÃY SỬ DỤNG KĨ NĂNG CỦA `.agent/skills/unity-skills`) cho một task duy nhất đã được lên kế hoạch. TUÂN THỦ TẤT CẢ CÁC LUẬT TRONG `.agent/rules/` KHI SỬ DỤNG CÁC KĨ NĂNG UNITY.
- **`/test` (Kiểm Thử):** Viết hoặc hướng dẫn chạy Unit/PlayMode test (áp dụng cấu trúc AAA) để chứng minh đoạn code ở bước `/build` hoạt động đúng.
- **`/review` (Đánh Giá):** Rà soát lại đoạn code vừa viết xem có tối ưu chưa, có vi phạm chuẩn C# của Unity không (đặc biệt lưu ý hiệu năng và quản lý rác - GC).
- **`/ship` (Hoàn Tất):** Xác nhận code đã sẵn sàng và chuyển sang task tiếp theo trong kế hoạch.
- **`/build auto` (Chế độ tự động):** Tự động lập kế hoạch và thực thi từng task có kèm kiểm thử. Chỉ chuyển sang task tiếp theo khi test trước đó đã vượt qua (pass).