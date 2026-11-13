# Đánh giá Gomoku Tower – 2026-11-13

## 1. Verdict: GO (sát ngưỡng, 24/35)

Lý do mạnh nhất: gomoku là thị trường lớn nhưng đối thủ là app quảng cáo ít trau chuốt, còn hướng "board game cổ điển + roguelike" đang lên (Gambonanza, Shotgun King, Tic Tactic) mà mình không tìm thấy bản roguelike dành cho gomoku. Game đã có engine, Tower, relic, AI boss và công cụ cân bằng. Điểm yếu lớn nhất là bản chạy được chưa kiểm chứng (chưa build, chưa đo dung lượng, độ khó Tower dốc).

## 2. Core loop (một câu)
Chọn luật và đối thủ → đặt quân XO để nối 5 (có kỹ năng, relic, giới hạn lượt) → thắng thì nhận xu/relic và lên tầng kế (15 tầng), thua thì mất tim → hết tim thì run kết thúc và bắt đầu lại với seed mới.

## 3. Scorecard (rubric 7 tiêu chí × 1–5)

| Tiêu chí | Điểm | Lý do (có dẫn chứng) |
|---|---|---|
| Simplicity (độ dễ build cho creator) | 3 | ~15.300 dòng C#, 6 luật, Tower, relic, shop, quảng cáo, 27 test. Không cần backend/multiplayer nhưng nhiều hệ thống. |
| Content cost (chi phí món thứ 100) | 3 | 15 relic và 15 boss viết tay; có seed, omen sinh theo seed, `BalanceSim` đo win-rate từng relic, puzzle có solver kiểm. Nhưng ROADMAP ghi boss Elo 2400 chỉ thắng 0–10% ở tầng 6–15 nên cân bằng chưa xong. |
| Virality (khả năng lan truyền) | 2 | Clip 3 giây không tiếng: bàn 7×7 tự đánh đọc được nhưng bình thường, chưa nhìn thấy điểm roguelike. Chưa có kết quả để chia sẻ (daily seed mới nằm trong kế hoạch Phase 3). |
| Monetization (kiếm tiền) | 4 | 14 chỗ gọi quảng cáo thưởng (hồi sinh, x2 xu, reroll relic, quay thưởng, khôi phục streak...). Chưa có IAP/no-ads và chưa có interstitial, nên không đủ 5. |
| Competition gap (khoảng trống đối thủ) | 4 | Đối thủ gomoku là app quảng cáo lâu năm (vndynapp 7,8 triệu+ lượt cài, 3,63★ trên 31 nghìn đánh giá). Mình không tìm thấy gomoku roguelike trong các lượt tìm; Tic Tactic (tic-tac-toe deckbuilder, Steam) là bản gần nhất nhưng khác bàn cờ và nền tảng. |
| Trend timing (thời điểm) | 4 | "Cờ cổ điển + roguelike kiểu Balatro" đang lên: Gambonanza (Gambits như Joker, ra iOS/Android 2026), Shotgun King lên mobile (tháng 2/2026). Cửa sổ ≥3 tháng là suy đoán của mình, chưa có số liệu. |
| Creator fit (hợp với người làm) | 4 | Chưa có file hồ sơ creator; mình giả định solo, mạnh lập trình/tự động hóa, yếu art, ít ngân sách. Game dùng sprite sinh bằng mã, engine tách khỏi Unity, có `BalanceSim` và solver. |
| **Tổng** | **24/35** | Không có điểm 1. |

Hai chỉ số bổ sung (game đang làm dở):
- **Khoảng cách tới MVP phát hành được:** ước tính 7–14 ngày (mở Unity xử lý lỗi compile, build WebGL và đo dung lượng, chơi thử cả dọc/ngang, làm mềm độ khó tầng 6+, gắn SDK cổng game).
- **Phần tái dùng nếu đổi hướng:** `Core` không phụ thuộc Unity, `XOBossAI`, `BalanceSim`, solver puzzle, test, hệ sprite sinh bằng mã, hệ UI.

## 4. Đối thủ (so sánh)

| Tên | Nhà phát hành | Ra mắt / cập nhật | Vị trí / xu hướng | Kiếm tiền | Phàn nàn |
|---|---|---|---|---|---|
| [Gomoku - Caro - Renju](https://appgoblin.info/apps/com.vndynapp.carochess) | vndynapp (VN) | 09/2012 | 7,8 triệu+ lượt cài, ~407 nghìn MAU, 3,63★ (~31 nghìn đánh giá) | 100% quảng cáo, không IAP, ước ~$10 nghìn+/tháng (ước tính của AppGoblin) | Chưa đọc được review 1–3★ |
| [Gomoku V (iOS)](https://apps.apple.com/us/app/gomoku-v-5-in-a-line-game/id480115242) | — | cập nhật 23/12/2025 | 4,3★ nhưng chỉ 4 đánh giá ở US | Miễn phí + IAP ($1,99 Pro, gói "Ticketz") | Mô tả cho biết chế độ online đã bị gỡ từ bản 5.56 |
| [Tic Tactic](https://store.steampowered.com/app/3256850/Tic_Tactic/) | — | 18/09/2025 | 78% tích cực trên 23 đánh giá (Mostly Positive) | Trả phí $7,99 | Trang không nêu chủ đề review |
| [Gambonanza](https://www.pocketgamer.com/gambonanza/coming-soon-to-mobile) | Blukulele (mobile: ATEO) | 2026 | Được báo chí nhắc, bản mobile + PC | Bài viết không nêu | Chưa có phản hồi người chơi trong bài |
| [Shotgun King](https://www.pulse.bot/gaming/news/shotgun-king-is-chess-where-you-replace-your-army-with-a-shotgun-out-now-aec2a25b-f1f8-4784-9d25-435abeacc11b/) | Plug In Digital (mobile) | Mobile 02/2026 | Lên iOS/Android ([ixbt](https://ixbt.games/en/news/2026/02/08/plug-in-digital-vypustit-saxmatnyi-rogalik-shotgun-king-v-google-play-i-app-store.html)) | Chưa kiểm | Chưa kiểm |

Vòng đời: gomoku thuần là bão hòa; "board game cổ điển + roguelike" đang lên.

## 5. Khoảng trống có thể khai thác
1. Gomoku roguelike (tầng, boss, relic) gần như chưa có trên mobile/web theo kết quả tìm của mình; đối thủ chính chỉ là bàn cờ + AI + quảng cáo.
2. Chế độ online bị gỡ ở ít nhất một app (Gomoku V); chơi một mình có chiều sâu (Tower, daily seed) lấp chỗ trống đó.
3. Đối thủ gomoku thường chỉ có 1–2 luật; game này có 6 luật và puzzle có solver.

## 6. Điều chỉnh để tăng điểm
**Một thay đổi tác động lớn nhất:** làm mềm độ khó Tower ở tầng 1–5 cho người mới và hiện roguelike ngay trong 3 phút đầu (chọn relic sau trận 1). Cổng Poki loại game có giữ chân yếu trong 3 phút đầu; ROADMAP đã ghi tầng 6+ gần như bất khả thi. Mỗi lần chia sẻ được kết quả (daily seed + thẻ ảnh) sẽ nâng Virality từ 2 lên 3.

## 7. Kill switch (ngưỡng dừng thử nghiệm)
- Nguồn có sẵn: một developer nêu ngưỡng Poki Player Fit Test là 125/500 (25%) người chơi ít nhất 3 phút (theo [Cinevva](https://app.cinevva.com/guides/publish-game-poki); đây là một nguồn, chưa kiểm với tài liệu Poki).
- Đề xuất của mình (chưa có nguồn): nếu sau 2 vòng chỉnh vẫn dưới 25% ở ngưỡng 3 phút, hoặc bản WebGL không xuống dưới 8 MB, thì dừng gửi cổng và chuyển sang bản mobile.
- Giao `tracking-game-metrics` khi có số liệu thật.

## 8. Giả định và điều chưa biết
- Mình chưa chơi game; chưa compile hay build. Điểm dựa trên mã, ROADMAP và tìm kiếm.
- Chưa có hồ sơ creator (`game-trend-profile.md`); các giả định ở mục Creator fit là của mình.
- Dung lượng WebGL ≤5 MB (mục tiêu trong ROADMAP) chưa kiểm chứng; Poki yêu cầu tải đầu dưới ~8 MB theo Cinevva.
- Chưa đọc được review 1–3★ của đối thủ (chỉ có tiêu đề/trang tổng hợp), nên mục "phàn nàn" chưa đầy đủ.
- Tên "Gomoku Tower" chưa kiểm nhãn hiệu/kho ứng dụng.
- Số liệu doanh thu và MAU của AppGoblin là ước tính của bên thứ ba.
- Ước tính 7–14 ngày tới MVP là suy đoán.
