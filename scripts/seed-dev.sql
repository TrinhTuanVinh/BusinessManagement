INSERT INTO "Customers" ("Id","FullName","Email","Phone","Address","IsActive","CreatedAt")
SELECT md5('c' || g)::uuid, 'Customer ' || g, 'seed' || g || '@test.com', NULL, NULL,
       (g % 20 <> 0), NOW() - (g || ' minutes')::interval
FROM generate_series(1, 10000) g;

INSERT INTO "Products" ("Id","Name","Category","Price","Stock","CreatedAt")
SELECT md5('p' || g)::uuid, 'Product ' || g, 'Cat' || (g % 20),
       (5 + (g % 200))::numeric(18,2), 100 + (g % 50), NOW()
FROM generate_series(1, 2000) g;

100.000 đơn: khách 1..9000 mỗi người 10 đơn; 100 khách đầu có thêm 100 đơn; khách 9001..10000 không có đơn
INSERT INTO "Orders" ("Id","CustomerId","Status","VoucherCode","CreatedAt")
SELECT md5('o' || g)::uuid,
       md5('c' || CASE WHEN g <= 90000 THEN (g % 9000) + 1 ELSE (g % 100) + 1 END)::uuid,
       'Confirmed', NULL,
       NOW() - ((g % 365) || ' days')::interval - ((g % 24) || ' hours')::interval
FROM generate_series(1, 100000) g;

3 dòng hàng mỗi đơn, 3 sản phẩm khác nhau
INSERT INTO "OrderItems" ("Id","OrderId","ProductId","Quantity","UnitPrice","CreatedAt")
SELECT md5('i' || g || '-' || k)::uuid, md5('o' || g)::uuid, p."Id", (1 + (g % 5))::int, p."Price", NOW()
FROM generate_series(1, 100000) g
CROSS JOIN generate_series(0, 2) k
JOIN "Products" p ON p."Id" = md5('p' || (1 + ((g * 31 + k * 7) % 2000)))::uuid;

ANALYZE;