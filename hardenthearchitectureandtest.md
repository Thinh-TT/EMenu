Tách Application khỏi Infrastructure qua interface.
Thêm transaction cho checkout/payment.
Viết test: unit (service) + integration (API chính).
Kết quả: code dễ mở rộng, ít regression khi thêm tính năng.

Stack 1: Tách abstraction cho nghiệp vụ core
x

Mục tiêu

Application bắt đầu tách khỏi Infrastructure
xác định rõ interface nào cần có trước
Việc cần làm

Tạo thư mục interface/abstraction trong EMenu.Application
ví dụ: Abstractions/Repositories, Abstractions/Persistence
Tạo interface cho các nhóm nghiệp vụ core
ISessionRepository
IOrderRepository
IPaymentRepository
IBillRepository
IProductRepository
ITableRepository
Nếu cần, tạo interface transaction chung
IUnitOfWork
hoặc IAppDbContext nếu bạn muốn tách ở mức DbContext abstraction
Xác định contract tối thiểu cho từng interface
chỉ đưa method thật sự dùng
tránh tạo interface quá to ngay từ đầu

Stack 2: Refactor Session/Order/Bill sang interface
x

Mục tiêu

kéo 3 service nghiệp vụ gần nhau nhất sang abstraction trước
Việc cần làm

Refactor SessionService
bỏ phụ thuộc trực tiếp AppDbContext
dùng ISessionRepository, ITableRepository, IOrderRepository
Refactor OrderService
dùng IOrderRepository, ISessionRepository, IProductRepository
Refactor BillService
dùng IBillRepository hoặc IOrderRepository + query methods phù hợp
Cài implementation repository ở Infrastructure
Đăng ký DI trong Program.cs

Stack 3: Refactor Payment/Kitchen/User/Staff sang interface
x

Mục tiêu

hoàn tất phần lớn service layer của Application
Việc cần làm

Refactor PaymentService
tách truy cập Order, Invoice, Payment, Session, Table
Refactor KitchenService
tách logic lấy pending item và update status qua repository
Refactor UserService, StaffService
dùng abstraction cho user/role/staff
Nếu thấy hợp lý, tách thêm query service riêng cho dashboard/menu

Stack 4: Gỡ hẳn reference Application -> Infrastructure
x

Mục tiêu

hoàn tất bước “cứng hóa kiến trúc”
Việc cần làm

Kiểm tra toàn bộ EMenu.Application
tìm using EMenu.Infrastructure...
tìm chỗ dùng AppDbContext, EF entities/query trực tiếp
Gỡ project reference khỏi EMenu.Application.csproj
Đảm bảo Web tham chiếu Infrastructure để DI implementation hoạt động
Build lại toàn solution

Stack 5: Chuẩn hóa transaction và command flow
x

Mục tiêu

gom transaction về cách làm nhất quán, tránh save rải rác
Việc cần làm

Chốt cách quản lý transaction
IUnitOfWork
hoặc transaction wrapper ở command service
Rà các use case nhiều bước
submit order
cash checkout
VNPay callback
start session
end session
Giảm SaveChanges rải rác trong cùng một luồng
Tách read/write rõ hơn nếu được
query không transaction
command mới commit

Stack 6: Unit test cho service core
x

Mục tiêu

khóa logic nghiệp vụ bằng test
Việc cần làm

Tạo project test
ví dụ: EMenu.Tests
Tạo test cho SessionService
mở bàn thành công
chặn mở bàn bận
chặn đóng session chưa thanh toán
đóng session hợp lệ
Tạo test cho OrderService
thêm món thành công
chặn session đóng
chặn quantity sai
chặn món unavailable
submit rollback nếu có item lỗi
Tạo test cho PaymentService
cash checkout thành công
chặn checkout lặp
chặn order rỗng
Tạo test cho KitchenService
transition hợp lệ
transition sai
Tạo test cho PasswordService
hash/verify
detect legacy
validate policy

Stack 7: Integration test cho API chính
x

Mục tiêu

test thực tế route + auth + middleware + DB flow
Việc cần làm

Dùng WebApplicationFactory
Chuẩn bị test DB riêng
ưu tiên SQLite in-memory hơn EF InMemory nếu cần gần thực tế hơn
Viết integration test cho:
POST /api/session/start
POST /api/order/submit
PUT /api/kitchen/update-status
POST /Payment/Cash
auth/authorize theo role
Seed data test tối thiểu
role
user/staff
table
session/order mẫu
Thêm lệnh chạy test rõ ràng cho team



