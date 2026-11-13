# PROJECT STATUS

## 1. Mục tiêu và phạm vi MVP
Business Management API (ASP.NET Core): Auth, Products, Customers, Orders, Payments, RabbitMQ worker.

| Bắt buộc | Làm sau | Chưa cần |
|---|---|---|
| Auth (JWT, role), CRUD Products/Customers, Orders + trừ kho trong transaction, Payments (bản ghi nghiệp vụ), validation, logging, unit + integration test, Docker Compose, RabbitMQ (retry, DLQ, idempotent consumer) | Permission chi tiết, Transactional Outbox, Idempotency-Key, Kafka demo | Microservices, CQRS/MediatR, thanh toán thật, frontend |

## 2. Công nghệ và phiên bản
- .NET SDK 10.0.300 / runtime 10.0.14 (LTS, hỗ trợ đến 11/2028)
- ASP.NET Core Web API (controllers); `Microsoft.OpenApi` ghim **2.7.5**
- EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3
- PostgreSQL 18 (Docker Compose, cổng 5434)
- xUnit 2.9.3, Testcontainers (PostgreSql)
- Chưa có: JWT, RabbitMQ, Worker

## 3. Cấu trúc thư mục (đối chiếu với thực tế)
```
src/
  BusinessManagement.Domain/          Common, Entities
  BusinessManagement.Application/     Abstractions (IAppDbContext), Checkout/{Discount,Events,Payments,Ports,Pricing}, DependencyInjection.cs
  BusinessManagement.Infrastructure/  Persistence/{Configurations,Migrations}, cài đặt thanh toán/thông báo/log đơn, DependencyInjection.cs
  BusinessManagement.Api/             Controllers, Contracts/{Requests,Responses}, ExceptionHandling, Program.cs
tests/
  BusinessManagement.UnitTests/        Domain + Application
  BusinessManagement.IntegrationTests/ CompositionRoot, Api (WebApplicationFactory), Persistence (Testcontainers)
learning/                              Code học (Day01, Day04, Learning.Tests), ngoài solution sản phẩm
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
| 7 | Pricing API, exception handler, smoke test | ✅ (còn nợ dọn dẹp, mục 9) |
| 8 | EF Core, PostgreSQL, migrations, test persistence | ✅ 113 test xanh |

API đã có: `GET /health`, `POST /api/pricing/quote`. `OrderController` là tạm thời.

## 5. Quyết định kiến trúc và lý do

### Tổng thể
| Quyết định | Lý do |
|---|---|
| Layered modular monolith, 1 Api + 1 Worker | Đủ cho MVP, dễ giải thích; không tách microservices chỉ để phức tạp hơn |
| Rule bất biến của một entity nằm trong entity; điều phối nhiều entity hoặc DB nằm ở Application service | Tránh anemic model, dễ test |
| Mỗi layer có `DependencyInjection.cs` riêng; `IPaymentMethod` mỗi khoá đúng một implementation | Composition root gọn; trùng khoá làm `ToDictionary` ném lỗi |
| Lifetime: Singleton chỉ cho thứ không trạng thái và không phụ thuộc Scoped; còn lại Scoped | Tránh captive dependency. `IVoucherFactory` phải đổi sang Scoped khi đọc DB |
| Bật `ValidateScopes` + `ValidateOnBuild` trong test composition | Bắt lỗi lifetime ngoài môi trường Development |

### Domain
| Quyết định | Lý do |
|---|---|
| `Order.AddItem` không trừ kho | Trừ kho cần transaction và DB; làm ở service (Ngày 12) |
| Huỷ đơn đã `Confirmed` phải hoàn tồn kho | Ghi chú cho Ngày 12 |
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
| Một voucher mỗi đơn; mã voucher sai: `DomainException` trước khi thanh toán | Không để khách bị tính đủ tiền trong im lặng |
| Factory chỉ cần khi mã voucher biết lúc chạy | Bảng cố định thì đăng ký từng strategy trong DI |
| Hoàn tiền là use case riêng (`RefundProcessor`), chỉ nhận `IRefundablePayment` | ISP: Cash không bị ép có `Refund` |
| Adapter cổng ngân hàng: số âm → `DomainException` trước khi gọi cổng; số tiền làm tròn về 0 → coi là thành công, không gọi cổng; mã lỗi ≠ 0 → `PaymentException` có `ErrorCode` | Quyết định Ngày 5 |
| Observer trong process: handler lỗi ghi log Error, không ném lại; `Process` vẫn trả `finalAmount` | Tiền đã thu thì không báo đơn thất bại |
| Không dùng MediatR | Danh sách handler qua DI đủ cho nhu cầu hiện tại |

### Truy cập dữ liệu (Ngày 6, 8)
| Quyết định | Lý do |
|---|---|
| `DbContext` trực tiếp qua `IAppDbContext`, không generic repository | EF Core đã là Unit of Work và repository; generic repository che `Include` và projection. Xem lại nếu có truy vấn phức tạp dùng lại nhiều nơi |
| `IAppDbContext` đặt ở Application | Đảo phụ thuộc (Application không tham chiếu Infrastructure) và cho phép fake. Đánh đổi: Application tham chiếu EF Core |
| Truy vấn dùng lại nằm ở Application (extension trên `IQueryable`), không ở Domain | Domain không được biết EF |
| Ranh giới transaction ở Application service: một use case một `SaveChangesAsync` | Các tầng khác không cần biết |
| Test persistence bằng PostgreSQL thật (Testcontainers + `MigrateAsync`) | InMemory không thực thi FK, unique, check, transaction |
| Ánh xạ bằng Fluent API trong Infrastructure | Domain không tham chiếu EF, và Fluent API làm được thứ attribute không làm được (check constraint, `OnDelete`, index tổ hợp) |
| `AppDbContext` Scoped; `IAppDbContext` trỏ cùng instance qua `AddScoped(sp => AppDbContext)` | `AddScoped<IAppDbContext, AppDbContext>()` sẽ tạo context thứ hai trong cùng request |

### API (Ngày 7)
| Quyết định | Lý do |
|---|---|
| `POST /api/pricing/quote` trả 200 | Chỉ tính, không tạo tài nguyên |
| Lỗi hình thức → 400 (DTO); lỗi nghiệp vụ → 422 (`DomainException`); lỗi lạ → 500 không lộ chi tiết, có `traceId` | Một định dạng lỗi nhất quán, không `try/catch` trong controller |
| `UseExceptionHandler` đứng trước các middleware nó cần bảo vệ | Handler chỉ bắt lỗi từ phía sau nó |
| Attribute validation của DTO record đặt ở **tham số constructor**, không dùng `[property: ...]` | MVC ném `InvalidOperationException` với `property:` |
| Số tài khoản thật và connection string nằm trong user-secrets; `appsettings.Development.json` chỉ có placeholder | Không đưa secret vào Git |
- Đọc: AsNoTracking + projection; ghi: entity được theo dõi + phương thức Domain; một use case một SaveChangesAsync
- Phân trang offset, pageSize ≤ 100, sắp xếp whitelist, luôn có ThenBy(Id)
- PUT không sửa tồn kho; ngừng/kích hoạt khách bằng POST .../deactivate và .../activate (không có DELETE)
- Email trùng: AnyAsync (thân thiện) + unique index (bảo đảm, chặn cả race condition)
- 400 = sai hình thức (DTO); 404 = tài nguyên không có (cũng có 404 do routing); 409 = xung đột dữ liệu (kể cả DbUpdateException 23xxx); 422 = vi phạm quy tắc Domain

## 6. Database schema và migrations
| Bảng | Ràng buộc |
|---|---|
| Customers | PK; UNIQUE(Email) |
| Products | PK; CHECK Price >= 0; CHECK Stock >= 0; index Category, Name |
| Orders | PK; FK CustomerId → Customers (RESTRICT); index (CustomerId, CreatedAt) |
| OrderItems | PK; FK OrderId → Orders (CASCADE, NOT NULL); FK ProductId → Products (RESTRICT); UNIQUE(OrderId, ProductId); CHECK Quantity > 0; CHECK UnitPrice >= 0 |

Migrations: `InitialCreate`, `RequireOrderItemOrderId` (đã áp dụng, DB không còn DEFAULT).
Kiểu cột: `uuid`, `numeric(18,2)`, `timestamp with time zone` (luôn UTC), enum lưu dạng chuỗi.

## 7. Cách chạy
```
docker compose up -d                 # cần file .env (POSTGRES_USER, POSTGRES_PASSWORD), không commit
dotnet user-secrets set "ConnectionStrings:Default" "<chuỗi kết nối>" --project src/BusinessManagement.Api
dotnet user-secrets set "Payments:Bank:MerchantAccount" "<số giả>" --project src/BusinessManagement.Api
dotnet ef database update --project src/BusinessManagement.Infrastructure --startup-project src/BusinessManagement.Api
dotnet run --project src/BusinessManagement.Api
```

## 8. Cách chạy test
```
dotnet test                                                   # cần Docker Desktop đang chạy (test persistence)
dotnet test --filter "FullyQualifiedName~ProductTests"
```

## 9. Lỗi đã biết / nợ kỹ thuật

**Nghiệp vụ và độ tin cậy**
- Thanh toán → ghi log → thông báo chưa atomic: mất sự kiện nếu process chết sau `Pay` (Ngày 12, 14–17)
- Observer trong process: handler lỗi thì mất bản ghi (Outbox, Ngày 17)
- Tra voucher nằm ở cả `OrderProcessor` và `PricingService` → Ngày 12 `OrderService` dùng lại `PricingService`
- `Cancel()` chưa hoàn tồn kho (Ngày 12)
- `IVoucherFactory` Singleton: đổi sang Scoped khi đọc DB
- `OrderTotal` có hơn 2 chữ số thập phân vẫn được chấp nhận
- `LegacyBankPaymentAdapter`: kiểm tra xem kiểm tra số 0 đã đặt sau bước làm tròn chưa
- `FileOrderLog` ghi đồng thời có thể ném `IOException` (tạm thời)
- `traceId` của 400 (W3C) khác định dạng của 422 (Kestrel)

**Nợ dọn dẹp**
- `OrderController` và `OrderProcessor` (Console/File) là tạm thời, xoá ở Ngày 12
- `Directory.Build.props`, README, xoá `WeatherForecast*` / `.http` / `Class1.cs`, xoá file `log` ở gốc
- Đổi tên namespace `Infrastructure.PaymentMethod` / `.Notify` / `.OrderLog` theo vai trò
- Kiểm tra `Customer.UpdateContact` không còn tham số mặc định gây vô hiệu hoá khách

## 10. Phần còn thiếu
CRUD Products/Customers, Auth (JWT, role), Orders và tồn kho, Payments, RabbitMQ + Worker, Docker Compose đầy đủ, README, CI.

## Nợ kỹ thuật (thêm)
- Chưa có [Authorize] trên các endpoint CRUD (Ngày 11)
- PUT là "ghi đè cuối cùng thắng"; kiểm soát đồng thời xử lý ở Ngày 12
- Sắp xếp theo tên phụ thuộc collation PostgreSQL (tiếng Việt có dấu có thể sắp sai)
- Tìm kiếm ToLower().Contains(...) không dùng được index; cân nhắc trigram nếu bảng lớn

## 11. Công việc tiếp theo
Ngày 9: CRUD Products và Customers (DTO, validation, phân trang, lọc, sắp xếp).

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
- Xử lý lỗ hổng trong transitive dependency (GHSA-v5pm-xwqc-g5wc)

## Bài học đã rút (câu chuyện phỏng vấn)
- `Assert.Throws` đúng với mọi exception cùng loại: phải khoá lý do và kiểm tra hậu quả
- `ValidateOnBuild` một mình không bắt được captive dependency; cần bật cả `ValidateScopes`
- `ON DELETE RESTRICT` được PostgreSQL báo bằng `23001`, không phải `23503`; khi map lỗi DB sang HTTP, coi nhóm `23xxx` là xung đột dữ liệu
- Shadow FK mặc định cho phép NULL; phải `IsRequired()`. Migration đã áp dụng và chia sẻ thì thêm migration mới, không sửa cũ
- Volume Docker giữ thông tin đăng nhập của lần khởi tạo đầu; đổi mật khẩu hoặc cổng phải `docker compose down -v`
- AlterColumn từ nullable sang NOT NULL khiến EF sinh defaultValue, và Npgsql tạo DEFAULT thật trong DB. Xoá dòng đó khỏi file sau khi đã áp dụng không gỡ được DEFAULT, vì EF chỉ DROP DEFAULT khi biết default cũ. Phải xoá defaultValue trước khi áp dụng migration lần đầu, hoặc chạy ALTER ... DROP DEFAULT bằng tay (hoặc trong một migration mới nếu DB đã chia sẻ).
- LINQ trả về truy vấn mới: quên gán lại thì bộ lọc im lặng biến mất; test có TotalCount trên dữ liệu có sẵn mới bắt được
- Test dùng dữ liệu riêng (Guid trong email, nhóm riêng) vì cả collection dùng chung một DB

## Quyết định (Ngày 9)
- Đọc: AsNoTracking + projection; ghi: entity được theo dõi + phương thức Domain; một use case một SaveChangesAsync
- Phân trang offset, pageSize ≤ 100, sắp xếp whitelist, luôn có ThenBy(Id)
- PUT không sửa tồn kho; ngừng/kích hoạt khách bằng POST .../deactivate và .../activate (không có DELETE)
- Email trùng: AnyAsync (thân thiện) + unique index (bảo đảm, chặn cả race condition)
- 400 = sai hình thức (DTO); 404 = tài nguyên không có (cũng có 404 do routing); 409 = xung đột dữ liệu (kể cả DbUpdateException 23xxx); 422 = vi phạm quy tắc Domain

## Bài học Ngày 9
- LINQ trả về truy vấn mới: quên gán lại thì bộ lọc im lặng biến mất; test có TotalCount trên dữ liệu có sẵn mới bắt được
- Test dùng dữ liệu riêng (Guid trong email, nhóm riêng) vì cả collection dùng chung một DB

## Nợ kỹ thuật (thêm)
- Chưa có [Authorize] trên các endpoint CRUD (Ngày 11)
- PUT là "ghi đè cuối cùng thắng"; kiểm soát đồng thời xử lý ở Ngày 12
- Sắp xếp theo tên phụ thuộc collation PostgreSQL (tiếng Việt có dấu có thể sắp sai)
- Tìm kiếm ToLower().Contains(...) không dùng được index; cân nhắc trigram nếu bảng lớn

## Ngày 10: SQL, index, EXPLAIN, N+1
| Thí nghiệm | Kết quả |
|---|---|
| Đơn của một khách, mới nhất trước | Index Scan Backward trên (CustomerId, CreatedAt): 0,12 ms, không Sort |
| Đơn 7 ngày gần đây | Seq Scan 24,3 ms → Index Only Scan 0,48 ms sau khi thêm index CreatedAt (~50x) |
| Status = 'Confirmed' (100% dòng) | Seq Scan dù có index: index chọn lọc thấp không giúp ích |
| lower(Name) LIKE '%x%' | Seq Scan, ước lượng 1 so với thực tế 111; B-tree không dùng được cho wildcard đầu; hướng xử lý: pg_trgm |
| Báo cáo top khách (300k dòng hàng) | 441 ms; COUNT(DISTINCT) gây Sort external merge ra đĩa 15 MB; chấp nhận được ở quy mô này |

- IX_Orders_CreatedAt: <giữ/xoá và lý do>
- N+1: ReportService luôn đúng 2 truy vấn (khoá bằng test đếm truy vấn bằng DbCommandInterceptor)
- Nợ: báo cáo cần [Authorize(AdminOnly)] (Ngày 11); xem lại nếu báo cáo được gọi dày

## Quyết định (Ngày 11)
- Mật khẩu: PasswordHasher của ASP.NET Core Identity (PBKDF2, muối ngẫu nhiên); chỉ lưu hash
- JWT HS256: claim sub, email, role; kiểm tra chữ ký, issuer, audience, hạn (30 phút), ClockSkew 30 giây; MapInboundClaims = false và RoleClaimType = "role"
- Fallback policy: mọi endpoint yêu cầu đăng nhập; công khai chỉ /health, /openapi, register, login
- Đăng ký công khai luôn là Staff; nâng Admin bằng SQL (chủ ý, chưa có quản lý người dùng)
- Đăng nhập sai: cùng thông báo cho email sai và mật khẩu sai, luôn chạy một lần băm (chống dò tài khoản và timing)
- JwtOptions là nguồn duy nhất cho cả tạo và kiểm tra token; thiếu hoặc khoá < 32 byte thì app không khởi động
- [AllowAnonymous] đặt ở từng action, không đặt ở cấp class

## Nợ kỹ thuật (thêm)
- JWT stateless: user bị khoá hoặc đổi vai trò vẫn dùng được token cũ đến khi hết hạn; cần refresh token và thu hồi
- Chưa có rate limiting và khoá tài khoản khi đăng nhập sai nhiều lần
- 401 và 403 trả thân rỗng (chưa là ProblemDetails)
- Đăng ký trả 409 khi email trùng: lộ email nào đã đăng ký (chấp nhận với công cụ nội bộ)
- Chưa có quản lý người dùng và đổi mật khẩu

## Quyết định (Ngày 12)
- Tạo đơn giữ hàng ngay (Pending): trừ tồn bằng UPDATE có điều kiện (Stock >= qty) + kiểm tra số dòng; một transaction cho cả đơn
- Hết hàng: ConflictException (409); khách/sản phẩm không có trong body: DomainException (422)
- Các dòng sắp theo ProductId để mọi giao dịch khoá cùng thứ tự (tránh deadlock)
- Huỷ đơn: cổng nguyên tử UPDATE Status WHERE Status <> 'Cancelled' (chống hoàn kho hai lần)
- Quy tắc tồn kho nằm ở SQL cho đường đồng thời; Product.DecreaseStock còn cho luồng đơn lẻ; CHECK Stock >= 0 là chốt chặn cuối
- Lựa chọn khác đã cân nhắc: optimistic (xmin + thử lại), pessimistic (SELECT ... FOR UPDATE)

## Nợ kỹ thuật (thêm)
- Giảm giá/voucher chưa tích hợp vào đơn; Order chưa lưu số tiền giảm → Ngày 13 (PricingService)
- Chưa có thanh toán và Confirm; đơn Pending giữ hàng mãi nếu bị bỏ rơi (cần job huỷ đơn quá hạn)
- Chưa có Idempotency-Key: gửi lại POST tạo thêm một đơn
- OrderEventPublisher và handler chưa được ai dùng → Ngày 14 (OrderCreated)
- Chưa có danh sách đơn và lọc theo khách