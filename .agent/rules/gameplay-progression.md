---
description: "Gameplay Progression, Wave Preview, Distance-based Combat, Energy Core & Board Sequence"
globs: "**/GamePlay/**, **/Camera/**, **/UI/**, **/Board/**"
alwaysApply: true
---

# Gameplay Progression & Wave Lifecycle Rules

## 1. Wave Opening Sequence Order (Thứ Tự Mở Đầu Wave Chuẩn)
Mỗi Wave (từ Wave 1 đến Wave 10) PHẢI tuân thủ chính xác thứ tự tuần tự sau:
1. **Bước 1 - Camera Wave Preview (Chiếu camera soi quái)**:
   - Quái vật của Wave được sinh ra ở cự ly phía trước (`targetX - distance`).
   - Bàn cờ match-3 (`Board`) PHẢI được giữ ẩn ở bên dưới màn hình (`anchoredPosition.y = -1600f`).
   - `CameraWavePreview` thực hiện pan camera chậm rãi và êm ái (`panSpeed = 1.2f`, `Ease.InOutCubic`) từ vị trí Player sang soi bầy quái.
   - Dừng lại (`previewDuration = 1.5f`) để người chơi kịp quan sát số lượng, loại quái và hệ nguyên tố.
   - Pan mượt mà quay trở về vị trí Player ban đầu (`panSpeed = 1.2f`, `Ease.InOutCubic`).
2. **Bước 2 - Hội Thoại Cốt Truyện / Tình Huống (`DialogueManager`)**:
   - **Lia camera xong MỚI ĐẾN hội thoại**: Sau khi camera quay về vị trí ban đầu hoàn tất, đoạn hội thoại mở đầu mới được phát trong Messenger chat box.
   - Từng bong bóng thoại xuất hiện tự động và cuộn xuống mượt mà.
3. **Bước 3 - Banner Thông Báo Độ Khó & Hệ Quái (`WaveIntroBannerUI`)**:
   - Sau khi kết thúc đoạn hội thoại, dải banner trượt vào giữa màn hình.
   - Hiển thị: Số Wave, Độ khó ("Dễ", "Thường", "Khó", "CỰC KHÓ (BOSS)"), Hệ nguyên tố của quái và hệ khắc chế.
   - Banner dừng lại 1.2s rồi mờ dần (`DOFade(0)`) và biến mất hoàn toàn.
4. **Bước 4 - Bàn Cờ Match-3 Trượt Lên (`BoardController.SlideInBoard`)**:
   - Sau khi banner biến mất hoàn toàn, bàn cờ match-3 mới trượt từ dưới lên giữa màn hình (`center.DOAnchorPosY(160f, 0.45f)`).
   - Trạng thái game chuyển sang `GamePlayState.MatchBoard`, kích hoạt hiển thị số coin và cho phép người chơi ghép đá.

---

## 2. Match-3 Board Rendering & Sibling Index Rules (Quy Tắc Hiển Thị Bàn Cờ)
1. **Tuyệt đối tách biệt container giữa Slot và Stone**:
   - Dưới `center` (Board) bắt buộc có 2 RectTransform container riêng biệt:
     - `SlotsContainer`: Sibling Index = 0 (luôn ở lớp dưới). Chứa toàn bộ `SlotStoneBase`.
     - `StonesContainer`: Sibling Index = 1 (luôn ở lớp trên). Chứa toàn bộ `StoneBase`.
2. **Đảm bảo chiều sâu Z = 0 & Scale = 1**:
   - Mọi `Slot` và `Stone` khi Instantiate hoặc Spawn qua `ObjectPoolingManager` phải có `localPosition.z = 0f` và `localScale = Vector3.one`.
   - `ResetContainerTransform()` bắt buộc cưỡng chế cả 2 container có `localScale = Vector3.one` và `sizeDelta = (width, height)`.
3. **Đá luôn nổi trên ô Slot**:
   - Mỗi viên đá khi spawn hoặc swap phải gọi `stone.transform.SetAsLastSibling()`.
   - Nhờ `StonesContainer` nằm trên `SlotsContainer`, không bao giờ xảy ra lỗi đá bị che khuất hoặc chìm dưới nền slot (`stone bị set dưới slot`).

---

## 3. Wave Progression & Progress Bar by Distance (Tiến Trình Wave & UI Fill Thanh Tiến Trình)
1. **Không kết thúc wave theo số lượng quái**:
   - Wave KHÔNG kết thúc khi quái vật bị tiêu diệt hết (`numEnemy <= 0`).
   - Wave kết thúc khi **Người chơi di chuyển đủ quãng đường `distance`** quy định trong `StepData` của Wave đó.
2. **Thanh UI Fill tiến trình (`LevelProgressBarUI`)**:
   - Thanh tiến trình có đường ray nền (`ProgressBarTrack`) và thanh fill trực quan (`ProgressFill` với `Image.Type = Filled`).
   - Cập nhật fill mượt mà realtime theo công thức:
     $$\text{OverallFill} = \frac{(\text{CurrentWave} - 1) + \frac{\text{CurrentDistance}}{\text{TargetDistance}}}{\text{TotalWaves}}$$
   - Nằm ngay dưới các nút tròn mốc milestone (các mốc tròn sáng lên và highlight khi fill chạm đến).
3. **Cơ chế đẩy lùi quái còn sống (Enemy Pushback)**:
   - Khi hết quãng đường của Step, quái vật còn sống KHÔNG bị xóa mà bị đẩy lùi ra xa (`EnemyController.PushBackLivingEnemies(5f)`).
   - Quái còn sống được giữ lại và gộp chung với đàn quái của Wave tiếp theo.

---

## 4. Energy Core Deduction Rules (Quy Tắc Trừ Lõi Năng Lượng)
- **Áp dụng cho TẤT CẢ các lần match**:
  - Mỗi cụm 3 viên đá được ghép hợp lệ (dù do người chơi chủ động tráo đá hay do đá rơi ngẫu nhiên tự động ghép tạo Cascade) đều trừ đúng **5 lõi năng lượng** (`Player.Instance.TrySpendEnergy(5)`).
  - Trừ theo số cụm: $\text{Số lõi trừ} = \max(1, \lfloor \text{Số đá} / 3 \rfloor) \times 5$.
- Nếu người chơi không đủ 5 lõi năng lượng, không cho phép chủ động tráo đá.
- Sau mỗi wave hoàn thành, thưởng 10 lõi năng lượng.

---

## 5. Game End Cleanup Rules (Dọn Dẹp Khi Kết Thúc Game)
- Khi Game Over (Thắng hoặc Thua):
  - Dừng toàn bộ Coroutines và các bộ đếm thời gian (`StopAllCoroutines()`, `CancelInvoke()`).
  - Hủy bỏ các Tween chuyển động của Camera và UI.
  - Tắt combat và ngắt toàn bộ các hoạt động sự kiện chuyển wave kế tiếp để không bị lặp wave sau khi game đã kết thúc.
