# System Description - EMenu

## 1. Tong quan he thong
- `EMenu` la he thong quan ly van hanh nha hang theo mo hinh web MVC, gom:
- `Frontend`: ASP.NET Core MVC + Razor Views + JavaScript (fetch API, SignalR, Chart.js).
- `Backend`: ASP.NET Core controllers + Application Services + Repository/UoW.
- `Database`: SQL Server, truy cap qua Entity Framework Core.
- Muc tieu nghiep vu chinh:
- Quan ly ban, phien order (session), menu, don mon, bep, hoa don, thanh toan.
- Quan ly nhan su co ban (user, role, staff), danh muc san pham va combo.

## 2. Cau truc giai phap
- `EMenu.Web`: presentation layer (controllers, views, static assets, auth cookie, SignalR hub).
- `EMenu.Application`: business layer (cac service xu ly nghiep vu).
- `EMenu.Application.Abstractions`: hop dong (repository interfaces, DTOs, UoW/transaction abstractions).
- `EMenu.Domain`: entities, enum, constants (nguon model nghiep vu).
- `EMenu.Infrastructure`: EF Core `AppDbContext`, repository implementations, migrations, seeding.

## 3. Backend architecture
### 3.1 Startup va DI
- `Program.cs` dang ky:
- `AppDbContext` voi SQL Server.
- Repository implementations qua `AddInfrastructureRepositories()`.
- Application services: `AuthService`, `OrderService`, `SessionService`, `PaymentService`, `KitchenService`, `DashboardService`, ...
- Application services: `AuthService`, `OrderService`, `SessionService`, `PaymentService`, `KitchenService`, `DashboardService`, `ProcurementService`, ...
- Cookie authentication (`CookieAuth`) va phan quyen theo role.
- Swagger (development), SignalR (`/orderHub`), antiforgery token header `RequestVerificationToken`.
- Auto validate antiforgery cho controller/view requests.

### 3.2 Mo hinh xu ly nghiep vu
- Service layer la trung tam xu ly:
- `SessionService`: mo/dong phien tai ban.
- `OrderService`: tao don, them mon, submit gio hang, tinh bill theo session.
- `KitchenService`: cap nhat trang thai mon theo workflow.
- `PaymentService`: thanh toan tien mat/VNPay success flow, tao invoice + payment, dong session.
- `BillService`: tong hop bill tu order items.
- `DashboardService`: doanh thu ngay, top mon, thong ke ban.
- `UserService`, `StaffService`, `CategoryService`, `ProductService`, `ComboService`: CRUD va validation lien quan.
- `UserService`, `StaffService`, `CategoryService`, `ProductService`, `ComboService`: CRUD va validation lien quan.
- `ProcurementService`: quan ly nha cung cap, tao phieu nhap nguyen lieu, cap nhat ton kho theo transaction.

### 3.3 Data access pattern
- Application layer chi lam viec voi interfaces:
- `IOrderRepository`, `ISessionRepository`, `IPaymentRepository`, `IUnitOfWork`, ...
- Infrastructure trien khai EF Core repositories.
- Cac use case quan trong su dung transaction qua `IUnitOfWork.BeginTransaction()`.

### 3.4 API va controller style
- He thong dung ket hop:
- MVC controllers tra ve views (man hinh quan tri/van hanh).
- API-style endpoints cho JS frontend (`/api/order`, `/api/session`, `/api/kitchen`, `/api/dashboard`, `/api/bill`).
- Cac endpoint quan trong:
- `POST /api/order/submit` (khach hang gui gio hang).
- `GET /api/order/status` (tracking trang thai mon).
- `PUT /api/kitchen/update-status` (bep cap nhat mon).
- `POST /api/session/start|end` (mo/dong phien).
- `POST /Payment/Cash`, `POST /Payment/VNPay`.
- `GET /Procurement/Index`, `GET|POST /Procurement/CreateReceipt`, `GET /Procurement/ReceiptHistory`.
- `GET /api/procurement/suppliers`, `GET /api/procurement/receipts`.

## 4. Frontend architecture
### 4.1 Presentation stack
- Razor Views theo module: `Auth`, `Table`, `Menu`, `Kitchen`, `Dashboard`, `BillPage`, `Checkout`, `Qr`, ...
- Razor Views theo module: `Auth`, `Table`, `Menu`, `Kitchen`, `Dashboard`, `BillPage`, `Checkout`, `Qr`, `Procurement`, ...
- Layout chung `_Layout.cshtml`:
- Navbar dong theo role dang dang nhap.
- Tich hop antiforgery token vao meta.
- Load bootstrap, site CSS, JS global.

### 4.2 JavaScript modules
- `menu.js`: quan ly session id theo URL, them mon nhanh.
- `cart.js`: gio hang tren `sessionStorage`, submit don qua API.
- `table.js`: mo/dong ban tu giao dien staff.
- `kitchen.js`: hien thi mon cho bep, cap nhat status, nhan event realtime.
- `bill.js`: tai bill va checkout API.
- `dashboard.js`: goi API thong ke va ve chart.
- `antiforgery.js`: dong bo token vao request headers.
- Man hinh `Procurement/CreateReceipt` su dung form dong (line items) de them nhieu nguyen lieu trong 1 phieu nhap.

### 4.3 Realtime
- SignalR hub `OrderHub` tai endpoint `/orderHub`.
- Event dang dung:
- `OrderSubmitted`: thong bao don moi.
- `NewOrder`: cap nhat danh sach don cho bep.
- `OrderStatusUpdated`: dong bo trang thai mon realtime.

## 5. Luong nghiep vu chinh
### 5.1 Luong khach hang qua QR
- Khach scan QR: `/Customer/Start?tableId=...`.
- Nhap thong tin khach -> tao customer -> tao session gan voi table.
- Chuyen sang `/Menu?tableId=...&sessionId=...`.
- Khach chon mon, submit gio hang (`/api/order/submit`).
- Bep nhan don va xu ly trang thai.
- Khach theo doi trang thai tai `OrderPage/Tracking`.

### 5.2 Luong staff/admin tai ban
- Staff/Admin vao man hinh `Table`.
- Mo session, xem bill, checkout.
- Thanh toan cash tao `Invoice` + `Payment`, dong session va tra ban ve available.

### 5.3 Luong kitchen
- Kitchen vao `Kitchen/Index`.
- Lay pending items qua API.
- Chuyen trang thai mon theo thu tu:
- Pending -> Preparing -> Ready -> Served.
- Co the cancel neu chua served.
- Trang thai phat realtime cho cac man hinh dang theo doi.

### 5.4 Luong nhap hang (supplier & import)
- Staff/Admin vao `Procurement/Index` de CRUD nha cung cap.
- Tao phieu nhap tai `Procurement/CreateReceipt` voi nhieu dong nguyen lieu.
- He thong validate supplier/staff/nguyen lieu va quantity/price > 0.
- Khi luu phieu nhap: tao `Receipt` + `ReceiptIngredients`, dong thoi tang `Ingredient.StockQuantity` trong cung transaction.
- Theo doi lich su phieu nhap theo ngay/nha cung cap tai `Procurement/ReceiptHistory`.

## 6. Security va phan quyen
- Xac thuc bang cookie auth.
- Role constants:
- `Admin`, `Staff`, `Kitchen`.
- Khu vuc admin: user, staff, category, product, combo.
- Khu vuc van hanh: dashboard/table/menu/kitchen/qr theo role duoc cap.
- Co antiforgery token cho form va fetch API.

## 7. Tich hop ngoai
- VNPay:
- Tao payment URL tu cau hinh (`TmnCode`, `HashSecret`, `ReturnUrl`).
- Co action `VNPayReturn` de nhan ket qua redirect.
- Chart.js:
- Hien thi doanh thu va top product tren dashboard.

## 8. Nhan xet ky thuat hien tai
- Uu diem:
- Tach layer ro rang (Web/Application/Infrastructure/Domain).
- Service layer bao phu nghiep vu cot loi.
- Co transaction cho cac use case quan trong.
- Co realtime SignalR cho kitchen va tracking.
- Diem can luu y:
- `AuthController` dang truy cap `AppDbContext` truc tiep, chua dung `AuthService`.
- `POST /api/order/submit` va `GET /api/order/status` de `AllowAnonymous`, can xem lai policy bao mat neu dua len production.
- `PaymentController.VNPayReturn` chua thay verify secure hash va chua thay dong bo nghiep vu thanh toan vao DB tu callback.
- Trong code co tham chieu view `PaymentSuccess`/`PaymentFail` nhung chua thay file view tuong ung.
- `table.js` dang hard-code `customerId=1` khi mo ban tu giao dien table.
- `appsettings.json` dang co connection string va VNPay secrets dang plain text (nen dua qua secret manager/ENV).

## 9. San sang mo rong
- Nen tang hien tai phu hop de mo rong them cac module:
- HR (timekeeping/wage), inventory, supplier/receipt, reservation.
- Huong mo rong it pha vo:
- Them entity + configuration + repository + service theo pattern san co.
- Uu tien giu nguyen contracts cu, bo sung contracts moi theo feature slice.
- Neu can public API cho module moi, tiep tuc theo convention `/api/{module}` + antiforgery + role policy ro rang.
