# Plan Nâng Cấp Customer Flow: Cart UI + Call Checkout

## Summary
- Chia làm 2 task độc lập nhưng dùng chung hạ tầng hiện có: customer flow hiện tại, `sessionStorage` cart và SignalR hub `/orderHub`.
- Mục tiêu là giữ nguyên nghiệp vụ order/session, chỉ nâng cấp UX ở `Menu` và thêm realtime `Call checkout` giữa `Tracking` và `Table`.

## Task 1: Sửa Cart ở `Menu`
- Đổi cart từ panel cố định thành `floating cart button` bám màn hình, có badge số lượng món realtime.
- Khi bấm cart icon:
  - Desktop mở `drawer` trượt từ phải.
  - Mobile mở `bottom sheet` từ dưới lên.
- Giữ nguyên dữ liệu cart trong `sessionStorage`; không đổi contract submit order hiện tại.
- `Menu` page chỉ còn catalog chính; cart drawer chịu trách nhiệm:
  - render danh sách món
  - tăng/giảm/xóa món
  - hiển thị tổng tiền
  - nút `Submit Order`
- Khi cart rỗng:
  - badge ẩn hoặc về `0`
  - drawer hiển thị empty state rõ ràng
- Khi đổi `sessionId` trong URL, tiếp tục giữ logic xóa cart cũ như hiện tại.
- Bổ sung localized strings cho cart drawer: open/close cart, empty cart, total, submit, quantity actions.
- CSS cập nhật theo hướng:
  - thêm FAB nổi ở góc phải dưới
  - thêm backdrop cho drawer
  - khóa scroll nền khi drawer mở
  - giữ trải nghiệm tốt trên mobile và desktop

### Public/UI changes
- `Menu` không còn cart panel luôn mở.
- Thêm UI state mới cho page: `cart open/closed`.
- Không đổi API `POST /api/order/submit`.

## Task 2: Thêm `Call Checkout` giữa `Tracking` và `Table`
- Trên `OrderPage/Tracking` thêm nút `Call checkout`.
- Nút này gọi API mới:
  - `POST /api/order/call-checkout?sessionId={id}`
- API xử lý:
  - validate session tồn tại và đang active
  - lấy `tableId`, `tableName`
  - tạo hoặc cập nhật trạng thái `checkout requested`
  - broadcast SignalR event mới qua `/orderHub`
- Dùng một tracker phía server cho yêu cầu checkout đang mở, dạng singleton hoặc in-memory service keyed theo `sessionId` hoặc `tableId`, để:
  - page `Table` load lại vẫn thấy request đang chờ
  - request chỉ bị xóa khi staff mở bill của bàn đó
- Hub events mới:
  - `CheckoutRequested`
  - `CheckoutRequestCleared`
- `Tracking` page:
  - chuyển phần script inline sang JS riêng để dễ quản lý SignalR + button state
  - sau khi call thành công, disable nút hoặc đổi sang trạng thái “Checkout requested”
  - nếu call lại cùng session trong lúc request còn mở thì không tạo duplicate state
- `Table/Index`:
  - thêm kết nối SignalR
  - hydrate initial checkout-request state từ server vào `pageDataJson`
  - khi nhận `CheckoutRequested`, table card tương ứng được highlight realtime
  - hiển thị badge hoặc note kiểu “Checkout requested”
  - thêm action rõ ràng để staff mở bill/checkout ngay từ card đó
- Rule clear:
  - khi staff bấm vào bill của bàn đó qua flow `Table -> Bill`, request được clear ở server và broadcast `CheckoutRequestCleared`
- Staff UX trên `Table`:
  - highlight là trạng thái chính
  - chưa thêm âm báo ở đợt này
  - không chỉ dùng toast; trạng thái phải bám trên card đến khi được clear

### Public/API/interface changes
- Thêm endpoint mới: `POST /api/order/call-checkout`
- Thêm SignalR events: `CheckoutRequested`, `CheckoutRequestCleared`
- Mở rộng `tableManagementData` để chứa checkout request state hiện tại và localized strings liên quan
- Không đổi contract `GET /api/order/status`

## Test Plan
- Cart:
  - Menu load bình thường, cart icon luôn bám màn hình khi scroll
  - badge cập nhật đúng khi thêm/tăng/giảm/xóa món
  - drawer mở/đóng tốt trên desktop và mobile
  - submit order từ drawer vẫn gọi đúng API và chuyển sang `Tracking`
  - đổi sang session khác thì cart cũ bị reset như logic hiện tại
- Call checkout:
  - tại `Tracking`, bấm `Call checkout` gửi request thành công cho session active
  - `Table/Index` đang mở nhận realtime và highlight đúng bàn
  - reload `Table/Index` vẫn còn thấy request đang mở
  - staff bấm `Bill` của đúng bàn thì request bị clear và các client `Table` khác cũng update realtime
  - gọi `Call checkout` cho session không hợp lệ hoặc đã đóng trả lỗi phù hợp
  - bấm lặp lại nhiều lần không tạo nhiều request trùng cho cùng một session hoặc table
- Regression:
  - `OrderStatusUpdated` trên `Tracking` vẫn hoạt động
  - `OrderSubmitted` và kitchen flow hiện tại không bị ảnh hưởng
  - `Transfer`, `Merge`, `End Session` trên `Table` không bị vỡ UI khi có SignalR mới

## Assumptions / Defaults
- Cart chỉ đổi UI, chưa chuyển sang server-side cart.
- Checkout request là trạng thái vận hành tạm thời, lưu in-memory phía server; chưa thêm cột DB ở đợt này.
- Một session chỉ có tối đa một checkout request đang mở.
- Clear request xảy ra khi staff mở bill từ trang `Table`, không chờ tới lúc thanh toán hoàn tất.
- Tiếp tục dùng `orderHub` hiện có, không tách hub mới.
