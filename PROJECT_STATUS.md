# PROJECT STATUS

> Cập nhật lần cuối: Ngày 12 (173 test xanh)

## 1. Mục tiêu và phạm vi MVP
Business Management API (ASP.NET Core): Auth, Products, Customers, Orders, Payments, RabbitMQ worker.

| Bắt buộc | Làm sau | Chưa cần |
|---|---|---|
| Auth (JWT, role), CRUD Products/Customers, Orders + trừ kho trong transaction, Payments (bản ghi nghiệp vụ), validation, logging, unit + integration test, Docker Compose, RabbitMQ (retry, DLQ, idempotent consumer) | Permission chi tiết, Transactional Outbox, Idempotency-Key, Kafka demo | Microservices, CQRS/MediatR, thanh toán thật, frontend |

Tiến độ MVP:
- [x] Auth: đăng ký, đăng nhập, JWT, phân quyền theo vai trò
- [x] CRUD Products, Customers (phân trang, lọc, sắp xếp)
- [x] Orders: tạo đơn giữ hàng trong transaction, chống bán vượt tồn, huỷ đơn hoàn kho
- [x] Báo cáo top khách (2 truy vấn, đã đo bằng EXPLAIN)
- [ ] Voucher/giảm giá lưu trong đơn, danh sách đơn, logging có ngữ cảnh (Ngày 13)
- [ ] Payments và Confirm đơn
- [ ] RabbitMQ + Worker: retry, DLQ, idempotent consumer (Ngày 14–17)
- [ ] Docker Compose đầy đủ, README, CI (Ngày 18–19)

## 2. Công nghệ và phiên bản
- .NET SDK 10.0.300 / runtime 10.0.14 (LTS, hỗ trợ đến 11/2028)
- ASP.NET Core Web API (controllers); `Microsoft.OpenApi` ghim **2.7.5**
- EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3
- PostgreSQL 18 (Docker Compose, cổng 5434)
- Xác thực: `JwtBearer` + `Microsoft.IdentityModel.JsonWebTokens`; băm mật khẩu bằng `PasswordHasher` của ASP.NET Core Identity (PBKDF2)
- xUnit 2.9.3, Testcontainers (PostgreSql)
- RabbitMQ.Client 7.x, Worker

## 3. Cấu trúc thư mục (đối chiếu với thực tế)
```
src/
  BusinessManagement.Domain/          Common, Entities (Product, Customer, Order, OrderItem, OrderStatus, User)
  BusinessManagement.Application/     Abstractions (IAppDbContext), Auth, Common, Customers, Products, Orders, Reports,
                                      Checkout/{Discount,Events,Payments,Ports,Pricing}, DependencyInjection.cs
  BusinessManagement.Infrastructure/  Persistence/{Configurations,Migrations}, Auth (hasher, JWT), cài đặt thanh toán/thông báo/log đơn,
                                      DependencyInjection.cs
  BusinessManagement.Api/             Controllers, Contracts/{Requests,Responses}, ExceptionHandling, Program.cs
tests/
  BusinessManagement.UnitTests/        Domain + Application (logic thuần)
  BusinessManagement.IntegrationTests/ CompositionRoot, Api (WebApplicationFactory), Services và Persistence (Testcontainers)
learning/                              Code học (Day01, Day04, Learning.Tests), ngoài solution sản phẩm
scripts/                               seed-dev.sql (dữ liệu thử cho DB dev, không dùng cho test)
```

## 4. Tiến độ
| Ngày | Nội dung | Trạng thái |
|---|---|---|
| 1 | Skeleton, LINQ/async (1512 ms tuần tự so với 515 ms WhenAll) | ✅ |
| 1 | Nộp hồ sơ, gửi CV | [ ] chưa xong |
| 2 | Domain entities, unit test, thử nghiệm đột biến | ✅ |
| 3 | SOLID + DI, lifetimes, captive dependency | ✅ |
| 4 | DSA và Big-O | ✅ |
| 5 | Strategy, Factory, Adapter | ✅ |
| 6 | Decorator, Observer, quyết định kiến trúc | ✅ |
| 7 | Pricing API, exception handler, smoke test | ✅ |
| 8 | EF Core, PostgreSQL, migrations, test persistence | ✅ |
| 9 | CRUD Products và Customers | ✅ |
| 10 | SQL JOIN/GROUP BY, index, EXPLAIN, N+1 | ✅ |
| 11 | Đăng ký, đăng nhập, JWT, phân quyền | ✅ |
| 12 | Tạo đơn, transaction, chống bán vượt tồn | ✅ |

API hiện có:

| Nhóm | Endpoint | Quyền |
|---|---|---|
| Hệ thống | `GET /health`, `/openapi` (Development) | Công khai |
| Auth | `POST /api/auth/register`, `POST /api/auth/login` | Công khai |
| | `GET /api/auth/me` | Đăng nhập |
| Pricing | `POST /api/pricing/quote` | Đăng nhập |
| Products | `GET`, `POST`, `PUT /api/products`, `GET /api/products/{id}` | Đăng nhập |
| | `DELETE /api/products/{id}` | Admin |
| Customers | `GET`, `POST`, `PUT /api/customers`, `GET /api/customers/{id}`, `POST .../deactivate`, `POST .../activate` | Đăng nhập |
| Orders | `POST /api/orders`, `GET /api/orders/{id}`, `POST /api/orders/{id}/cancel` | Đăng nhập |
| Báo cáo | `GET /api/reports/top-customers` | Admin |

## 5. Quyết định kiến trúc và lý do

### Tổng thể
| Quyết định | Lý do |
|---|---|
| Layered modular monolith, 1 Api + 1 Worker | Đủ cho MVP, dễ giải thích; không tách microservices chỉ để phức tạp hơn |
| Rule bất biến của một entity nằm trong entity; điều phối nhiều entity hoặc DB nằm ở Application service | Tránh anemic model, dễ test |
| Mỗi layer có `DependencyInjection.cs` riêng; `IPaymentMethod` mỗi khoá đúng một implementation | Composition root gọn; trùng khoá làm `ToDictionary` ném lỗi |
| Lifetime: Singleton chỉ cho thứ không trạng thái và không phụ thuộc Scoped; còn lại Scoped | Tránh captive dependency. `IVoucherFactory` phải đổi sang Scoped khi đọc DB |
| Bật `ValidateScopes` + `ValidateOnBuild` trong test composition | Bắt lỗi lifetime ngoài môi trường Development |
| Cấu hình thiếu hoặc yếu (MerchantAccount, connection string, `Jwt:Key` < 32 byte) thì app không khởi động | Fail-fast tốt hơn chạy rồi lỗi ngầm |

### Domain
| Quyết định | Lý do |
|---|---|
| `Order.AddItem` không trừ kho | Trừ kho cần transaction và DB; làm ở `OrderService` |
| `OrderItem.UnitPrice` sao chép giá lúc thêm | Đổi giá sau đó không ảnh hưởng đơn cũ |
| Tiền dùng `decimal`, DB `numeric(18,2)`, làm tròn `AwayFromZero` | Tránh sai số nhị phân; làm tròn banker's mặc định của C# cho kết quả bất ngờ |
| Id dùng `Guid.CreateVersion7()` | Có thứ tự theo thời gian, ít phân mảnh index |
| Ngừng khách bằng `IsActive`, không xoá cứng | Giữ lịch sử đơn (FK RESTRICT) |
| `Order.AddItem`: không tối ưu bằng dictionary với đơn nhỏ | O(k) mỗi lần thêm, O(n²) cho n lần, nhưng đơn thường 1–20 dòng; dictionary tốn bộ nhớ và phải giữ đồng bộ. Đo trước khi sửa |

### Giảm giá, thanh toán, sự kiện
| Quyết định | Lý do |
|---|---|
| `IDiscountStrategy` có `IsApplicable` và `CalculateDiscount`; Priority nhỏ áp dụng trước (1 = theo loại khách, 2 = voucher) | Tách "áp dụng cho ai" khỏi "giảm bao nhiêu"; thứ tự có test khoá (150.000 so với 145.000) |
| Tổng giảm bị kẹp theo phần còn lại; strategy trả số âm thì ném lỗi | Hợp đồng `[0, OrderTotal]` (LSP) |
| Loại khách không có chính sách: không giảm giá, không lỗi | Quyết định Ngày 5 |
| Một voucher mỗi đơn; mã voucher sai: `DomainException` | Không để khách bị tính đủ tiền trong im lặng |
| Factory chỉ cần khi mã voucher biết lúc chạy | Bảng cố định thì đăng ký từng strategy trong DI |
| Hoàn tiền là use case riêng (`RefundProcessor`), chỉ nhận `IRefundablePayment` | ISP: Cash không bị ép có `Refund` |
| Adapter cổng ngân hàng: số âm → `DomainException` trước khi gọi cổng; số tiền làm tròn về 0 → coi là thành công, không gọi cổng; mã lỗi ≠ 0 → `PaymentException` có `ErrorCode` | Quyết định Ngày 5 |
| Observer trong process: handler lỗi ghi log Error, không ném lại | Việc chính đã xong thì lỗi của bên phụ không được làm thất bại việc chính |
| Không dùng MediatR | Danh sách handler qua DI đủ cho nhu cầu hiện tại |

### Truy cập dữ liệu
| Quyết định | Lý do |
|---|---|
| `DbContext` trực tiếp qua `IAppDbContext`, không generic repository | EF Core đã là Unit of Work và repository; generic repository che `Include` và projection |
| `IAppDbContext` đặt ở Application | Đảo phụ thuộc và cho phép fake. Đánh đổi: Application tham chiếu EF Core |
| Truy vấn dùng lại nằm ở Application, không ở Domain | Domain không được biết EF |
| Ranh giới transaction ở Application service | Các tầng khác không cần biết |
| Ánh xạ bằng Fluent API trong Infrastructure | Domain không tham chiếu EF; làm được check constraint, `OnDelete`, index tổ hợp |
| `AppDbContext` Scoped; `IAppDbContext` trỏ cùng instance qua `AddScoped(sp => AppDbContext)` | `AddScoped<IAppDbContext, AppDbContext>()` sẽ tạo context thứ hai trong cùng request |
| Test persistence bằng PostgreSQL thật (Testcontainers + `MigrateAsync`) | InMemory không thực thi FK, unique, check, transaction |
| Đọc: `AsNoTracking` + projection; ghi: entity được theo dõi + phương thức Domain | Đọc nhẹ; ghi qua quy tắc Domain |

### API và xử lý lỗi
| Quyết định | Lý do |
|---|---|
| 400 = sai hình thức (DTO); 404 = không tồn tại; 409 = xung đột dữ liệu; 422 = vi phạm quy tắc Domain; 500 = lỗi lạ, không lộ chi tiết, có `traceId` | Một định dạng lỗi nhất quán, không `try/catch` trong controller |
| `DbUpdateException` nhóm `23xxx` → 409, body không chứa tên constraint | `RESTRICT` là `23001`, unique là `23505`, FK là `23503` |
| `UseExceptionHandler` đứng trước các middleware nó cần bảo vệ | Handler chỉ bắt lỗi từ phía sau nó |
| Attribute validation của DTO record đặt ở **tham số constructor**, không dùng `[property: ...]` | MVC ném `InvalidOperationException` với `property:` |
| `POST /api/pricing/quote` trả 200 | Chỉ tính, không tạo tài nguyên |
| Phân trang offset, `pageSize` ≤ 100, sắp xếp theo whitelist, luôn có `ThenBy(Id)` | An toàn, không lặp/bỏ sót dòng giữa các trang |
| `PUT` không sửa tồn kho; ngừng/kích hoạt khách bằng `POST .../deactivate` và `.../activate` (không có `DELETE`) | Tồn kho cần phép tính nguyên tử; giữ lịch sử đơn |
| Email trùng: `AnyAsync` (thân thiện) + unique index (bảo đảm, chặn race condition) | Index mới là chốt chặn cuối |

### Xác thực và phân quyền (Ngày 11)
| Quyết định | Lý do |
|---|---|
| Mật khẩu: PBKDF2 có muối ngẫu nhiên (`PasswordHasher` của Identity); chỉ lưu hash | Lộ DB vẫn phải dò từng mật khẩu, chậm |
| JWT HS256: claim `sub`, `email`, `role`; kiểm tra chữ ký, issuer, audience, hạn 30 phút, `ClockSkew` 30 giây; `MapInboundClaims = false`, `RoleClaimType = "role"` | Thiếu `RoleClaimType` thì `[Authorize(Roles)]` luôn 403 |
| Fallback policy: mọi endpoint yêu cầu đăng nhập; công khai chỉ `/health`, `/openapi`, register, login | An toàn mặc định |
| Đăng ký công khai luôn là Staff; nâng Admin bằng SQL | Chủ ý: chưa có quản lý người dùng |
| Đăng nhập sai: cùng thông báo cho email sai và mật khẩu sai; luôn chạy một lần băm | Chống dò tài khoản và đo thời gian |
| `JwtOptions` là nguồn duy nhất cho cả tạo và kiểm tra token | Không lệch cấu hình |
| `[AllowAnonymous]` đặt ở từng action, không đặt ở cấp class | Cấp class ghi đè mọi `[Authorize]` |

### Đơn hàng và đồng thời (Ngày 12)
| Quyết định | Lý do |
|---|---|
| Tạo đơn giữ hàng ngay (trạng thái Pending): `UPDATE ... SET Stock = Stock - @q WHERE Id = @id AND Stock >= @q`, kiểm tra số dòng bị ảnh hưởng | Kiểm tra và trừ là một câu lệnh nguyên tử; đọc rồi ghi trong ứng dụng mất cập nhật |
| Cả đơn trong một transaction; hết hàng ở dòng nào thì rollback toàn bộ | Không mất hàng mà không có đơn |
| Các dòng sắp theo `ProductId` trước khi khoá | Mọi giao dịch khoá cùng thứ tự, tránh deadlock |
| Hết hàng: `ConflictException` (409); khách/sản phẩm không có trong body: `DomainException` (422) | Trạng thái dữ liệu xung đột so với request sai nội dung |
| Huỷ đơn: cổng nguyên tử `UPDATE Orders SET Status = 'Cancelled' WHERE Status <> 'Cancelled'`, rồi hoàn kho | Hai request huỷ cùng lúc chỉ hoàn kho một lần |
| Quy tắc tồn kho nằm ở SQL cho đường đồng thời; `Product.DecreaseStock` còn cho luồng đơn lẻ; `CHECK Stock >= 0` là chốt chặn cuối | Ba lớp bảo vệ |
| Đã cân nhắc: optimistic (token phiên bản, thử lại) và pessimistic (`SELECT ... FOR UPDATE`) | Chỉ dùng khi luật phức tạp không viết được thành một `UPDATE` |

## 6. Database schema và migrations
| Bảng | Ràng buộc |
|---|---|
| Customers | PK; UNIQUE(Email) |
| Products | PK; CHECK Price >= 0; CHECK Stock >= 0; index Category, Name |
| Orders | PK; FK CustomerId → Customers (RESTRICT); index (CustomerId, CreatedAt) |
| OrderItems | PK; FK OrderId → Orders (CASCADE, NOT NULL); FK ProductId → Products (RESTRICT); UNIQUE(OrderId, ProductId); CHECK Quantity > 0; CHECK UnitPrice >= 0 |
| Users | PK; UNIQUE(Email); Role lưu dạng chuỗi |

Migrations: `InitialCreate`, `RequireOrderItemOrderId`, `AddUsers`.
Kiểu cột: `uuid`, `numeric(18,2)`, `timestamp with time zone` (luôn UTC; PostgreSQL lưu đến micro-giây nên cắt chữ số cuối của `DateTime` .NET), enum lưu dạng chuỗi.

### Thí nghiệm index (Ngày 10, 100k đơn, 300k dòng hàng)
| Thí nghiệm | Kết quả |
|---|---|
| Đơn của một khách, mới nhất trước | `Index Scan Backward` trên `(CustomerId, CreatedAt)`: 0,12 ms, không `Sort` |
| Đơn 7 ngày gần đây | `Seq Scan` 24,3 ms → `Index Only Scan` 0,48 ms sau khi thêm index `CreatedAt` (~50 lần) |
| `Status = 'Confirmed'` (100% dòng) | `Seq Scan` dù có index: index chọn lọc thấp không giúp ích |
| `lower(Name) LIKE '%x%'` | `Seq Scan`, ước lượng 1 so với thực tế 111; B-tree không dùng được cho wildcard đầu; hướng xử lý: `pg_trgm` |
| Báo cáo top khách | 441 ms; `COUNT(DISTINCT)` gây `Sort` external merge ra đĩa 15 MB; chấp nhận được ở quy mô này |

- `IX_Orders_CreatedAt`: giữ, thêm vào migration ở Ngày 13 (đã đo ~50 lần).
- `ReportService` luôn đúng 2 truy vấn, khoá bằng test đếm truy vấn (`DbCommandInterceptor`).

## 7. Cách chạy
```
docker compose up -d                 # cần file .env (POSTGRES_USER, POSTGRES_PASSWORD), không commit
dotnet user-secrets set "ConnectionStrings:Default" "<chuỗi kết nối>" --project src/BusinessManagement.Api
dotnet user-secrets set "Payments:Bank:MerchantAccount" "<số giả>" --project src/BusinessManagement.Api
dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên >= 32 byte>" --project src/BusinessManagement.Api
dotnet ef database update --project src/BusinessManagement.Infrastructure --startup-project src/BusinessManagement.Api
dotnet run --project src/BusinessManagement.Api
```
Nâng một tài khoản lên Admin (chưa có endpoint, chủ ý): `UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = '<email>';`, rồi đăng nhập lại để lấy token mới (token cũ vẫn mang vai trò cũ).

## 8. Cách chạy test
```
dotnet test                                                   # cần Docker Desktop đang chạy (test persistence)
dotnet test --filter "FullyQualifiedName~OrderServiceTests"
```

## 9. Lỗi đã biết / nợ kỹ thuật

**Nghiệp vụ và độ tin cậy**
- `OrderService` chưa dùng `PricingService`; đơn chưa lưu voucher và số tiền giảm (Ngày 13)
- Chưa có Payments và `Confirm`; đơn Pending giữ hàng mãi nếu bị bỏ rơi (cần job huỷ đơn quá hạn)
- Chưa có Idempotency-Key: gửi lại `POST /api/orders` tạo thêm một đơn
- Mọi người dùng đăng nhập đều xem và huỷ được đơn bất kỳ (chưa gắn đơn với người tạo)
- Giá sản phẩm được đọc trước transaction; đổi giá đồng thời có thể lệch vài mili-giây
- `Order.Cancel()` ở Domain không được `OrderService` dùng (huỷ bằng SQL); đồng bộ hoặc bỏ
- Khi làm Payments: tránh chuỗi thanh toán → ghi log → thông báo không atomic (Outbox, Ngày 17)
- `OrderEventPublisher` và handler chưa ai dùng (Ngày 14: `OrderCreated`); handler lỗi thì mất bản ghi
- `IVoucherFactory` Singleton: đổi sang Scoped khi đọc DB
- Chưa có `Customer.Segment`: ưu đãi theo loại khách chưa áp dụng cho đơn thật
- `OrderTotal` có hơn 2 chữ số thập phân vẫn được chấp nhận
- `PUT` sản phẩm và khách là "ghi đè cuối cùng thắng" (chưa có token phiên bản)
- Sắp xếp theo tên phụ thuộc collation PostgreSQL (tiếng Việt có dấu có thể sắp sai)
- Tìm kiếm `ToLower().Contains(...)` không dùng được index; cân nhắc trigram nếu bảng lớn
- `FileOrderLog` ghi đồng thời có thể ném `IOException` (tạm thời)

**Bảo mật**
- JWT stateless: user bị khoá hoặc đổi vai trò vẫn dùng được token cũ đến khi hết hạn; cần refresh token và thu hồi
- Chưa có rate limiting và khoá tài khoản khi đăng nhập sai nhiều lần
- 401 và 403 trả thân rỗng (chưa là ProblemDetails)
- Đăng ký trả 409 khi email trùng: lộ email nào đã đăng ký (chấp nhận với công cụ nội bộ)
- Chưa có quản lý người dùng và đổi mật khẩu

**Quan sát**
- `traceId` của 400 tự động (W3C) khác định dạng của 422, 404, 409 (Kestrel); Ngày 13 thống nhất bằng `X-Request-Id`

**Nợ dọn dẹp (cần xác nhận đã làm chưa)**
- `Directory.Build.props`, README, xoá `WeatherForecast*` / `.http` / `Class1.cs`, xoá file `log` ở gốc
- Đổi tên namespace `Infrastructure.PaymentMethod` / `.Notify` / `.OrderLog` theo vai trò
- `LegacyBankPaymentAdapter`: kiểm tra việc kiểm tra số 0 đã đặt **sau** bước làm tròn
- `OrderService.CreateAsync`: kiểm tra đã chặn `Guid.Empty` của `customerId` và `productId` (hai test trả 422)

## 10. Phần còn thiếu
Voucher/giảm giá trong đơn, danh sách đơn, Payments, RabbitMQ + Worker, Docker Compose đầy đủ (Api, Worker, RabbitMQ), README, CI.

## 11. Công việc tiếp theo
Ngày 13: tích hợp `PricingService` vào đơn (lưu `DiscountAmount`, `VoucherCode`), `GET /api/orders` có lọc và phân trang, logging có ngữ cảnh và middleware `X-Request-Id`. Ngày 14: khái niệm message queue, RabbitMQ bằng Docker Compose, publish `OrderCreated`.

## 12. Câu hỏi phỏng vấn đã học
- `IEnumerable` và `IQueryable`
- Vì sao tránh `async void`; `.Result` gây deadlock thế nào; `Task.WhenAll` khi một task lỗi
- Interface và abstract class; composition over inheritance
- SOLID bằng ví dụ của dự án; DIP, DI và IoC container
- DI lifetimes, captive dependency, `ValidateScopes` và `ValidateOnBuild`
- Làm sao biết unit test không "xanh giả" (thử nghiệm đột biến, khoá lý do lỗi, kiểm tra hậu quả)
- Strategy, Factory, Adapter; Adapter và Decorator
- Observer trong process và message queue; vì sao không làm `Process` thất bại khi handler lỗi
- Có cần Repository khi dùng EF Core không
- ASP.NET Core request pipeline và nơi xử lý exception
- Vì sao thêm constraint ở DB khi Domain đã validate
- Phân trang, lọc, sắp xếp trong REST API và các cạm bẫy (tie-breaker, whitelist, offset và keyset)
- N+1 là gì; phát hiện bằng log SQL và bộ đếm truy vấn; cách sửa
- Lưu mật khẩu an toàn và JWT hoạt động thế nào; 401 và 403
- Chống bán vượt tồn khi nhiều request đồng thời (cập nhật nguyên tử, transaction, thứ tự khoá, test song song)
- Xử lý lỗ hổng trong transitive dependency (GHSA-v5pm-xwqc-g5wc)

## 13. Bài học đã rút (câu chuyện phỏng vấn)

**Kiểm thử**
- `Assert.Throws` đúng với mọi exception cùng loại: phải khoá lý do và kiểm tra hậu quả
- Test dùng dữ liệu riêng (Guid trong email, nhóm riêng) vì cả collection dùng chung một DB
- Test không gọi đúng hàm của service, hoặc đọc lại bằng cùng `DbContext`, chỉ tự so sánh với chính nó: đọc lại bằng `DbContext` mới
- Khoá lỗi N+1 bằng test đếm truy vấn (`DbCommandInterceptor`), khoá lỗi đồng thời bằng test song song trên DB thật; viết một test "ngây thơ" để ghi lại lỗi (`Stock` còn 4 sau 10 lần bán từ 5 hàng)

**DI và cấu hình**
- `ValidateOnBuild` một mình không bắt được captive dependency; cần bật cả `ValidateScopes`
- Volume Docker giữ thông tin đăng nhập của lần khởi tạo đầu; đổi mật khẩu hoặc cổng phải `docker compose down -v`

**Database và EF Core**
- `ON DELETE RESTRICT` được PostgreSQL báo bằng `23001`, không phải `23503`; khi map lỗi DB sang HTTP, coi nhóm `23xxx` là xung đột dữ liệu
- Shadow FK mặc định cho phép NULL; phải `IsRequired()`. Migration đã áp dụng và chia sẻ thì thêm migration mới, không sửa cũ
- `AlterColumn` từ nullable sang `NOT NULL` khiến EF sinh `defaultValue`, và Npgsql tạo DEFAULT thật trong DB. Xoá dòng đó khỏi file sau khi đã áp dụng thì không gỡ được, vì EF chỉ `DROP DEFAULT` khi biết default cũ. Phải xoá `defaultValue` trước khi áp dụng lần đầu, hoặc chạy `ALTER ... DROP DEFAULT` bằng tay
- LINQ trả về truy vấn mới: quên gán lại thì bộ lọc im lặng biến mất; test có `TotalCount` trên dữ liệu có sẵn mới bắt được
- Index chỉ giúp khi điều kiện chọn lọc; đọc `EXPLAIN` (loại node, ước lượng so với thực tế, `Buffers`) trước khi thêm index
- `COUNT(DISTINCT)` trên nhiều dòng có thể buộc `Sort` ra đĩa

**Bảo mật và API**
- Payload JWT không mã hoá: không đưa dữ liệu nhạy cảm vào; token cũ giữ vai trò cũ đến khi hết hạn
- Thiếu `RoleClaimType` thì `[Authorize(Roles)]` luôn 403; `[AllowAnonymous]` ở cấp class ghi đè mọi `[Authorize]`
- 404 do routing (không khớp `{id:guid}`) khác 404 do `NotFoundException`: chỉ loại sau mới có ProblemDetails
- `FirstOrDefault` trên kiểu giá trị trả về giá trị mặc định (`Guid.Empty`), không phân biệt "không tìm thấy" với "tìm thấy giá trị mặc định"

**Đồng thời**
- Đọc, kiểm tra, ghi trong ứng dụng mất cập nhật; chuyển thành một câu `UPDATE` có điều kiện và kiểm tra số dòng bị ảnh hưởng
- Khoá theo thứ tự nhất quán để tránh deadlock; cổng trạng thái nguyên tử để chống làm hai lần (hoàn kho hai lần)

## Quyết định (Ngày 13)
- Đơn lưu DiscountAmount và VoucherCode (chụp lúc đặt, như UnitPrice); Total = Subtotal - DiscountAmount
- Voucher kiểm tra TRƯỚC transaction: mã sai không chạm kho, transaction giữ khoá ngắn hơn
- Danh sách đơn: một truy vấn đếm + một truy vấn trang (khoá bằng test đếm truy vấn); sắp xếp có ThenBy(Id)
- X-Request-Id: nhận từ client chỉ khi khớp ^[A-Za-z0-9_-]{8,64}$, nếu không sinh mới; có trong log (scope), header và lỗi
- Log: message template, chỉ ghi Id (không email, token, mật khẩu); log "đã tạo đơn" ghi sau commit
## Nợ kỹ thuật (thêm)
- Customer.Segment chưa có (đang dùng "Standard"); sản phẩm giá 0 + voucher ra 422 khó hiểu
- 400 tự động của MVC vẫn dùng traceId W3C

## Quyết định (Ngày 14)
- Exchange topic bền vững bm.events; queue bm.orders.created durable, gắn DLX bm.events.dlx → bm.orders.created.dlq (khai báo sẵn vì tham số queue không đổi được sau khi tạo)
- Tin persistent + publisher confirm; mandatory = true; một kết nối dùng chung, mỗi lần publish một channel
- Publish sau commit; lỗi publish chỉ ghi log Error, không làm hỏng đơn; publish dùng timeout 5 giây, không dùng ct của request
- Sự kiện chỉ chứa Id và số tiền (không email); EventId để consumer chống trùng
- Topology khai báo ở cả publisher để tin không rơi khi Worker chưa chạy

## Nợ kỹ thuật (thêm)
- Dual write: đơn đã commit mà publish lỗi thì mất sự kiện; không có thử lại → Transactional Outbox (Ngày 17)
- Chưa có consumer (Ngày 15), retry/DLQ thật và idempotency (Ngày 16)
- Chưa dùng quorum queue và publisher channel pooling
- RabbitMQ chưa nằm trong Compose cho Api/Worker (Ngày 18)