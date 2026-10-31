# PROJECT STATUS

> Cập nhật lần cuối: Ngày 2

## 1. Mục tiêu và phạm vi MVP
Business Management API (ASP.NET Core): Auth, Products, Customers, Orders, Payments, RabbitMQ worker.

| Bắt buộc | Làm sau | Chưa cần |
|---|---|---|
| Auth (JWT, role), CRUD Products/Customers, Orders + trừ kho trong transaction, Payments (bản ghi nghiệp vụ), validation, logging, unit + integration test, Docker Compose, RabbitMQ (retry, DLQ, idempotent consumer) | Permission chi tiết, Transactional Outbox, Idempotency-Key, Kafka demo, health checks | Microservices, CQRS/MediatR, thanh toán thật, frontend |

## 2. Công nghệ và phiên bản
- .NET SDK 10.0.300 / runtime 10.0.8 (LTS, hỗ trợ đến 11/2028)
- ASP.NET Core Web API (controllers), EF Core 10 *(chưa cài)*, PostgreSQL *(chưa cài)*
- xUnit, Docker Compose *(chưa)*, RabbitMQ *(chưa)*
- Microsoft.OpenApi **2.7.5** (ghim thủ công, xem mục 5)

## 3. Cấu trúc thư mục
```
src/   Api, Application, Domain, Infrastructure   (Worker: Ngày 15)
tests/ UnitTests, IntegrationTests (chưa có test)
playground/Day01   (bài tập, không thuộc sản phẩm)
```

## 4. Tiến độ
| Ngày | Nội dung | Trạng thái |
|---|---|---|
| 1 | Skeleton, LINQ/async | ✅ (1512ms tuần tự so với 515ms WhenAll) |
| 1 | Nộp hồ sơ, gửi CV | [ ] chưa xong |
| 2 | Domain entities + 21 unit test | ✅ xanh 21/21 |
| 2 | Thử nghiệm đột biến (4 test đỏ khi cố ý phá code) | ✅ |

Module hoàn thành: Domain (Product, Customer, Order, OrderItem).
API đã triển khai: chưa có.

## 5. Quyết định kiến trúc và lý do
| Quyết định | Lý do |
|---|---|
| Layered modular monolith, 1 Api + 1 Worker | Đủ cho MVP, dễ giải thích; không tách microservices chỉ để phức tạp hơn |
| Dùng DbContext trực tiếp, không generic repository | EF Core đã là Unit of Work; chốt lại ở Ngày 6 |
| Rule bất biến nằm trong entity; điều phối nhiều entity hoặc DB nằm ở Application service | Tránh anemic model, dễ test |
| `Order.AddItem` **không** trừ kho | Trừ kho cần transaction và DB; làm ở service (Ngày 12) |
| Huỷ đơn đã `Confirmed` phải hoàn tồn kho | Ghi chú cho Ngày 12 |
| `OrderItem.UnitPrice` sao chép giá tại thời điểm thêm | Giá sản phẩm đổi sau đó không ảnh hưởng đơn cũ |
| Tiền dùng `decimal`; DB `precision(18,2)` | Tránh sai số làm tròn (cấu hình ở Ngày 8) |
| Id dùng `Guid.CreateVersion7()` | Có thứ tự theo thời gian, ít phân mảnh index |
| Khách hàng ngừng hoạt động qua `IsActive`, không xoá cứng | Giữ lịch sử đơn hàng (khoá ngoại) |
| Email chuẩn hoá chữ thường; unique index ở DB | Domain kiểm tra cơ bản, DB là chốt chặn cuối |
| Ghim `Microsoft.OpenApi` 2.7.5 | `Microsoft.AspNetCore.OpenApi` 10.0.x kéo bản 2.0.0 dính GHSA-v5pm-xwqc-g5wc; giữ nhánh 2.x để khỏi vỡ tương thích |

## 6. Database schema và migrations
Chưa có (bắt đầu Ngày 8).

## 7. Cách chạy
```
dotnet build
dotnet run --project src/BusinessManagement.Api
```

## 8. Cách chạy test
```
dotnet test
dotnet test --filter "FullyQualifiedName~ProductTests"
```

## 9. Lỗi đã biết / cần rà soát
- `Customer.UpdateContact`: cần xác minh không còn tham số mặc định làm khách bị vô hiệu hoá âm thầm
- `Order.CustomerId` / `Order.Customer`: kiểm tra đã đổi sang `private set`
- `Order.AddItem/Confirm/Cancel`: kiểm tra đã gọi `Touch()`

## 10. Phần còn thiếu
API, DB, auth, RabbitMQ, Docker Compose, integration test, README.

## 11. Công việc tiếp theo
Ngày 3: SOLID và Dependency Injection.

## 12. Câu hỏi phỏng vấn đã học
- IEnumerable vs IQueryable
- Vì sao tránh `async void`; `.Result` gây deadlock thế nào
- Interface vs abstract class
- Composition over inheritance
- Xử lý lỗ hổng trong transitive dependency (ví dụ GHSA-v5pm-xwqc-g5wc)

| 3 | SOLID + DI, refactor OrderProcessor, lifetimes, 43 test | ✅ |

## Quyết định (Ngày 3)
- Abstraction ở Application; implementation tạm ở playground; ghép ở composition root
- Loại khách / phương thức thanh toán không hợp lệ: ném DomainException, chưa thanh toán
- Làm tròn tiền: Math.Round(..., 2, MidpointRounding.AwayFromZero)
- Hoàn tiền là use case riêng (RefundProcessor), không nằm trong OrderProcessor
- BankPayment (Singleton) phải stateless / thread-safe
- Bật cả ValidateScopes và ValidateOnBuild để bắt captive dependency khi khởi động

## Lỗi đã biết
- Thanh toán → ghi log → thông báo chưa atomic: Record lỗi sau Pay thì tiền đã thu mà không có bản ghi (Ngày 12, 17)
- IDiscountPolicy vừa chọn đối tượng (CustomerType) vừa tính tiền: xem lại ở Ngày 5 (Strategy)