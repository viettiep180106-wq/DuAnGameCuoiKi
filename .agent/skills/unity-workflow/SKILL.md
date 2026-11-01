---
name: unity-workflow
description: Bộ quy trình làm việc chuẩn cho dự án Unity. Cung cấp các lệnh (/spec, /plan, /build, /test, /review, /ship) để kiểm soát Agent lập kế hoạch, code và kiểm thử từng bước.
---

# Unity Project Workflow

Khi người dùng gọi các lệnh sau, hãy tuân thủ nghiêm ngặt quy trình:

- **`/spec` (Định nghĩa):** Đọc Game Design Document (GDD). Phân tích yêu cầu, xác định các lớp (classes) và cấu trúc kiến trúc cần thiết. KHÔNG viết code ở bước này.
- **`/plan` (Lên Kế Hoạch):** Chia nhỏ yêu cầu từ `/spec` thành các task nhỏ (atomic tasks). Mỗi task chỉ giải quyết 1 vấn đề cụ thể (ví dụ: Task 1 - Tạo UI Root, Task 2 - Viết Controller).
- **`/build` (Xây Dựng):** Viết code cho một task duy nhất đã được lên kế hoạch. TUÂN THỦ TẤT CẢ CÁC LUẬT TRONG `.agent/rules/`.
- **`/test` (Kiểm Thử):** Viết hoặc hướng dẫn chạy Unit/PlayMode test (áp dụng cấu trúc AAA) để chứng minh đoạn code ở bước `/build` hoạt động đúng.
- **`/review` (Đánh Giá):** Rà soát lại đoạn code vừa viết xem có tối ưu chưa, có vi phạm chuẩn C# của Unity không (đặc biệt lưu ý hiệu năng và quản lý rác - GC).
- **`/ship` (Hoàn Tất):** Xác nhận code đã sẵn sàng và chuyển sang task tiếp theo trong kế hoạch.
- **`/build auto` (Chế độ tự động):** Tự động lập kế hoạch và thực thi từng task có kèm kiểm thử. Chỉ chuyển sang task tiếp theo khi test trước đó đã vượt qua (pass).