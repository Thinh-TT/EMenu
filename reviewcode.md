# REVIEW CODE EMENU - CHUẨN BỊ BÁO CÁO VẤN ĐÁP

## TỔNG QUAN DỰ ÁN

- **Name**: EMenu - Hệ thống quản lý nhà hàng
- **Tech Stack**:
  - Frontend: ASP.NET Core MVC + Razor Views + JavaScript (fetch API, SignalR, Chart.js)
  - Backend: ASP.NET Core Controllers + Application Services + Repository Pattern
  - Database: SQL Server + Entity Framework Core
  - Realtime: SignalR Hub `/orderHub`

---

## KIẾN TRÚC PHÂN LỚP

```
EMenu.Web (Presentation Layer)
    ├─ Controllers, Views, Hubs
    ├─ Giao tiếp với client
    └─ Authentication + Authorization

EMenu.Application (Business Logic Layer)
    ├─ 22+ Services xử lý nghiệp vụ
    └─ DTOs, Configurations

EMenu.Application.Abstractions (Contracts)
    ├─ Repository Interfaces
    ├─ Unit of Work Pattern
    └─ DTO Definitions

EMenu.Infrastructure (Data Access Layer)
    ├─ AppDbContext (EF Core)
    ├─ Repository Implementations
    ├─ Migrations
    └─ Seeding

EMenu.Domain (Business Models)
    ├─ Entities (24 models)
    ├─ Enums
    └─ Constants
```

---

## ENTITIES CHÍNH (24 Models)

**HR Management**:

- `User`, `Role`, `UserRole`, `Staff`, `Shift`, `ShiftLog`, `Timekeeping`, `Wage`

**Menu Management**:

- `Category`, `Product`, `ComboProduct`, `Ingredient`, `IngredientProduct`

**Operations**:

- `RestaurantTable`, `Customer`, `OrderSession`, `Order`, `OrderProduct`
- `Invoice`, `Payment`, `Receipt`, `ReceiptIngredient`, `Supplier`
- `Reservation`

---

## 7 FLOWS CHÍNH CẦN ĐỌC

### **Flow 1: Khách Hàng Qua QR** (Ngày 1)

**Quy trình**: Scan QR → Nhập info → Tạo Session → Chọn Menu → Gửi Order → Theo dõi trạng thái

**Code cần đọc**:

1. **Controllers**:
   - `Customer/Start` - QR entry point, tạo khách mới
   - `Menu/Index` - hiển thị menu sản phẩm, combo
   - `OrderPage/Tracking` - khách follow status món

2. **Services**:
   - `CustomerService.CreateCustomer()` - tạo khách từ form
   - `SessionService.OpenSession()` - mở phiên tại bàn
   - `OrderService.SubmitOrder()` - gửi order từ cart

3. **Frontend**:
   - `menu.js` - quản lý sessionId từ URL, add items nhanh
   - `cart.js` - sessionStorage cart management, display total
   - `tracking.js` - realtime order status

4. **Realtime (SignalR)**:
   - `OrderSubmitted` - thông báo bếp nhận order mới
   - `OrderStatusUpdated` - cập nhật trạng thái từ bếp
   - `NewOrder` - danh sách order cho bếp

**Key Points**:

- Session liên kết với Table + Customer
- Cart lưu trong sessionStorage, không server-side
- Realtime push trạng thái từ Kitchen → Tracking

---

### **Flow 2: Staff/Admin Tại Bàn** (Ngày 1)

**Quy trình**: Mở bàn → Xem bill → Thanh toán (Cash/VNPay) → Đóng bàn

**Code cần đọc**:

1. **Controllers**:
   - `Table/Index` - dashboard quản lý trạng thái bàn
   - `Payment/Cash` - thanh toán tiền mặt
   - `Payment/VNPay` - thanh toán VNPay

2. **Services**:
   - `TableService.OpenTable()`, `.CloseTable()` - cập nhật trạng thái bàn
   - `BillService.GetBill()` - tính tổng hóa đơn
   - `PaymentService.ProcessPayment()` - xử lý thanh toán
   - `SessionService.EndSession()` - đóng phiên

3. **UI**:
   - `table.js` - trạng thái bàn realtime, modal transfer/merge
   - `bill.js` - tính tiền, checkout button

4. **SignalR Events**:
   - `Callcheckout` - khách gọi thanh toán từ Tracking

**Key Points**:

- Trạng thái bàn: Available → Occupied → Available
- Bill tính từ Order items chưa invoiced
- Transaction để update Table + Session + Invoice + Payment

---

### **Flow 3: Kitchen/Chuẩn Bị** (Ngày 1-2)

**Quy trình**: Nhận order → Chuẩn bị → Cập nhật trạng thái → Phục vụ → Xác nhận

**Code cần đọc**:

1. **Controllers**:
   - `Kitchen/Index` - dashboard hiển thị pending items
   - `api/kitchen/update-status` - update status endpoint

2. **Services**:
   - `KitchenService.GetPendingItems()` - lấy danh sách chưa làm
   - `KitchenService.UpdateOrderStatus()` - transition trạng thái
   - `OrderService.GetOrdersByStatus()` - filter theo status

3. **Realtime Workflow**:
   - `NewOrder` - bếp nhận order mới (từ OrderSubmitted)
   - `OrderStatusUpdated` - broadcast khi status thay đổi
   - Tracking page + Table page update realtime

4. **Status Workflow**:

   ```
   Pending → Preparing → Ready → Served

   ```

5. **UI**:
   - Pending items card, button transition status
   - Cancel order option
   - Realtime badge/highlight

**Key Points**:

- Một Order có nhiều OrderProduct, mỗi cái có status riêng
- Realtime push rất quan trọng để Tracking cập nhật

---

### **Flow 4: Thanh Toán VNPay** (Ngày 2)

**Quy trình**: Khách chọn VNPay → Tạo payment URL → Redirect sandbox → Giao dịch → Callback → Cập nhật DB

**Code cần đọc**:

1. **Controllers**:
   - `PaymentController.VNPay()` - khởi tạo giao dịch, tạo URL
   - `PaymentController.VNPayReturn()` - callback handler từ sandbox

2. **Services**:
   - `VNPayService.CreatePaymentUrl()` - build URL cùng signature
   - `VNPayService.ValidateCallback()` - xác minh hash callback
   - `PaymentService.HandleVNPayCallback()` - update DB nếu thanh toán thành công

3. **Configuration**:
   - `VNPayConfig` - TmnCode, HashSecret, Url, ReturnUrl từ appsettings.json
   - Checkout request tracker - dùng `/orderHub` event

4. **Reference**:
   - [pvnpay.md](pvnpay.md) - Sandbox debug checklist

5. **Key Validation**:
   - `vnp_ResponseCode == 00` && `vnp_TransactionStatus == 00` → thành công
   - Verify hash từ callback parameters
   - Idempotent: không tạo duplicate Payment/Invoice nếu callback repeat

**Key Points**:

- ReturnUrl phải khớp scheme (http/https) và port chính xác
- Hash validation: HMAC SHA512 từ tất cả parameters `vnp_*` (trừ Hash fields)
- Flow thành công: Invoice + Payment → Order.Status = Completed → Session close
- Callback có thể retry → cần idempotency

---

### **Flow 5: Nhập Hàng (Procurement)** (Ngày 2-3)

**Quy trình**: Quản lý NCC → Tạo phiếu nhập → Thêm items nguyên liệu → Cập nhật tồn kho

**Code cần đọc**:

1. **Controllers**:
   - `Procurement/Index` - CRUD suppliers
   - `Procurement/CreateReceipt` - form dynamic với nhiều line items
   - `Procurement/ReceiptHistory` - view lịch sử nhập

2. **Services**:
   - `ProcurementService.CreateSupplier()`, `.GetSuppliers()`
   - `ProcurementService.CreateReceipt()` - tạo receipt + receipt items
   - `InventoryService.UpdateStock()` - cập nhật Ingredient.StockQuantity

3. **Models**:
   - `Receipt` + `ReceiptIngredient` (many-to-many)
   - `Supplier` - thông tin NCC
   - `Ingredient` - nguyên liệu có StockQuantity

4. **Validation**:
   - Quantity, Price > 0
   - Supplier, Ingredient, Staff phải hợp lệ
   - Transaction để update Receipt + Ingredients atomicity

5. **UI**:
   - Form thêm/xóa line items (client-side dynamic)
   - Dropdown select Ingredient

**Key Points**:

- Receipt tạo và lock, không edit sau
- StockQuantity cập nhật trong cùng transaction
- Kiểm soát Supplier để trace nguồn gốc hàng

---

### **Flow 6: Đặt Bàn (Reservation)** (Ngày 3)

**Quy trình**: Khách chọn bàn + giờ → Kiểm tra conflict → Tạo booking → Xác nhận

**Code cần đọc**:

1. **Controllers**:
   - `Reservation/Index` - Staff CRUD reservation (list, create, confirm, cancel)
   - `Reservation/Book` - Customer online booking
   - `api/reservation/check-conflict` - kiểm tra trùng realtime

2. **Services**:
   - `ReservationService.CreateReservation()` - validate + tạo booking
   - `ReservationService.CheckTableConflict()` - check `TableID + ReservationTime` overlap
   - `ReservationService.ConfirmReservation()`, `.CancelReservation()`

3. **Validation Rule**:
   - Bàn không được đặt trùng giờ (same TableID + overlapping time window)
   - Bỏ qua booking đã `Cancelled`
   - Check bàn có trạng thái `Reserved` từ status

4. **Status Workflow**:

   ```
   Pending → Confirmed → (Served / Cancelled)

   ```

5. **UI**:
   - Staff: list view với filter date/table/status, modal CRUD
   - Customer: form chọn bàn + ngày/giờ + info, validate realtime

**Key Points**:

- Realtime conflict check giữa các booking chưa cancel
- Giữ audit: khi nào tạo, ai xác nhận/hủy
- Table status cập nhật khi reservation xác nhận

---

### **Flow 7: Chuyển/Gộp Bàn** (Ngày 3-4)

**Quy trình**: Chọn bàn nguồn/đích → Validate → Di chuyển orders → Đóng session cũ

**Code cần đọc**:

1. **API Endpoints**:
   - `POST /api/session/transfer` - chuyển hết order từ bàn này sang bàn khác
   - `POST /api/session/merge` - gộp 2 session

2. **Services**:
   - `SessionService.TransferTable()` - logic chuyển
   - `SessionService.MergeTable()` - logic gộp
   - Cả 2 dùng `IUnitOfWork.BeginTransaction()` để ensure atomicity

3. **Nghiệp Vụ Chi Tiết** (từ [transfermergetable.md](transfermergetable.md)):

   **Transfer**:
   - Nguồn phải `Occupied`, đích phải `Available`
   - Chuyển toàn bộ **chưa invoiced** orders sang session mới ở bàn đích
   - Đóng session nguồn, ghi `TableStatus = Available`
   - Đặt `TableStatus = Occupied` cho bàn đích

   **Merge**:
   - Nguồn phải `Occupied`, đích có thể `Available` hoặc `Occupied`
   - Nếu đích `Occupied`: gộp vào session đích, **giữ customer của đích**
   - Nếu đích `Available`: tạo session mới (customer từ nguồn), rồi gộp
   - Đóng session nguồn

   **Rule Chặn**:
   - Không cho `source == target`
   - Nếu session có order **đã invoiced** → chặn

4. **UI Integration**:
   - `Table/Index` - nút Transfer/Merge cho bàn Occupied
   - Modal chọn bàn đích theo rule
   - Update table status realtime sau thao tác

5. **Logging**:
   - Audit log chi tiết: thời điểm, source/target, số order, kết quả

**Key Points**:

- Transfer: bàn này → bàn kia (toàn bộ session di chuyển)
- Merge: kết hợp 2 session vào 1 (giữ lại customer nào?)
- Chỉ chuyển khi **chưa invoiced** orders
- Transaction để tránh data inconsistency

---

## SERVICES CHÍNH (22)

| Service                | Trách Nhiệm          | Methods Quan Trọng                                  |
| ---------------------- | -------------------- | --------------------------------------------------- |
| **AuthService**        | Xác thực, đăng nhập  | Login, Register, ValidateCredentials                |
| **SessionService**     | Quản lý phiên bàn    | OpenSession, EndSession, Transfer, Merge            |
| **OrderService**       | Xử lý order          | SubmitOrder, UpdateStatus, GetByStatus, CancelOrder |
| **KitchenService**     | Xử lý trạng thái món | GetPendingItems, UpdateOrderStatus                  |
| **PaymentService**     | Xử lý thanh toán     | ProcessPayment, HandleVNPayCallback                 |
| **VNPayService**       | Tích hợp VNPay       | CreatePaymentUrl, ValidateCallback                  |
| **BillService**        | Tính hóa đơn         | GetBill, CalculateTotal                             |
| **TableService**       | Quản lý bàn          | OpenTable, CloseTable, GetStatus                    |
| **CustomerService**    | Quản lý khách        | CreateCustomer, UpdateCustomer                      |
| **DashboardService**   | Thống kê KPI         | GetDailySummary, GetTopProducts, GetAlerts          |
| **ProcurementService** | Nhập hàng            | CreateReceipt, GetReceipts, GetSuppliers            |
| **ReservationService** | Đặt bàn              | CreateReservation, CheckConflict, Confirm           |
| **UserService**        | Quản lý user         | CRUD user, role                                     |
| **StaffService**       | Quản lý nhân viên    | CRUD staff, timekeeping                             |
| **CategoryService**    | Danh mục             | CRUD category                                       |
| **ProductService**     | Sản phẩm             | CRUD product, search                                |
| **ComboService**       | Combo sản phẩm       | CRUD combo                                          |
| **MenuService**        | Thực đơn             | GetMenu, GetMenuByCategory                          |
| **HrService**          | Nhân sự mở rộng      | Timekeeping, wage report                            |
| **InventoryService**   | Quản lý tồn kho      | UpdateStock, GetLowStockAlerts                      |
| **QrService**          | Tạo QR code          | GenerateQR cho table                                |
| **PasswordService**    | Mật khẩu             | Hash, Validate                                      |

---

## CROSS-CUTTING CONCERNS

### Authentication & Authorization

- **Cookie Authentication**: `CookieAuth` trong Program.cs
- **Roles**: `Admin`, `Staff`, `Kitchen`
- **Khu vực**:
  - Admin: User, Staff, Category, Product, Combo
  - Staff: Dashboard, Table, Menu, QR, Kitchen, Reservation
  - Kitchen: Kitchen, OrderStatus update
  - Anonymous: Customer Start, Menu view, Reservation/Book

### Antiforgery & Security

- Antiforgery token header: `RequestVerificationToken`
- Auto-validate cho controllers (trừ API anonymous)
- `request.Header` gồm token từ meta tag

### Realtime (SignalR `/orderHub`)

| Event                | Từ       | Đến         | Mục Đích                   |
| -------------------- | -------- | ----------- | -------------------------- |
| `OrderSubmitted`     | Customer | Kitchen     | Bếp nhận order mới         |
| `NewOrder`           | Backend  | Kitchen     | Danh sách order pending    |
| `OrderStatusUpdated` | Kitchen  | Tracking    | Cập nhật status            |
| `Callcheckout`       | Customer | Staff/Table | Khách yêu cầu thanh toán   |
| `CheckoutRequested`  | Customer | Table page  | Highlight bàn cần checkout |

### Error Handling & Logging

- `ILogger` trong services
- Validate DTOs trước xử lý
- Audit log cho thao tác quan trọng (transfer, merge, payment)

---

## FEATURES MỚI ĐANG PHÁT TRIỂN

### 1. Localization (Multi-language UI)

- **Referece**: [languageswitching.md](languageswitching.md)
- Toggle EN/VI trên header
- Resource files: `.resx` cho core pages
- Cookie persistence
- Backend: tiếng Anh, Frontend: tiếng Anh/Việt

### 2. Customer Cart UI Upgrade

- **Task 1** trong [upgradecustomerflow.md](upgradecustomerflow.md)
- Floating cart button → Drawer (desktop) / Bottom sheet (mobile)
- sessionStorage cart, không thay đổi API
- Badge hiển thị số lượng

### 3. Call Checkout Feature

- **Task 2** trong [upgradecustomerflow.md](upgradecustomerflow.md)
- `POST /api/order/call-checkout` endpoint mới
- `CheckoutRequestTracker` singleton service
- Table page highlight bàn cần checkout realtime
- Tracking page button disable sau call

---

## THỨ TỰ ĐỌC ĐỀ XUẤT

### Tuần 1 (4-5 ngày)

**Ngày 1: Foundation**

- [ ] [Program.cs](EMenu.Web/Program.cs) - DI, DbContext, Services setup
- [ ] Duyệt 24 Entities trong [EMenu.Domain/Entities/](EMenu.Domain/Entities/)
- [ ] Overview 22 Services trong [EMenu.Application/Services/](EMenu.Application/Services/)

**Ngày 2: Flow 1 + Flow 2**

- [ ] Flow QR: Controllers (Customer/Start, Menu, OrderPage/Tracking) + Services (CustomerService, SessionService, OrderService)
- [ ] Flow Table: Controllers (Table/Index) + Services (TableService, BillService) + JS (table.js, bill.js)

**Ngày 3: Flow 3 + Flow 4**

- [ ] Flow Kitchen: Controllers (Kitchen/Index) + Services (KitchenService) + Realtime SignalR
- [ ] Flow VNPay: Controllers (Payment) + Services (VNPayService, PaymentService) + [pvnpay.md](pvnpay.md)

**Ngày 4: Flow 5 + Flow 6**

- [ ] Flow Procurement: Controllers (Procurement) + Services (ProcurementService, InventoryService)
- [ ] Flow Reservation: Controllers (Reservation) + Services (ReservationService)

**Ngày 5: Flow 7 + Features**

- [ ] Flow Transfer/Merge: Controllers (Session API) + Services (SessionService) + [transfermergetable.md](transfermergetable.md)
- [ ] Features: Localization, Cart Upgrade, Call Checkout

---

## CÁCH đọc MỖI FLOW

Áp dụng cho Flow 1-7:

1. ✅ **Đọc tài liệu markdown** - hiểu quy trình nghiệp vụ
2. ✅ **Xác định Controllers** - entry points, API endpoints
3. ✅ **Trace Services** - logic xử lý, validation, database operations
4. ✅ **Hiểu Entities** - data models, relationships
5. ✅ **Xem Frontend** - views, JavaScript files, user interactions
6. ✅ **Realtime Events** - SignalR nếu có
7. ✅ **Chạy thử locally** - nếu có dev environment

---

## CÂU HỎI THƯỜNG GẶP KHI VẤN ĐÁP

### Architecture

- [ ] Tại sao dùng 5 projects riêng? Lợi ích của repository pattern + UoW?
- [ ] Dependency injection flow, làm sao resolve `IOrderRepository` trong service?
- [ ] Entity Framework Core migration strategy?

### Flows

- [ ] Flow QR từ scan QR đến tracking, mấy services liên quan? Realtime cách nào?
- [ ] Session được tạo ở đâu? Liên kết với Table + Customer thế nào?
- [ ] Kitchen status transition workflow, khi nào có thể cancel?
- [ ] VNPay callback hơi mới, có thể fail ở đâu? Idempotency đảm bảo thế nào?
- [ ] Transfer vs Merge khác nhau cơ bản?

### Security

- [ ] Authentication cookie flow?
- [ ] Authorization: role-based control ở đâu enforce?
- [ ] Antiforgery token, JavaScript gửi header thế nào?
- [ ] API endpoint nào nên anonymous, nên private?

### Realtime & Performance

- [ ] SignalR hub `/orderHub` có message queue không? Load test?
- [ ] Session/Order realtime update, client nào subscribe?
- [ ] Dashboard KPI refresh realtime hay periodic polling?

### Data Integrity

- [ ] Transaction xử lý thế nào khi payment thất bại halfway?
- [ ] Reservation conflict check - khoảng giờ tính thế nào?
- [ ] StockQuantity cập nhật atomicity khi import?

---

## GỘP THÔNG TIN KTRA

| Item               | Status | Notes                                              |
| ------------------ | ------ | -------------------------------------------------- |
| Program.cs DI      | ✓      | Tất cả services registered                         |
| Entity models      | ✓      | 24 entities, relationships defined                 |
| Repository pattern | ✓      | Interfaces in Abstractions, impl in Infrastructure |
| Authorization      | ⚠️     | Role-based, cần verify anonymous endpoints         |
| VNPay              | ⚠️     | Plain-text secrets cần Secret Manager              |
| Realtime           | ✓      | SignalR hub `/orderHub`                            |
| Transaction        | ✓      | UnitOfWork dùng cho payment, transfer, merge       |
| Localization       | ✓      |                                                    |

---

## Tài Liệu Tham Khảo

- [systemdescription.md](systemdescription.md) - Tổng quan hệ thống
- [languageswitching.md](languageswitching.md) - Localization feature
- [upgradecustomerflow.md](upgradecustomerflow.md) - Cart + Checkout features
- [pvnpay.md](pvnpay.md) - VNPay debug checklist
- [newdb.md](newdb.md) - Database schema
- [transfermergetable.md](transfermergetable.md) - Transfer/Merge flow

---

**Good luck with your project presentation!**
