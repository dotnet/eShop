-- Brand 4 and type 4 deliberately have no items. Names sort by their zero-padded number.
INSERT INTO "CatalogBrand" ("Id", "Brand") VALUES
  (1, 'Edge Alpha'),
  (2, 'Edge Beta'),
  (3, 'Edge Gamma'),
  (4, 'Edge Unused Brand');

INSERT INTO "CatalogType" ("Id", "Type") VALUES
  (1, 'Edge Boots'),
  (2, 'Edge Packs'),
  (3, 'Edge Tents'),
  (4, 'Edge Unused Type');

INSERT INTO "Catalog" ("Id", "Name", "Description", "Price", "PictureFileName", "CatalogTypeId", "CatalogBrandId", "AvailableStock", "RestockThreshold", "MaxStockThreshold", "OnReorder") VALUES
  (1,  'Edge 01 Penny Boot',                              'Cheapest item: price 0.01.',           0.01,    '1.webp',  1, 1, 10, 5, 100, false),
  (2,  'Edge 02 Trail Boot',                              'Plain trail boot.',                    59.50,   '2.webp',  1, 1, 10, 5, 100, false),
  (3,  'Edge 03 Summit Boot',                             'Price with a thousands separator.',    1234.5,  '3.webp',  1, 1, 5,  5, 100, false),
  (4,  'Edge 04 Day Pack',                                'Whole-dollar price.',                  25,      '4.webp',  2, 1, 10, 5, 100, false),
  (5,  'Edge 05 Expedition Pack',                         'Most expensive item: price 9999.99.',  9999.99, '5.webp',  2, 2, 3,  1, 100, false),
  (6,  'Edge 06 Zero Stock Pack',                         'No stock left.',                       45.00,   '6.webp',  2, 2, 0,  5, 100, true),
  (7,  'Edge 07 Dome Tent',                               'Plain dome tent.',                     199.99,  '7.webp',  3, 2, 10, 5, 100, false),
  (8,  'Edge 08 Blank Description Tent',                  '',                                     120,     '8.webp',  3, 2, 10, 5, 100, false),
  (9,  'Edge 09 Tunnel Tent',                             'Last item on page 1.',                 349.95,  '9.webp',  3, 3, 10, 5, 100, false),
  (10, 'Edge 10 Bivy',                                    'First item on page 2.',                89,      '10.webp', 3, 3, 10, 5, 100, false),
  (11, 'Edge 11 Hiking Boot',                             'Plain hiking boot.',                   75,      '11.webp', 1, 3, 10, 5, 100, false),
  (12, 'Edge 12 Mega Pack',                               'Price of exactly one thousand.',       1000,    '12.webp', 2, 3, 10, 5, 100, false),
  (13, 'Edge 13 Ultra Long Name Hiking Pack Edition Max 50', 'Name uses the full 50 characters.', 15,      '13.webp', 2, 3, 10, 5, 100, false),
  (14, 'Edge 14 Last Item',                               'Last item on page 2.',                 10,      '14.webp', 3, 1, 10, 5, 100, false);
