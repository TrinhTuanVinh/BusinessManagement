# PROJECT STATUS

> Cập nhật lần cuối: Ngày 19 hoàn tất (231 test xanh, CI xanh, đã chạy lại từ bản clone sạch theo README)

## 1. Mục tiêu và phạm vi MVP
Business Management API (ASP.NET Core): Auth, Products, Customers, Orders, RabbitMQ worker.

| Bắt buộc | Làm sau | Chưa cần |
|---|---|---|
| Auth (JWT, role), CRUD Products/Customers, Orders + trừ kho trong transaction, validation, logging, unit + integration test, Docker Compose, RabbitMQ (retry, DLQ, idempotent consumer, outbox) | Payments, Permission chi tiết, Idempotency-Key, Kafka demo | Microservices, CQRS/MediatR, thanh toán thật, frontend |

Tiến độ MVP:
- [x] Auth: đăng ký, đăng nhập, JWT, phân quyền theo vai trò
- [x] CRUD Products, Customers (phân trang, lọc, sắp xếp)
- [x] Orders: giữ hàng trong transaction, chống bán vượt tồn, huỷ đơn hoàn kho, voucher, danh sách đơn
- [x] Logging có ngữ cảnh (`X-Request-Id`, structured logging); báo cáo top khách
- [x] RabbitMQ: Transactional Outbox + relay, consumer `ack` thủ công, thử lại có độ trễ, DLQ, idempotent consumer
- [x] Docker Compose (Api, Worker, PostgreSQL, RabbitMQ), CI (GitHub Actions), README, test HTTP đầu cuối
- [ ] Payments và Confirm đơn (chỉ có mã mẫu chưa nối vào luồng đơn)

## 2. Công nghệ và phiên bản
- .NET SDK 10.0.300 / runtime 10.0.14 (LTS, hỗ trợ đến 11/2028)
- ASP.NET Core Web API (controllers), OpenAPI + giao diện Swagger; `Microsoft.OpenApi` 2.12.x
- EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3; PostgreSQL 18
- Xác thực: `JwtBearer` + `Microsoft.IdentityModel.JsonWebTokens`; `PasswordHasher` của ASP.NET Core Identity (PBKDF2)
- RabbitMQ 4 (`rabbitmq:4-management`) + `RabbitMQ.Client` 7.x
- xUnit 2.9.3, Testcontainers (PostgreSql, RabbitMq)
- Docker (Dockerfile nhiều giai đoạn, chạy không phải root), Docker Compose, GitHub Actions

## 3. Cấu trúc thư mục (đối chiếu với thực tế)
```
src/
  BusinessManagement.Domain/          entity và bất biến (Product, Customer, Order, OrderItem, User, Notification,
                                      OutboxMessage, ProcessedMessage), DomainException
  BusinessManagement.Application/     use case (Orders, Products, Customers, Auth, Reports), Messaging (OrderCreated, IEventBus),
                                      Notifications (OrderCreatedHandler), Checkout (Discount, Pricing, Payments mẫu)
  BusinessManagement.Infrastructure/  Persistence (Configurations, Migrations), Auth, Messaging (RabbitMqEventBus, Topology,
                                      OutboxRelayService), DependencyInjection (AddInfrastructure, AddPersistence, AddMessaging)
  BusinessManagement.Api/             Controllers, Contracts, ExceptionHandling, Middleware (RequestId), Program.cs
  BusinessManagement.Worker/          OutboxRelayWorker, OrderCreatedConsumer (BackgroundService)
tests/
  BusinessManagement.UnitTests/        logic thuần
  BusinessManagement.IntegrationTests/ PostgreSQL + RabbitMQ thật (Testcontainers), HTTP đầu cuối, composition
scripts/                               seed-dev.sql (dữ liệu thử cho DB dev)
.github/workflows/ci.yml · docker-compose.yml · .env.example · .gitattributes · Directory.Build.props · README.md
```

## 4. Tiến độ
| Ngày | Nội dung | Trạng thái |
|---|---|---|
| 1 | Skeleton, LINQ/async (1512 ms tuần tự so với 515 ms WhenAll) | ✅ |
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
| 14 | Message queue, RabbitMQ, publish `OrderCreated` | ✅ |
| 15 | Worker làm consumer, `ack`, DLQ | ✅ |
| 16 | Idempotent consumer, thử lại có độ trễ | ✅ |
| 17 | Transactional Outbox, relay, Kafka (khái niệm) | ✅ |
| 18 | Docker Compose đầy đủ, CI, migrate lúc khởi động | ✅ |
| 19 | README, test đầu cuối, dọn repo, chạy lại từ bản clone sạch | ✅ |

API hiện có:

| Nhóm | Endpoint | Quyền |
|---|---|---|
| Hệ thống | `GET /health`, tài liệu OpenAPI và Swagger | Công khai |
| Auth | `POST /api/auth/register`, `POST /api/auth/login` | Công khai |
| | `GET /api/auth/me` | Đăng nhập |
| Pricing | `POST /api/pricing/quote` | Đăng nhập |
| Products | `GET`, `POST`, `PUT /api/products`, `GET /api/products/{id}` | Đăng nhập |
| | `DELETE /api/products/{id}` | Admin |
| Customers | `GET`, `POST`, `PUT /api/customers`, `GET /api/customers/{id}`, `POST .../deactivate`, `POST .../activate` | Đăng nhập |
| Orders | `POST /api/orders` (có `voucherCode`), `GET /api/orders` (lọc, phân trang), `GET /api/orders/{id}`, `POST /api/orders/{id}/cancel` | Đăng nhập |
| Báo cáo | `GET /api/reports/top-customers` | Admin |

## 5. Quyết định kiến trúc và lý do

### Tổng thể
| Quyết định | Lý do |
|---|---|
| Layered modular monolith, 1 Api + 1 Worker | Đủ cho MVP, dễ giải thích; không tách microservices chỉ để phức tạp hơn |
| Rule bất biến của một entity nằm trong entity; điều phối nhiều entity hoặc DB nằm ở Application service | Tránh anemic model, dễ test |
| Mỗi layer có `DependencyInjection.cs`; `AddPersistence` và `AddMessaging` tách khỏi `AddInfrastructure` | Worker chỉ cần DB và RabbitMQ, không kéo JWT hay thanh toán |
| Lifetime: Singleton chỉ cho thứ không trạng thái và không phụ thuộc Scoped; còn lại Scoped | Tránh captive dependency; test composition bật `ValidateScopes` + `ValidateOnBuild` |
| Cấu hình thiếu hoặc yếu (connection string, `Jwt:Key` < 32 byte, RabbitMQ) thì app không khởi động | Fail-fast tốt hơn chạy rồi lỗi ngầm |

### Domain, giảm giá, thanh toán mẫu
| Quyết định | Lý do |
|---|---|
| `Order.AddItem` không trừ kho; `OrderItem.UnitPrice`, `Order.DiscountAmount`, `VoucherCode` chụp tại lúc đặt | Trừ kho cần transaction; đổi giá hoặc voucher sau đó không làm đổi đơn cũ |
| Tiền dùng `decimal`, DB `numeric(18,2)`, làm tròn `AwayFromZero` | Tránh sai số nhị phân và banker's rounding bất ngờ |
| Id dùng `Guid.CreateVersion7()`; ngừng khách bằng `IsActive`, không xoá cứng | Ít phân mảnh index; giữ lịch sử đơn |
| `IDiscountStrategy` (Priority nhỏ áp dụng trước) + `IVoucherFactory`; tổng giảm kẹp theo phần còn lại; voucher sai là `DomainException` | Tách "áp dụng cho ai" khỏi "giảm bao nhiêu"; không tính đủ tiền trong im lặng |
| Adapter cổng ngân hàng, Decorator ghi log, `RefundProcessor` (ISP): **có test nhưng chưa nối vào luồng đơn** | Mã mẫu cho Payments sau này; ghi rõ trong README |
| Observer trong process (Ngày 6) đã được RabbitMQ pub/sub thay thế | Bản trong process mất sự kiện khi process chết |
| Không dùng MediatR, CQRS, generic repository | Không có nhu cầu đo được; xem câu trả lời phỏng vấn trong mục 12 |

### Truy cập dữ liệu
| Quyết định | Lý do |
|---|---|
| `DbContext` trực tiếp qua `IAppDbContext`; không repository, không tự viết Unit of Work | EF Core đã là repository và unit of work; generic repository che `Include` và projection |
| `IAppDbContext` đặt ở Application | Đảo phụ thuộc, cho phép fake. Đánh đổi: Application tham chiếu EF Core |
| Ranh giới transaction ở Application service; ánh xạ bằng Fluent API trong Infrastructure | Domain không biết EF; làm được check constraint, `OnDelete`, index tổ hợp và có điều kiện |
| Đọc: `AsNoTracking` + projection; ghi: entity theo dõi + phương thức Domain | Đọc nhẹ; ghi qua quy tắc Domain |
| Test persistence bằng PostgreSQL thật (Testcontainers + `MigrateAsync`) | InMemory không thực thi FK, unique, check, transaction |

### API và xử lý lỗi
| Quyết định | Lý do |
|---|---|
| 400 sai hình thức; 404 không tồn tại; 409 xung đột dữ liệu; 422 vi phạm quy tắc Domain; 500 không lộ chi tiết, có `traceId` | Một định dạng lỗi nhất quán (ProblemDetails), không `try/catch` trong controller |
| `DbUpdateException` nhóm `23xxx` → 409, body không chứa tên constraint | `RESTRICT` là `23001`, unique là `23505`, FK là `23503` |
| Validation attribute của DTO record đặt ở **tham số constructor**; Guid bắt buộc dùng `[NotEmptyGuid]` | `[property:]` làm MVC ném lỗi; `[Required]` vô tác dụng với `Guid` (thiếu → `Guid.Empty`) |
| Action `GET` dùng `[FromQuery]` với lớp request riêng; `DateTimeOffset` → UTC | Kiểu phức hợp mặc định bind từ thân request |
| Phân trang offset, `pageSize` ≤ 100, sắp xếp whitelist, luôn có `ThenBy(Id)` | An toàn, không lặp hay bỏ sót dòng giữa các trang |
| `X-Request-Id` nhận từ client chỉ khi khớp `^[A-Za-z0-9_-]{8,64}$`, nếu không sinh mới; có trong log, header, body lỗi | Chống log injection; tra log theo một mã |
| Log dùng message template, chỉ ghi Id (không email, token, mật khẩu); JSON ở môi trường không phải Development | Trường lọc được; không rò dữ liệu cá nhân |

### Xác thực và phân quyền
| Quyết định | Lý do |
|---|---|
| PBKDF2 có muối (`PasswordHasher` của Identity); đăng nhập sai trả cùng thông báo và luôn chạy một lần băm | Lộ DB vẫn phải dò chậm; chống dò tài khoản và đo thời gian |
| JWT HS256, claim `sub`, `email`, `role`; kiểm tra chữ ký, issuer, audience, hạn 30 phút; `MapInboundClaims = false`, `RoleClaimType = "role"` | Thiếu `RoleClaimType` thì `[Authorize(Roles)]` luôn 403 |
| Fallback policy: mọi endpoint yêu cầu đăng nhập; `[AllowAnonymous]` đặt ở từng action | An toàn mặc định; cấp class ghi đè mọi `[Authorize]` |
| Đăng ký công khai luôn là Staff; nâng Admin bằng SQL | Chủ ý: chưa có quản lý người dùng |

### Đơn hàng và đồng thời
| Quyết định | Lý do |
|---|---|
| Tạo đơn giữ hàng ngay (Pending): `UPDATE ... SET Stock = Stock - @q WHERE Id = @id AND Stock >= @q`, kiểm tra số dòng bị ảnh hưởng, cả đơn trong một transaction | Kiểm tra và trừ là một câu lệnh nguyên tử; đọc rồi ghi trong ứng dụng mất cập nhật |
| Các dòng sắp theo `ProductId` trước khi khoá; hết hàng → 409; khách/sản phẩm không có → 422 | Khoá cùng thứ tự tránh deadlock |
| Huỷ đơn: cổng nguyên tử `UPDATE Orders SET Status = 'Cancelled' WHERE Status <> 'Cancelled'`, rồi hoàn kho | Hai request huỷ cùng lúc chỉ hoàn kho một lần |
| Ba lớp bảo vệ tồn kho: SQL có điều kiện, `Product.DecreaseStock`, `CHECK Stock >= 0`. Đã cân nhắc optimistic và `SELECT FOR UPDATE` | Chỉ dùng khi luật phức tạp không viết được thành một `UPDATE` |
| Voucher kiểm tra trước transaction; danh sách đơn đúng 2 truy vấn (đếm + trang) | Transaction giữ khoá ngắn hơn; khoá bằng test đếm truy vấn |

### Messaging (Ngày 14–17)
| Quyết định | Lý do |
|---|---|
| **Transactional Outbox:** `OrderService` ghi `OutboxMessage` cùng transaction với đơn; `EventId` = `OutboxMessage.Id` | Ghi DB và broker không atomic; commit xong publish lỗi thì mất sự kiện |
| Relay (Worker): `SELECT ... FOR UPDATE SKIP LOCKED` theo lô 20, timeout 5 giây mỗi tin, lùi dần 2^n giây (tối đa 300), dừng lô khi lỗi; cờ `Outbox:Enabled` | Nhiều relay chia việc không gửi trùng; test consumer tắt relay |
| Api không còn phụ thuộc broker; `OutboxRelayService` chỉ đăng ký trong Worker | Broker chậm hay tắt không làm đơn chậm hay lỗi |
| Exchange `topic` bền vững; tin persistent + publisher confirm + `mandatory`; DLX khai báo từ đầu | Tin sai routing key không biến mất im lặng; tham số queue không đổi được sau khi tạo |
| Consumer: `autoAck=false`, `ack` sau khi xử lý, prefetch 10, một scope DI mỗi tin; JSON hỏng hoặc `DomainException` vào DLQ ngay; tắt êm không `ack` tin đang xử lý | Tin chỉ bị xoá khi xử lý xong; tin độc không lặp vô hạn; `BackgroundService` là Singleton, `DbContext` là Scoped |
| **Idempotent consumer:** `ProcessedMessages` unique `(Consumer, EventId)`, ghi cùng transaction với thông báo; phân biệt trùng và lỗi DB bằng kiểm tra lại sự tồn tại | Giao ít nhất một lần + idempotent = hiệu ứng như đúng một lần; không dùng Redis vì dấu phải cùng transaction với hiệu ứng |
| Thử lại: queue chờ TTL cố định (DLX về exchange chính), đếm lần bằng header `x-attempt`, tối đa `MaxAttempts` (3) rồi DLQ; ack bản gốc sau khi bản thử lại được broker xác nhận | `requeue: true` lặp tức thì; TTL từng tin chỉ hết hạn khi ở đầu hàng |
| RabbitMQ thay vì Kafka | Hàng đợi công việc cần ack, thử lại, DLQ sẵn, một bên tiêu thụ. Kafka hợp khi nhiều bên đọc độc lập và cần phát lại; Outbox và idempotency vẫn cần y nguyên |

### Triển khai (Ngày 18–19)
| Quyết định | Lý do |
|---|---|
| Dockerfile nhiều giai đoạn (sdk → aspnet), chạy không phải root; Worker dùng image aspnet | Image cuối nhỏ, không có SDK |
| Cấu hình hoàn toàn qua biến môi trường (`A__B`); bí mật chỉ nằm trong `.env` (không commit), không `ENV`/`ARG` bí mật trong Dockerfile | Lớp image đọc được bằng `docker history`; cùng một image chạy mọi môi trường |
| Compose: các dịch vụ gọi nhau bằng tên (`db:5432`, `rabbitmq`); Worker chờ Api khoẻ | Api migrate trước khi `/health` trả lời nên Worker không gặp bảng thiếu |
| `Database:MigrateOnStartup` mặc định `false`, Compose bật | Tiện cho demo; production dùng job riêng |
| CI: restore, build, test trên `ubuntu-latest` với `--blame-hang-timeout`, kèm job build Dockerfile | Test treo thành lỗi có tên; CI là người kiểm tra trung thực về phụ thuộc ngầm vào máy dev |
| Gom 10 migration thành một `InitialCreate` trước khi công khai repo | Chưa có DB dùng chung; lịch sử có thêm rồi hoàn tác và hai `defaultValue: false` có thể để lại DEFAULT trong DB |

## 6. Database schema và migrations
| Bảng | Ràng buộc |
|---|---|
| Customers | PK; UNIQUE(Email) |
| Products | PK; CHECK Price >= 0; CHECK Stock >= 0; index Category, Name |
| Orders | PK; FK CustomerId (RESTRICT); DiscountAmount CHECK >= 0; VoucherCode; index (CustomerId, CreatedAt); index CreatedAt |
| OrderItems | PK; FK OrderId (CASCADE, NOT NULL); FK ProductId (RESTRICT); UNIQUE(OrderId, ProductId); CHECK Quantity > 0; CHECK UnitPrice >= 0 |
| Users | PK; UNIQUE(Email); Role lưu dạng chuỗi |
| Notifications | PK; index OrderId (không unique, chủ ý) |
| ProcessedMessages | PK; UNIQUE(Consumer, EventId) |
| OutboxMessages | PK; index CreatedAt có điều kiện `WHERE ProcessedAt IS NULL` |

Migration: một `InitialCreate`. Kiểu cột: `uuid`, `numeric(18,2)`, `timestamptz` (luôn UTC; PostgreSQL cắt ở micro-giây), enum lưu dạng chuỗi.

### Topology RabbitMQ
| Thành phần | Cấu hình |
|---|---|
| Exchange `bm.events` | `topic`, durable |
| Queue `bm.orders.created` | durable; DLX `bm.events.dlx`, routing key `order.created`; binding `order.created` |
| Exchange `bm.events.dlx` / queue `bm.orders.created.dlq` | `direct`, durable; DLQ binding `order.created` |
| Exchange `bm.events.retry` / queue `bm.orders.created.retry` | `x-message-ttl` = `RetryDelayMs`, DLX về `bm.events` với `order.created` |

### Thí nghiệm index (Ngày 10, 100k đơn, 300k dòng hàng)
| Thí nghiệm | Kết quả |
|---|---|
| Đơn của một khách, mới nhất trước | `Index Scan Backward` trên `(CustomerId, CreatedAt)`: 0,12 ms, không `Sort` |
| Đơn 7 ngày gần đây | `Seq Scan` 24,3 ms → `Index Only Scan` 0,48 ms sau khi thêm index `CreatedAt` (~50 lần) |
| `Status = 'Confirmed'` (100% dòng) | `Seq Scan` dù có index: chọn lọc thấp không giúp ích |
| `lower(Name) LIKE '%x%'` | `Seq Scan`, ước lượng 1 so với thực tế 111; B-tree không dùng được cho wildcard đầu; hướng xử lý: `pg_trgm` |
| Báo cáo top khách | 441 ms; `COUNT(DISTINCT)` gây `Sort` ra đĩa 15 MB; chấp nhận được ở quy mô này |

## 7. Cách chạy
Toàn bộ bằng Docker (xem README):
```
cp .env.example .env         # sửa POSTGRES_PASSWORD, RABBITMQ_PASSWORD (chữ và số), JWT_KEY (>= 32 ký tự)
docker compose up -d --build # Api http://localhost:8080, RabbitMQ UI http://localhost:15672
docker compose down -v       # xoá cả dữ liệu
```
Phát triển không dùng container cho Api: `docker compose up -d db rabbitmq`, đặt user-secrets (`ConnectionStrings:Default`, `Jwt:Key`, `Payments:Bank:MerchantAccount`, `RabbitMq:Password`), `dotnet tool restore`, `dotnet ef database update`, rồi `dotnet run` cho Api và Worker (dùng chung `UserSecretsId`).
- Nâng Admin: `UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = '<email>';` rồi đăng nhập lại (token cũ giữ vai trò cũ).
- `RABBITMQ_DEFAULT_USER/PASS` và mật khẩu PostgreSQL chỉ có tác dụng khi volume còn trống: đổi mật khẩu phải đổi trong chính dịch vụ hoặc xoá volume.

## 8. Cách chạy test
```
dotnet test       # cần Docker đang chạy (PostgreSQL và RabbitMQ thật); lần đầu kéo image
dotnet test --filter "FullyQualifiedName~OrderServiceTests"
dotnet test --blame-hang-timeout 3m     # nếu nghi test treo
```

## 9. Nợ kỹ thuật (cũng nằm trong README, mục Known limitations)

**Nghiệp vụ và độ tin cậy**
- Chưa có Payments và `Confirm`; đơn Pending giữ hàng mãi (thiếu job huỷ quá hạn)
- Chưa có Idempotency-Key cho `POST /api/orders`
- Ai đăng nhập cũng xem và huỷ được đơn bất kỳ (đơn chưa gắn với người tạo)
- `OutboxMessages` và `ProcessedMessages` tăng mãi (cần job dọn); dòng outbox chưa giới hạn số lần thử, chưa có cảnh báo tồn đọng; relay giữ transaction khi gửi lô
- Độ trễ thử lại cố định (chưa backoff luỹ tiến, jitter); chưa có công cụ phát lại tin từ DLQ
- Gửi email thật sẽ không nằm trong transaction được: cần khoá chống trùng của nhà cung cấp
- Giá sản phẩm đọc trước transaction; `Order.Cancel()` ở Domain không được `OrderService` dùng (huỷ bằng SQL)
- Chưa có `Customer.Segment` (đang dùng "Standard"); sản phẩm giá 0 + voucher ra 422 khó hiểu; `OrderTotal` của `/api/pricing/quote` chấp nhận hơn 2 chữ số thập phân
- `IVoucherFactory` Singleton: đổi sang Scoped khi đọc DB
- `PUT` là "ghi đè cuối cùng thắng"; sắp xếp theo tên phụ thuộc collation; `ToLower().Contains` không dùng index
- Worker dispatch tuần tự; queue vẫn là classic (cân nhắc quorum)

**Bảo mật**
- JWT stateless: không thu hồi được trước hạn; chưa có rate limiting, đổi mật khẩu, quản lý người dùng
- 401 và 403 trả thân rỗng; đăng ký trả 409 lộ email đã tồn tại (chấp nhận với công cụ nội bộ)

**Triển khai**
- Migrate lúc khởi động không an toàn khi nhiều instance Api: dùng job riêng
- Dockerfile `COPY . .` nên restore lại từ đầu mỗi lần; `UseHttpsRedirection` còn trong container chỉ có HTTP; cổng DB và RabbitMQ mở ra máy (chỉ phù hợp demo)

**Cần xác nhận**
- Đường dẫn Swagger khớp với README; Swagger hiện trong bản chạy bằng Compose
- `.env.example`: `POSTGRES_USER=app`
- `docs/postman/` (collection Postman); repo đã công khai, có mô tả và topic

## 11. Module tuỳ chọn (khi có JD đòi hỏi)
Pub/Sub thật (queue thứ hai ghi `AuditLog`), Chain of Responsibility (tách `GlobalExceptionHandler`), Redis (Idempotency-Key, giới hạn tốc độ đăng nhập), API tích hợp Dynamics bằng `ServiceClient` (cần biết CRM online hay on-premises), giao diện Next.js nhỏ, demo Kafka.
