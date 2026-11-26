# PROJECT STATUS

> Cập nhật lần cuối: Ngày 15 hoàn tất (212 test xanh)

## 1. Mục tiêu và phạm vi MVP
Business Management API (ASP.NET Core): Auth, Products, Customers, Orders, Payments, RabbitMQ worker.

| Bắt buộc | Làm sau | Chưa cần |
|---|---|---|
| Auth (JWT, role), CRUD Products/Customers, Orders + trừ kho trong transaction, Payments (bản ghi nghiệp vụ), validation, logging, unit + integration test, Docker Compose, RabbitMQ (retry, DLQ, idempotent consumer) | Permission chi tiết, Transactional Outbox, Idempotency-Key, Kafka demo | Microservices, CQRS/MediatR, thanh toán thật, frontend |

Tiến độ MVP:
- [x] Auth: đăng ký, đăng nhập, JWT, phân quyền theo vai trò
- [x] CRUD Products, Customers (phân trang, lọc, sắp xếp)
- [x] Orders: tạo đơn giữ hàng trong transaction, chống bán vượt tồn, huỷ đơn hoàn kho
- [x] Voucher/giảm giá lưu trong đơn, danh sách đơn có lọc và phân trang
- [x] Logging có ngữ cảnh (`X-Request-Id`, structured logging)
- [x] Báo cáo top khách (2 truy vấn, đã đo bằng EXPLAIN)
- [x] RabbitMQ: publish `OrderCreated` sau commit (Ngày 14)
- [x] Worker: consumer tạo thông báo, `ack` thủ công, DLQ (Ngày 15, hoàn tất)
- [ ] Idempotent consumer, thử lại có độ trễ (Ngày 16)
- [ ] Transactional Outbox (Ngày 17)
- [ ] Payments và Confirm đơn
- [ ] Docker Compose đầy đủ, README, CI (Ngày 18–19)

## 2. Công nghệ và phiên bản
- .NET SDK 10.0.300 / runtime 10.0.14 (LTS, hỗ trợ đến 11/2028)
- ASP.NET Core Web API (controllers); `Microsoft.OpenApi` ghim **2.7.5**
- EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3
- PostgreSQL 18 (Docker Compose, cổng 5434)
- Xác thực: `JwtBearer` + `Microsoft.IdentityModel.JsonWebTokens`; băm mật khẩu bằng `PasswordHasher` của ASP.NET Core Identity (PBKDF2)
- RabbitMQ 4 (image `rabbitmq:4-management`; AMQP 5672, giao diện quản lý 15672) + `RabbitMQ.Client` 7.x (API bất đồng bộ)
- xUnit 2.9.3, Testcontainers (PostgreSql, RabbitMq)

## 3. Cấu trúc thư mục (đối chiếu với thực tế)
```
src/
  BusinessManagement.Domain/          Common, Entities (Product, Customer, Order, OrderItem, OrderStatus, User, Notification)
  BusinessManagement.Application/     Abstractions (IAppDbContext), Auth, Common, Customers, Products, Orders, Reports,
                                      Messaging (OrderCreated, IEventBus, RoutingKeys), Notifications (IOrderCreatedHandler),
                                      Checkout/{Discount,Events,Payments,Ports,Pricing}, DependencyInjection.cs
  BusinessManagement.Infrastructure/  Persistence/{Configurations,Migrations}, Auth (hasher, JWT), Messaging (RabbitMqEventBus,
                                      RabbitMqOptions, RabbitMqTopology), cài đặt thanh toán/thông báo/log đơn,
                                      DependencyInjection.cs (AddInfrastructure, AddPersistence, AddMessaging)
  BusinessManagement.Api/             Controllers, Contracts/{Requests,Responses}, ExceptionHandling, Middleware (RequestIdMiddleware), Program.cs
  BusinessManagement.Worker/          OrderCreatedConsumer (BackgroundService), WorkerServiceExtensions
tests/
  BusinessManagement.UnitTests/        Domain + Application (logic thuần)
  BusinessManagement.IntegrationTests/ CompositionRoot, Api (WebApplicationFactory), Services, Persistence, RabbitMQ (Testcontainers)
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
| 13 | Voucher trong đơn, danh sách đơn, logging có ngữ cảnh | ✅ |
| 14 | Message queue, RabbitMQ, publish `OrderCreated` | ✅ (208 test xanh) |
| 15 | Worker làm consumer, `ack`, DLQ | 🔄 chờ xác nhận test chạy xanh |

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
| Orders | `POST /api/orders` (có `voucherCode`), `GET /api/orders` (lọc khách, trạng thái, khoảng ngày; phân trang), `GET /api/orders/{id}`, `POST /api/orders/{id}/cancel` | Đăng nhập |
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
| Cấu hình thiếu hoặc yếu (MerchantAccount, connection string, `Jwt:Key` < 32 byte, RabbitMQ) thì app không khởi động | Fail-fast tốt hơn chạy rồi lỗi ngầm |
| Tách `AddPersistence` và `AddMessaging` khỏi `AddInfrastructure` | Worker chỉ cần DB và RabbitMQ, không cần JWT hay thanh toán |

### Domain
| Quyết định | Lý do |
|---|---|
| `Order.AddItem` không trừ kho | Trừ kho cần transaction và DB; làm ở `OrderService` |
| `OrderItem.UnitPrice` và `Order.DiscountAmount`/`VoucherCode` chụp tại thời điểm đặt | Đổi giá hoặc voucher sau đó không làm đổi đơn cũ (hoá đơn và kế toán) |
| Tiền dùng `decimal`, DB `numeric(18,2)`, làm tròn `AwayFromZero` | Tránh sai số nhị phân; làm tròn banker's mặc định của C# cho kết quả bất ngờ |
| Id dùng `Guid.CreateVersion7()` | Có thứ tự theo thời gian, ít phân mảnh index |
| Ngừng khách bằng `IsActive`, không xoá cứng | Giữ lịch sử đơn (FK RESTRICT) |
| `Order.AddItem`: không tối ưu bằng dictionary với đơn nhỏ | O(k) mỗi lần thêm, O(n²) cho n lần, nhưng đơn thường 1–20 dòng; dictionary tốn bộ nhớ và phải giữ đồng bộ. Đo trước khi sửa |

### Giảm giá, thanh toán, sự kiện trong process
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
| Guid bắt buộc kiểm tra bằng `[NotEmptyGuid]` ở DTO (thiếu hoặc `0000…` là 400) | `[Required]` vô tác dụng với kiểu giá trị không-nullable (thiếu → `Guid.Empty`, không phải null) |
| Action `GET` dùng `[FromQuery]` với lớp request riêng (`ProductListRequest`, `OrderListRequest`) | Kiểu phức hợp mặc định bind từ thân request; lớp request cho validation và `DateTimeOffset` → UTC |
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

### Voucher, danh sách đơn, logging (Ngày 13)
| Quyết định | Lý do |
|---|---|
| Voucher kiểm tra TRƯỚC transaction giữ hàng | Mã sai không chạm kho; transaction (đang giữ khoá dòng) ngắn hơn |
| `Total = Subtotal - DiscountAmount`; `Order.ApplyDiscount` kiểm tra không âm và không vượt `Subtotal` | Đơn tự nhất quán |
| Danh sách đơn: một truy vấn đếm + một truy vấn trang; `Sum` và `Count` trong `Select` được EF dịch thành một câu SQL, không phải N+1 | Khoá bằng test đếm truy vấn |
| `X-Request-Id`: nhận từ client chỉ khi khớp `^[A-Za-z0-9_-]{8,64}$`, nếu không sinh mới; có trong log (scope), header phản hồi và body lỗi | Chống log injection và log tràn; tra log theo một mã |
| Log dùng message template, chỉ ghi Id (không email, token, mật khẩu); log "đã tạo đơn" ghi sau commit; môi trường không phải Development ghi JSON | Trường lọc được; commit lỗi thì không báo đã tạo |

### Messaging (Ngày 14)
| Quyết định | Lý do |
|---|---|
| Exchange `topic` bền vững `bm.events`; queue `bm.orders.created` durable, gắn DLX `bm.events.dlx` → `bm.orders.created.dlq` | Khai báo DLX từ đầu vì tham số queue không đổi được sau khi tạo |
| Tin persistent + publisher confirm; `mandatory = true`; một kết nối dùng chung, mỗi lần publish một channel | Không mất tin khi broker khởi động lại; tin sai routing key không biến mất im lặng |
| Publish sau commit; lỗi publish chỉ ghi log Error, không làm hỏng đơn; timeout 5 giây, không dùng `ct` của request | Đơn đã commit rồi thì khách ngắt kết nối không được làm mất sự kiện |
| Sự kiện chỉ chứa Id và số tiền (không email); `EventId` để consumer chống trùng | Không rò dữ liệu cá nhân; chuẩn bị idempotency |
| Topology khai báo ở cả publisher | Exchange không có queue nào gắn thì tin bị bỏ; không để rơi khi Worker chưa chạy |
| Đã quan sát: tắt RabbitMQ → `POST /api/orders` vẫn 201, log `Failed to publish OrderCreated` (`BrokerUnreachableException`); tin của đơn đó không được gửi lại | Dual write: nền tảng cho Transactional Outbox (Ngày 17) |

### Worker và consumer (Ngày 15, hoàn tất)
| Quyết định | Lý do |
|---|---|
| Worker là dự án riêng; chỉ đăng ký DB và RabbitMQ (`WorkerServiceExtensions`) | Không kéo JWT/thanh toán vào tiến trình nền |
| `autoAck = false`; `ack` sau khi xử lý xong; prefetch 10; một scope DI cho mỗi tin | Tin chỉ bị xoá khi xử lý xong; `DbContext` là Scoped, `BackgroundService` là Singleton |
| Tin không đọc được (JSON hỏng) hoặc handler lỗi: `nack(requeue: false)` → DLQ | Tin độc không được lặp vô hạn |
| Tắt êm: không `ack` tin đang xử lý; broker giao lại | Không mất tin khi triển khai lại |
| Kết nối lần đầu thất bại thì thử lại sau 5 giây | Tự khôi phục của client chỉ chạy sau lần kết nối đầu tiên |
| `Notifications` không có unique trên `OrderId` (chủ ý) | Test ghi lại hành vi "tin trùng ra hai thông báo"; Ngày 16 chống trùng |

## 6. Database schema và migrations
| Bảng | Ràng buộc |
|---|---|
| Customers | PK; UNIQUE(Email) |
| Products | PK; CHECK Price >= 0; CHECK Stock >= 0; index Category, Name |
| Orders | PK; FK CustomerId → Customers (RESTRICT); DiscountAmount `numeric(18,2)` CHECK >= 0; VoucherCode `varchar(30)`; index (CustomerId, CreatedAt); index CreatedAt |
| OrderItems | PK; FK OrderId → Orders (CASCADE, NOT NULL); FK ProductId → Products (RESTRICT); UNIQUE(OrderId, ProductId); CHECK Quantity > 0; CHECK UnitPrice >= 0 |
| Users | PK; UNIQUE(Email); Role lưu dạng chuỗi |
| Notifications | PK; index OrderId (không unique, chủ ý) |

Migrations: `InitialCreate`, `RequireOrderItemOrderId`, `AddUsers`, `AddOrderDiscount`, `AddNotifications`.
Kiểu cột: `uuid`, `numeric(18,2)`, `timestamp with time zone` (luôn UTC; PostgreSQL lưu đến micro-giây nên cắt chữ số cuối của `DateTime` .NET), enum lưu dạng chuỗi.

### Topology RabbitMQ
| Thành phần | Cấu hình |
|---|---|
| Exchange `bm.events` | `topic`, durable |
| Queue `bm.orders.created` | durable; `x-dead-letter-exchange = bm.events.dlx`, `x-dead-letter-routing-key = order.created`; binding `order.created` |
| Exchange `bm.events.dlx` | `direct`, durable |
| Queue `bm.orders.created.dlq` | durable; binding `order.created` trên DLX |

### Thí nghiệm index (Ngày 10, 100k đơn, 300k dòng hàng)
| Thí nghiệm | Kết quả |
|---|---|
| Đơn của một khách, mới nhất trước | `Index Scan Backward` trên `(CustomerId, CreatedAt)`: 0,12 ms, không `Sort` |
| Đơn 7 ngày gần đây | `Seq Scan` 24,3 ms → `Index Only Scan` 0,48 ms sau khi thêm index `CreatedAt` (~50 lần) |
| `Status = 'Confirmed'` (100% dòng) | `Seq Scan` dù có index: index chọn lọc thấp không giúp ích |
| `lower(Name) LIKE '%x%'` | `Seq Scan`, ước lượng 1 so với thực tế 111; B-tree không dùng được cho wildcard đầu; hướng xử lý: `pg_trgm` |
| Báo cáo top khách | 441 ms; `COUNT(DISTINCT)` gây `Sort` external merge ra đĩa 15 MB; chấp nhận được ở quy mô này |

- `ReportService` và danh sách đơn luôn đúng 2 truy vấn, khoá bằng test đếm truy vấn (`DbCommandInterceptor`).

## 7. Cách chạy
```
docker compose up -d                 # PostgreSQL + RabbitMQ; cần file .env (POSTGRES_USER, POSTGRES_PASSWORD,
                                     # RABBITMQ_USER, RABBITMQ_PASSWORD), không commit
dotnet user-secrets set "ConnectionStrings:Default" "<chuỗi kết nối>" --project src/BusinessManagement.Api
dotnet user-secrets set "Payments:Bank:MerchantAccount" "<số giả>" --project src/BusinessManagement.Api
dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên >= 32 byte>" --project src/BusinessManagement.Api
dotnet user-secrets set "RabbitMq:Password" "<mật khẩu trong .env>" --project src/BusinessManagement.Api
dotnet ef database update --project src/BusinessManagement.Infrastructure --startup-project src/BusinessManagement.Api
dotnet run --project src/BusinessManagement.Api
dotnet run --project src/BusinessManagement.Worker     # tiến trình nền nhận OrderCreated (dùng chung UserSecretsId với Api)
```
- Giao diện RabbitMQ: `http://localhost:15672` (tài khoản `RABBITMQ_USER` trong `.env`; không dùng được `guest` qua cổng Docker).
- Nâng một tài khoản lên Admin (chưa có endpoint, chủ ý): `UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = '<email>';`, rồi đăng nhập lại để lấy token mới (token cũ vẫn mang vai trò cũ).
- `RABBITMQ_DEFAULT_USER/PASS` chỉ có tác dụng khi volume còn trống: đổi mật khẩu phải `rabbitmqctl change_password` hoặc xoá volume `bm-rabbitdata`.

## 8. Cách chạy test
```
dotnet test                                                   # cần Docker Desktop đang chạy (PostgreSQL và RabbitMQ thật)
dotnet test --filter "FullyQualifiedName~OrderServiceTests"
```

## 9. Lỗi đã biết / nợ kỹ thuật

**Nghiệp vụ và độ tin cậy**
- Dual write: đơn đã commit mà publish lỗi thì mất sự kiện và không có thử lại → Transactional Outbox (Ngày 17); broker chậm (không phải bị từ chối) có thể kéo `POST /api/orders` chậm tới 5 giây
- Consumer chưa idempotent: tin giao hai lần tạo hai thông báo (test ghi lại hành vi này) → Ngày 16
- Chưa có thử lại có độ trễ và công cụ phát lại tin từ DLQ (Ngày 16)
- Chưa có Payments và `Confirm`; đơn Pending giữ hàng mãi nếu bị bỏ rơi (cần job huỷ đơn quá hạn)
- Chưa có Idempotency-Key: gửi lại `POST /api/orders` tạo thêm một đơn
- Mọi người dùng đăng nhập đều xem và huỷ được đơn bất kỳ (chưa gắn đơn với người tạo)
- Giá sản phẩm được đọc trước transaction; đổi giá đồng thời có thể lệch vài mili-giây
- `Order.Cancel()` ở Domain không được `OrderService` dùng (huỷ bằng SQL); đồng bộ hoặc bỏ
- Khi làm Payments: tránh chuỗi thanh toán → ghi log → thông báo không atomic (Outbox)
- `IVoucherFactory` Singleton: đổi sang Scoped khi đọc DB
- Chưa có `Customer.Segment`: ưu đãi theo loại khách chưa áp dụng cho đơn thật (đang dùng "Standard"); sản phẩm giá 0 + voucher ra 422 khó hiểu
- `OrderTotal` của `/api/pricing/quote` có hơn 2 chữ số thập phân vẫn được chấp nhận
- `PUT` sản phẩm và khách là "ghi đè cuối cùng thắng" (chưa có token phiên bản)
- Sắp xếp theo tên phụ thuộc collation PostgreSQL (tiếng Việt có dấu có thể sắp sai)
- Tìm kiếm `ToLower().Contains(...)` không dùng được index; cân nhắc trigram nếu bảng lớn
- Chưa dùng quorum queue và publisher channel pooling; Worker dispatch tuần tự (`ConsumerDispatchConcurrency = 1`)
- `FileOrderLog` ghi đồng thời có thể ném `IOException` (tạm thời)

**Bảo mật**
- JWT stateless: user bị khoá hoặc đổi vai trò vẫn dùng được token cũ đến khi hết hạn; cần refresh token và thu hồi
- Chưa có rate limiting và khoá tài khoản khi đăng nhập sai nhiều lần
- 401 và 403 trả thân rỗng (chưa là ProblemDetails)
- Đăng ký trả 409 khi email trùng: lộ email nào đã đăng ký (chấp nhận với công cụ nội bộ)
- Chưa có quản lý người dùng và đổi mật khẩu

**Quan sát**
- `traceId` của 400 tự động (W3C) khác định dạng của 422, 404, 409 (Kestrel); đã có `X-Request-Id` nhưng chưa thống nhất hai loại

**Cần xác nhận đã làm chưa**
- Test `PublishAsync_UnroutableRoutingKey_ObserveBehavior` chưa có assert: ghi lại loại exception (dự đoán `PublishReturnException`) và khoá bằng `Assert.IsType`
- `GET /api/orders` dùng `[FromQuery] OrderListRequest` (có validation, `DateTimeOffset`), không nhận thẳng `OrderListQuery`
- Test danh sách đơn theo trạng thái và theo ngày lọc theo `customerId`, không dùng số tổng toàn DB (`Assert.Equal(49, ...)`)
- Dữ liệu test voucher dùng đơn vị đồng (`SALE50K` = 50.000), không sửa nghiệp vụ cho khớp test
- `[NotEmptyGuid]` đã áp dụng cho `CustomerId`/`ProductId` của `CreateOrderRequest`
- `Directory.Build.props`, README, xoá `WeatherForecast*` / `.http` / `Class1.cs`, xoá file `log` ở gốc
- Đổi tên namespace `Infrastructure.PaymentMethod` / `.Notify` / `.OrderLog` theo vai trò
- `LegacyBankPaymentAdapter`: kiểm tra việc kiểm tra số 0 đã đặt **sau** bước làm tròn

## 10. Phần còn thiếu
Idempotent consumer và thử lại có độ trễ (Ngày 16), Transactional Outbox (Ngày 17), Payments, Docker Compose đầy đủ (Api, Worker, RabbitMQ), README, CI.

## 11. Công việc tiếp theo
Ngày 16: bảng `ProcessedMessages` chống xử lý lặp, thử lại có độ trễ trước khi vào DLQ, đảo kỳ vọng test "tin trùng ra hai thông báo".

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
- Làm log hữu ích khi debug production (structured logging, correlation id, không log dữ liệu cá nhân)
- Khi nào dùng message queue và nó đem lại vấn đề gì (ít nhất một lần, thứ tự, nhất quán cuối cùng, dual write, outbox)
- Làm consumer tin cậy (ack thủ công, idempotency, prefetch, DLQ cho tin độc, scope DI mỗi tin, tắt êm)
- Xử lý lỗ hổng trong transitive dependency (GHSA-v5pm-xwqc-g5wc)

## 13. Bài học đã rút (câu chuyện phỏng vấn)

**Kiểm thử**
- `Assert.Throws` đúng với mọi exception cùng loại: phải khoá lý do và kiểm tra hậu quả
- Test dùng dữ liệu riêng (Guid trong email, nhóm riêng, lọc theo `customerId`) vì cả collection dùng chung một DB; không khẳng định số lượng toàn cục
- Test không gọi đúng hàm của service, hoặc đọc lại bằng cùng `DbContext`, chỉ tự so sánh với chính nó: đọc lại bằng `DbContext` mới
- Khoá lỗi N+1 bằng test đếm truy vấn (`DbCommandInterceptor`), khoá lỗi đồng thời bằng test song song trên DB thật; viết một test "ngây thơ" để ghi lại lỗi (`Stock` còn 4 sau 10 lần bán từ 5 hàng)
- Test "ObserveBehavior" phải kết thúc bằng một assert, nếu không nó luôn xanh
- Test cần cấu hình riêng (Testcontainers) phải ghi đè cấu hình thật; nếu không nó nối vào DB và RabbitMQ của môi trường dev

**DI và cấu hình**
- `ValidateOnBuild` một mình không bắt được captive dependency; cần bật cả `ValidateScopes`
- Volume Docker giữ thông tin đăng nhập của lần khởi tạo đầu (PostgreSQL và RabbitMQ): đổi mật khẩu hoặc cổng phải xoá volume hoặc đổi mật khẩu trong chính dịch vụ
- Tham số của queue RabbitMQ (như DLX) không đổi được sau khi tạo: khai báo đúng từ đầu

**Database và EF Core**
- `ON DELETE RESTRICT` được PostgreSQL báo bằng `23001`, không phải `23503`; khi map lỗi DB sang HTTP, coi nhóm `23xxx` là xung đột dữ liệu
- Shadow FK mặc định cho phép NULL; phải `IsRequired()`. Migration đã áp dụng và chia sẻ thì thêm migration mới, không sửa cũ
- `AlterColumn` từ nullable sang `NOT NULL` khiến EF sinh `defaultValue`, và Npgsql tạo DEFAULT thật trong DB. Xoá dòng đó khỏi file sau khi đã áp dụng thì không gỡ được, vì EF chỉ `DROP DEFAULT` khi biết default cũ. Phải xoá `defaultValue` trước khi áp dụng lần đầu, hoặc chạy `ALTER ... DROP DEFAULT` bằng tay
- LINQ trả về truy vấn mới: quên gán lại thì bộ lọc im lặng biến mất; test có `TotalCount` trên dữ liệu có sẵn mới bắt được
- Index chỉ giúp khi điều kiện chọn lọc; đọc `EXPLAIN` (loại node, ước lượng so với thực tế, `Buffers`) trước khi thêm index
- `COUNT(DISTINCT)` trên nhiều dòng có thể buộc `Sort` ra đĩa
- `Sum`/`Count` trong `Select` của EF thành một câu SQL, không phải N+1; chứng minh bằng đếm truy vấn

**Bảo mật và API**
- Payload JWT không mã hoá: không đưa dữ liệu nhạy cảm vào; token cũ giữ vai trò cũ đến khi hết hạn
- Thiếu `RoleClaimType` thì `[Authorize(Roles)]` luôn 403; `[AllowAnonymous]` ở cấp class ghi đè mọi `[Authorize]`
- 404 do routing (không khớp `{id:guid}`) khác 404 do `NotFoundException`: chỉ loại sau mới có ProblemDetails
- `FirstOrDefault` trên kiểu giá trị trả về giá trị mặc định (`Guid.Empty`), không phân biệt "không tìm thấy" với "tìm thấy giá trị mặc định"; `[Required]` cũng vô tác dụng với `Guid`
- Action `GET` nhận kiểu phức hợp mà không có `[FromQuery]` sẽ bind từ thân request
- Không sửa dữ liệu nghiệp vụ cho khớp test; sửa dữ liệu test

**Đồng thời và messaging**
- Đọc, kiểm tra, ghi trong ứng dụng mất cập nhật; chuyển thành một câu `UPDATE` có điều kiện và kiểm tra số dòng bị ảnh hưởng
- Khoá theo thứ tự nhất quán để tránh deadlock; cổng trạng thái nguyên tử để chống làm hai lần (hoàn kho hai lần)
- Ghi hai hệ thống (DB và broker) không atomic: commit xong publish lỗi thì mất sự kiện; log "đã tạo" phải ghi sau commit
- `autoAck = true` xoá tin ngay khi giao nên consumer chết là mất tin; `ack` thủ công sau khi xử lý và giao ít nhất một lần nên consumer phải idempotent
- Tin độc phải `nack(requeue: false)` vào DLQ; `requeue: true` làm tin lặp vô hạn
- Dùng `IServiceScopeFactory` tạo scope cho mỗi tin: `BackgroundService` là Singleton, `DbContext` là Scoped