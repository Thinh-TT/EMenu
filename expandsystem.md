# Expand System Roadmap - EMenu

## 1. Muc tieu hoan thien san pham

- Hoan thien he thong EMenu tu "quan ly order co ban" thanh "he thong van hanh nha hang day du".
- Tich hop cac module mo rong da them entity:
- HR: `Timekeeping`, `Wage`.
- Inventory: `Ingredient`, `IngredientProduct`.
- Supplier & Import: `Supplier`, `Receipt`, `ReceiptIngredient`.
- Reservation: `Reservation`.
- Dam bao 3 tieu chi:
- Dung nghiep vu.
- De van hanh (UI ro rang, role phan quyen dung).
- San sang scale (code theo layer hien tai, it pha vo kien truc cu).

## 2. Huong di tong the

## Backend

- Giữ pattern hien co: `Controller -> Service -> Repository -> EF Core`.
- Moi module moi can co day du:
- Repository interfaces trong `EMenu.Application.Abstractions`.
- Repository implementations trong `EMenu.Infrastructure`.
- Service trong `EMenu.Application`.
- API/MVC controllers trong `EMenu.Web`.
- Validation nghiep vu dat o Service, khong day ve Controller.
- Tat ca use case ghi du lieu nhieu bang: dung `UnitOfWork + Transaction`.

## Frontend

- Uu tien xay man hinh van hanh don gian, de dung:
- Dashboard module moi (canh bao ton kho, lich lam, dat ban).
- CRUD pages + workflow pages.
- API calls qua fetch + antiforgery token nhu hien tai.
- Role-based UI theo `Admin`, `Staff`, `Kitchen`.

## Muc tieu release

- P0: Module mo rong chay duoc end-to-end.
- P1: Bao cao + dashboard tong hop.
- P2: Tinh chinh UX, optimize hieu nang, hardening security.

## 3. Chia nho task hop ly (theo Epic)

## Epic A - HR (Timekeeping, Wage)

### A1. Backend foundation

- [x] Tao interfaces:
- `ITimekeepingRepository`, `IWageRepository`.
- [x] Implement repositories EF (CRUD + query theo staff/ngay/thang).
- [x] Tao `HrService` (hoac `TimekeepingService` + `WageService`).
- [x] Nghiep vu:
- Check-in/Check-out theo ngay.
- Khong cho check-out truoc check-in.
- Moi staff chi co 1 wage profile.

### A2. API/MVC

- [x] `HrController` (Admin/Staff):
- Check-in, check-out, xem cham cong.
- [x] `WageController` (Admin):
- Tao/cap nhat muc luong.
- [x] Them audit log thong tin user thao tac.

### A3. Frontend

- [x] View cham cong theo ngay/thang.
- [x] View quan ly wage profile.
- [x] Quick action check-in/out cho staff.

### A4. Bao cao

- [x] Tong gio lam theo staff/thang.
- [x] Uoc tinh luong: `BaseSalary + HourlyRate * Hours`.

## Epic B - Inventory (Ingredient, IngredientProduct)

### B1. Backend foundation

- [x] Tao interfaces:
- `IIngredientRepository`, `IIngredientProductRepository`.
- [x] Implement query:
- Ton kho hien tai.
- Nguyen lieu theo mon.
- Mon su dung nguyen lieu nao.
- [x] `InventoryService`:
- CRUD ingredient.
- Gan/dieu chinh dinh muc nguyen lieu cho mon.

### B2. Nghiep vu ton kho

- [x] Ham tinh ton kho sau khi nhap hang.
- [x] Ham canh bao ton kho thap (`StockQuantity <= MinStock`).
- [x] (P1) Tru ton kho theo order da served.

### B3. Frontend

- [x] Man hinh danh sach ingredient + canh bao.
- [x] Man hinh mapping ingredient-product.
- [x] Filter/sort theo muc ton.

## Epic C - Supplier & Import (Supplier, Receipt, ReceiptIngredient)

### C1. Backend foundation

- [ ] Tao interfaces:
- `ISupplierRepository`, `IReceiptRepository`.
- [ ] `ProcurementService`:
- Tao phieu nhap.
- Them chi tiet nguyen lieu.
- Cap nhat ton kho sau khi nhap (transaction).

### C2. Nghiep vu phieu nhap

- [ ] Validate supplier/staff ton tai.
- [ ] Validate quantity/price > 0.
- [ ] Tinh tong gia tri phieu nhap.

### C3. Frontend

- [ ] CRUD supplier.
- [ ] Tao phieu nhap + chi tiet dong.
- [ ] Trang lich su nhap hang + loc theo ngay/supplier.

## Epic D - Reservation

### D1. Backend foundation

- [ ] Tao `IReservationRepository`.
- [ ] `ReservationService`:
- Tao dat ban.
- Xac nhan/huy dat ban.
- Check trung lich theo ban/thoi diem.

### D2. API/MVC

- [ ] `ReservationController`:
- Admin/Staff: quan ly dat ban.
- (Tuy chon) Customer tao dat ban online.

### D3. Frontend

- [ ] Calendar/list view dat ban.
- [ ] Form dat ban: customer, table, time, guests.
- [ ] Trang thai: Pending/Confirmed/Cancelled.

## Epic E - Dashboard mo rong

### E1. KPI moi

- [ ] Ton kho thap.
- [ ] Gia tri nhap hang theo ngay/thang.
- [ ] So ban dat truoc trong ngay.
- [ ] Tong gio lam nhan vien hom nay.

### E2. UI/UX

- [ ] The KPI + chart xu huong.
- [ ] Widget canh bao (low stock, reservation clash).

## Epic F - Security, Quality, Ops

### F1. Security

- [ ] Re-check authorize cho tat ca endpoint moi.
- [ ] Chuyen secrets khoi `appsettings.json` sang User Secrets/ENV.
- [ ] Validate input & anti-overposting cho form moi.

### F2. Testing

- [ ] Unit tests cho services moi.
- [ ] Integration tests cho workflow:
- Nhap hang -> tang ton kho.
- Dat ban -> conflict check.
- Check-in/out -> tinh gio.

### F3. Data migration & seed

- [ ] Tao migration cho schema mo rong.
- [ ] Seed data toi thieu cho module moi (ingredient/supplier/wage mau).
- [ ] Script rollback/chuyen doi du lieu neu can.

## 4. Thu tu trien khai de xong nhanh

## Phase 1 (P0 - bat buoc)

- [x] Epic B (Inventory foundation).
- [ ] Epic C (Import + cap nhat ton kho).
- [ ] Epic D (Reservation co ban).
- [x] Epic A (Timekeeping co ban).

## Phase 2 (P1 - nang cao)

- [ ] Dashboard mo rong (Epic E).
- [ ] Bao cao wage/tong hop inventory.
- [ ] UI polish cho module moi.

## Phase 3 (P2 - production hardening)

- [ ] Security hardening + test coverage + logging.
- [ ] Performance tuning query.
- [ ] Tieu chuan hoa API response va error code.

## 5. Definition of Done (DoD) cho moi module

- [ ] Co migration va apply DB thanh cong.
- [ ] Co repository + service + controller + view/API.
- [ ] Co validation nghiep vu chinh.
- [ ] Co phan quyen dung role.
- [ ] Co it nhat 1-2 test workflow chinh.
- [ ] Co log thao tac quan trong.
- [ ] Co tai lieu cap nhat trong `systemdescription.md`.

## 6. Backlog task mau (de vao board Jira/Trello)

- [x] BE-HR-01: Tao repository va service Timekeeping.
- [x] BE-HR-02: API check-in/check-out + validation.
- [x] FE-HR-01: View cham cong theo staff.
- [x] BE-INV-01: CRUD Ingredient + canh bao ton thap.
- [x] BE-INV-02: Mapping IngredientProduct.
- [x] FE-INV-01: Man hinh ton kho.
- [ ] BE-PRC-01: Tao Receipt + ReceiptIngredient (transaction).
- [ ] FE-PRC-01: Form nhap hang.
- [ ] BE-RSV-01: ReservationService + conflict check.
- [ ] FE-RSV-01: Man hinh reservation list/calendar.
- [ ] BE-DB-01: Dashboard metrics cho module moi.
- [ ] SEC-01: Chuyen secrets ra env.
- [ ] QA-01: Integration tests cho 3 workflow critical.

## 7. Ket qua ky vong sau khi hoan tat

- Co he thong EMenu full-flow:
- Dat ban -> vao ban -> goi mon -> bep -> thanh toan.
- Quan ly duoc dau vao van hanh:
- Nhan su (cham cong/luong co ban), nguyen lieu, nha cung cap, nhap hang.
- Quan tri de ra quyet dinh:
- Dashboard tong hop order/doanh thu/ton kho/dat ban.
