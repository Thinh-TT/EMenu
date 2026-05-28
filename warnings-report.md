# EMenu — Cảnh Báo & Kế Hoạch Xử Lý

> Sinh ngày: 2026-05-15
> Build: `dotnet build EMenuSolution.slnx`
> Kết quả gần nhất: **85 Warnings — 0 Errors**

---

## 1. Tổng Quan Cảnh Báo

| # | Mã | Số lượng | Khu vực | Mức độ |
|---|-----|----------|---------|--------|
| A | CS8618 | ~70 | Entity + DTO — non-nullable property chưa khởi tạo | Thấp–Trung bình |
| B | CS8601/CS8603 | ~11 | Services — possible null reference | Trung bình |
| C | CS8981 | ~4 | Migrations — tên class viết thường | Thấp |
| D | — | 6 | Tài liệu `systemdescription.md` §8 — vấn đề kiến trúc/bảo mật | Cao |

---

## 2. Chi Tiết & Cách Xử Lý

### A. CS8618 — Non-nullable property must contain a non-null value (~70)

**Mô tả:** Nullable reference types được bật. Các property kiểu `string`, `ICollection<T>`, entity reference không được gán giá trị mặc định.

**File tiêu biểu:**

| File | Property | Cách sửa |
|------|----------|-----------|
| `EMenu.Domain/Entities/RestaurantTable.cs` | `OrderSessions`, `Reservations` | `= new List<T>()` |
| `EMenu.Domain/Entities/Receipt.cs` | `Supplier`, `Staff`, `ReceiptIngredients` | `= string.Empty` / `= new List<T>()` |
| `EMenu.Domain/Entities/Reservation.cs` | `Customer`, `RestaurantTable` | `= null!` (set by EF) |
| `EMenu.Domain/Entities/ReceiptIngredient.cs` | `Receipt`, `Ingredient` | `= null!` |
| `EMenu.Domain/Entities/User.cs` | `UserName`, `Password`, `UserRoles`, `Staff` | `= string.Empty` / `= new List<T>()` |
| `EMenu.Domain/Entities/Shift.cs` | `ShiftLogs` | `= new List<T>()` |
| `EMenu.Domain/Entities/ShiftLog.cs` | `Staff`, `Shift` | `= null!` |
| `EMenu.Domain/Entities/Supplier.cs` | `Name`, `Receipts` | `= string.Empty` / `= new List<T>()` |
| `EMenu.Domain/Entities/Staff.cs` | `StaffName`, `Phone`, `Email`, `User`, `ShiftLogs`, `Orders`, `Timekeepings`, `Wage`, `Receipts` | `= string.Empty` / `= new List<T>()` |
| `EMenu.Domain/Entities/Timekeeping.cs` | `Staff` | `= null!` |
| `EMenu.Application/DTOs/BillDto.cs` | `TableName`, `Items` | `= string.Empty` / `= new()` |
| `EMenu.Application/DTOs/BillItemDto.cs` | `ProductName` | `= string.Empty` |

**Nguyên tắc chung:**
- **Collection navigation** → `= new List<T>()`
- **Scalar string** → `= string.Empty`
- **Entity reference (FK) — được EF Core set** → `= null!` (null-forgiving operator)
- **DTO** → `= string.Empty` hoặc `= new()`

---

### B. CS8601/CS8603 — Possible null reference (~11)

**Mô tả:** Service method gọi `FirstOrDefault()` trả về null nhưng return type không nullable.

| File | Line | Warning | Cách sửa |
|------|------|---------|-----------|
| `Services/CategoryService.cs` | 27 | CS8603 Possible null return | Đổi return type thành `Category?` hoặc throw nếu null là lỗi |
| `Services/CustomerService.cs` | 38 | CS8603 Possible null return | Như trên |
| `Services/ProductService.cs` | 30 | CS8603 Possible null return | Như trên |
| `Services/SessionService.cs` | 37, 42 | CS8603 Possible null return | Như trên |
| `Services/UserService.cs` | 33 | CS8603 Possible null return | Như trên |
| `Services/StaffService.cs` | 37 | CS8603 Possible null return | Như trên |
| `Services/TableService.cs` | 27 | CS8603 Possible null return | Như trên |
| `Services/BillService.cs` | 59, 88 | CS8601 Possible null assignment | Thêm null check hoặc `?? throw new InvalidOperationException(...)` |
| `Services/OrderService.cs` | 214 | CS8601 Possible null assignment | Như trên |

---

### C. CS8981 — Migration class tên viết thường (~4)

| File | Tên hiện tại | Đổi thành |
|------|-------------|-----------|
| `Migrations/20260307052239_create-entity.cs` | `createentity` | `CreateEntity` |
| `Migrations/20260307052239_create-entity.Designer.cs` | `createentity` | `CreateEntity` |
| `Migrations/20260407013308_expandbusiness.cs` | `expandbusiness` | `ExpandBusiness` |
| `Migrations/20260407013308_expandbusiness.Designer.cs` | `expandbusiness` | `ExpandBusiness` |

> Lưu ý: Đổi tên class xong cần cập nhật file snapshot migration.

---

### D. Vấn đề từ `systemdescription.md` §8

| # | Vấn đề | Mức độ | Cách xử lý |
|---|--------|--------|-----------|
| D1 | `AuthController` truy cập `AppDbContext` trực tiếp | Cao | Inject `IAuthService`, chuyển logic login/register vào service |
| D2 | `POST /api/order/submit` & `GET /api/order/status` để `AllowAnonymous` | Cao | Thêm policy xác thực session token; không để mở hoàn toàn trên production |
| D3 | `PaymentController.VNPayReturn` chưa verify hash / chưa đồng bộ DB | Cao | Gọi `VNPayService.ValidateCallback()` trước xử lý; đảm bảo transaction Invoice + Payment + Order |
| D4 | Thiếu View `PaymentSuccess` / `PaymentFail` | Trung bình | Tạo 2 Razor View trong `Views/Payment/` |
| D5 | `table.js` hard-code `customerId=1` | Trung bình | Tạo walk-in customer hoặc UI chọn customer trước khi mở bàn |
| D6 | Secrets plain text trong `appsettings.json` | Cao | `dotnet user-secrets` cho dev; ENV var / Key Vault cho production |

---

## 3. Kế Hoạch Thực Thi

### Giai đoạn 1 — An toàn trước (P0)

| Bước | Hạng mục | Dự kiến |
|------|----------|---------|
| 1.1 | D6: Chuyển secrets ra khỏi `appsettings.json` | 30 phút |
| 1.2 | D3: Verify hash trong VNPayReturn callback | 1 giờ |
| 1.3 | D1: AuthController refactor qua AuthService | 1 giờ |

### Giai đoạn 2 — Ổn định code (P1)

| Bước | Hạng mục | Dự kiến |
|------|----------|---------|
| 2.1 | A: Sửa toàn bộ CS8618 (~70 warnings) | 2 giờ |
| 2.2 | B: Sửa toàn bộ CS8601/CS8603 (~11 warnings) | 1 giờ |
| 2.3 | C: Đổi tên migration class sang PascalCase | 30 phút |
| 2.4 | D5: Bỏ hard-code customerId trong table.js | 30 phút |

### Giai đoạn 3 — Hoàn thiện (P2)

| Bước | Hạng mục | Dự kiến |
|------|----------|---------|
| 3.1 | D2: Review policy AllowAnonymous cho API order | 1 giờ |
| 3.2 | D4: Tạo View PaymentSuccess / PaymentFail | 30 phút |
| 3.3 | Build lại, xác nhận 0 Warning | 10 phút |

---

## 4. Nhật Ký Xử Lý

> Mỗi phiên làm việc: ghi ngày, người thực hiện, bước đã làm, warnings còn lại, ghi chú.

### [2026-05-15] — Khởi tạo báo cáo

- **Người thực hiện:** 
- **Bước đã làm:** Quét toàn bộ warnings từ build + tài liệu root. Tạo file báo cáo này.
- **Warnings còn lại:** 85
- **Ghi chú:**

---

### [YYYY-MM-DD] — Phiên ...

- **Người thực hiện:** 
- **Bước đã làm:** 
- **Warnings còn lại:** 
- **Ghi chú:**

---

### [YYYY-MM-DD] — Phiên ...

- **Người thực hiện:** 
- **Bước đã làm:** 
- **Warnings còn lại:** 
- **Ghi chú:**

---

### [YYYY-MM-DD] — Phiên ...

- **Người thực hiện:** 
- **Bước đã làm:** 
- **Warnings còn lại:** 
- **Ghi chú:**

---

### [YYYY-MM-DD] — Phiên cuối — Hoàn tất

- **Người thực hiện:** 
- **Warnings còn lại:** 0
- **Ghi chú:**
