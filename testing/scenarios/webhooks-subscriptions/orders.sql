-- Order 2 is dated 45 s before load, so it passes the 1 minute grace period about 15 s after loading and
-- reaches Paid on the following OrderProcessor poll (15-50 s after load). That leaves time for a test to
-- register a webhook before the OrderPaid event fires.
INSERT INTO ordering.orders ("Id", "BuyerId", "PaymentMethodId", "OrderDate", "OrderStatus", "Description",
  "Address_Street", "Address_City", "Address_State", "Address_Country", "Address_ZipCode") VALUES
  (1, 1, 1, '2025-08-01T10:00:00Z', 'Paid', 'Paid.', '1 Alice Way', 'Redmond', 'WA', 'U.S.', '98052'),
  (2, 1, 1, now() - interval '45 seconds', 'Submitted', NULL, '1 Alice Way', 'Redmond', 'WA', 'U.S.', '98052');

INSERT INTO ordering."orderItems" ("Id", "OrderId", "ProductId", "ProductName", "UnitPrice", "Discount", "Units", "PictureUrl") VALUES
  (1, 1, 8, 'Frostbite Insulated Jacket', 179.99, 0, 1, NULL),
  (2, 2, 9, 'VenturePro GPS Watch', 199.99, 0, 1, NULL);
