using eShop.Basket.API.Model;
using eShop.Catalog.API.Model;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using FsCheck;

namespace eShop.UnitTests.Shared.Generators;

public static class PropertyTestGenerators
{
    #region Product and Catalog Generators

    public static Gen<string> ValidProductNames()
    {
        var productTypes = new[] { "Jacket", "Boots", "Backpack", "Tent", "Sleeping Bag", "Hiking Poles", "Rain Gear" };
        var brands = new[] { "AdventureWorks", "Mountain Peak", "Trail Blazer", "Outdoor Pro" };
        var materials = new[] { "Waterproof", "Lightweight", "Durable", "Breathable", "Insulated" };

        return from productType in Gen.Elements(productTypes)
               from brand in Gen.Elements(brands)
               from material in Gen.Elements(materials)
               select $"{material} {brand} {productType}";
    }

    public static Gen<decimal> ValidPrices()
    {
        return Gen.Choose(1, 100000) // Cents
               .Select(cents => cents / 100m); // Convert to dollars
    }

    public static Gen<int> ValidProductIds()
    {
        return Gen.Choose(1, 10000);
    }

    public static Gen<int> ValidQuantities()
    {
        return Gen.Choose(1, 100);
    }

    public static Gen<CatalogItem> ValidCatalogItems()
    {
        return from id in ValidProductIds()
               from name in ValidProductNames()
               from description in ValidProductDescriptions()
               from price in ValidPrices()
               from typeId in Gen.Choose(1, 10)
               from brandId in Gen.Choose(1, 10)
               from stock in Gen.Choose(0, 1000)
               select new CatalogItem(typeId, brandId, description, name, price, $"product{id}.jpg")
               {
                   Id = id,
                   AvailableStock = stock,
                   RestockThreshold = 10,
                   MaxStockThreshold = 500
               };
    }

    public static Gen<string> ValidProductDescriptions()
    {
        var adjectives = new[] { "High-quality", "Professional", "Lightweight", "Durable", "Comfortable", "Versatile" };
        var features = new[] { "waterproof", "breathable", "insulated", "adjustable", "compact", "ergonomic" };
        var uses = new[] { "hiking", "camping", "climbing", "trekking", "outdoor adventures", "trail running" };

        return from adj in Gen.Elements(adjectives)
               from feature in Gen.Elements(features)
               from use in Gen.Elements(uses)
               select $"{adj} {feature} gear perfect for {use}.";
    }

    #endregion

    #region Basket Generators

    public static Gen<BasketItem> ValidBasketItems()
    {
        return from productId in ValidProductIds()
               from productName in ValidProductNames()
               from price in ValidPrices()
               from quantity in ValidQuantities()
               select new BasketItem
               {
                   Id = Guid.NewGuid().ToString(),
                   ProductId = productId,
                   ProductName = productName,
                   UnitPrice = price,
                   Quantity = quantity,
                   PictureUrl = $"product{productId}.jpg"
               };
    }

    public static Gen<List<BasketItem>> ValidBasketItemLists()
    {
        return Gen.ListOf(ValidBasketItems())
                  .Where(items => items.Count <= 20) // Reasonable basket size
                  .Select(items => items.GroupBy(i => i.ProductId)
                                       .Select(g => g.First())
                                       .ToList()); // Ensure unique product IDs
    }

    public static Gen<CustomerBasket> ValidCustomerBaskets()
    {
        return from buyerId in ValidBuyerIds()
               from items in ValidBasketItemLists()
               select new CustomerBasket
               {
                   BuyerId = buyerId,
                   Items = items
               };
    }

    public static Gen<string> ValidBuyerIds()
    {
        return from prefix in Gen.Elements("user", "customer", "buyer")
               from number in Gen.Choose(1, 10000)
               select $"{prefix}-{number}";
    }

    #endregion

    #region Order Generators

    public static Gen<OrderItem> ValidOrderItems()
    {
        return from productId in ValidProductIds()
               from productName in ValidProductNames()
               from price in ValidPrices()
               from discount in Gen.Choose(0m, price) // Discount can't exceed price
               from quantity in ValidQuantities()
               select new OrderItem(productId, productName, price, discount, $"product{productId}.jpg", quantity);
    }

    public static Gen<List<OrderItem>> ValidOrderItemLists()
    {
        return Gen.ListOf(ValidOrderItems())
                  .Where(items => items.Count <= 50) // Reasonable order size
                  .Select(items => items.GroupBy(i => i.ProductId)
                                       .Select(g => g.First())
                                       .ToList()); // Ensure unique product IDs
    }

    public static Gen<Address> ValidAddresses()
    {
        return from street in ValidStreetAddresses()
               from city in ValidCityNames()
               from state in ValidStateNames()
               from country in ValidCountryNames()
               from zipCode in ValidZipCodes()
               select new Address(street, city, state, country, zipCode);
    }

    #endregion

    #region Address Generators

    public static Gen<string> ValidStreetAddresses()
    {
        return from number in Gen.Choose(1, 9999)
               from streetName in Gen.Elements("Main St", "Oak Ave", "Pine Rd", "Elm Dr", "Cedar Ln", "Maple Way")
               select $"{number} {streetName}";
    }

    public static Gen<string> ValidCityNames()
    {
        return Gen.Elements(
            "Seattle", "Portland", "Denver", "Austin", "Boston", "Chicago", 
            "San Francisco", "New York", "Los Angeles", "Miami", "Phoenix", "Dallas"
        );
    }

    public static Gen<string> ValidStateNames()
    {
        return Gen.Elements(
            "WA", "OR", "CO", "TX", "MA", "IL", "CA", "NY", "FL", "AZ"
        );
    }

    public static Gen<string> ValidCountryNames()
    {
        return Gen.Elements("USA", "Canada", "United States", "US");
    }

    public static Gen<string> ValidZipCodes()
    {
        return Gen.Choose(10000, 99999).Select(zip => zip.ToString());
    }

    #endregion

    #region User and Authentication Generators

    public static Gen<string> ValidUserIds()
    {
        return from prefix in Gen.Elements("user", "customer", "admin")
               from guid in Gen.Fresh(() => Guid.NewGuid())
               select $"{prefix}-{guid}";
    }

    public static Gen<string> ValidEmailAddresses()
    {
        var domains = new[] { "example.com", "test.org", "demo.net", "sample.io" };
        
        return from username in Gen.Elements("john", "jane", "test", "demo", "user", "customer")
               from number in Gen.Choose(1, 999)
               from domain in Gen.Elements(domains)
               select $"{username}{number}@{domain}";
    }

    public static Gen<string> ValidUserNames()
    {
        var firstNames = new[] { "John", "Jane", "Mike", "Sarah", "David", "Lisa", "Tom", "Anna" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
        
        return from firstName in Gen.Elements(firstNames)
               from lastName in Gen.Elements(lastNames)
               select $"{firstName} {lastName}";
    }

    #endregion

    #region Payment Generators

    public static Gen<string> ValidCreditCardNumbers()
    {
        // Generate valid test credit card numbers (Luhn algorithm compliant)
        var testCards = new[]
        {
            "4111111111111111", // Visa
            "4000000000000002", // Visa
            "5555555555554444", // Mastercard
            "5200000000000007", // Mastercard
            "378282246310005",  // American Express
            "371449635398431"   // American Express
        };
        
        return Gen.Elements(testCards);
    }

    public static Gen<string> ValidCardHolderNames()
    {
        return ValidUserNames();
    }

    public static Gen<DateTime> ValidCardExpirationDates()
    {
        return Gen.Choose(1, 60) // 1-60 months from now
                  .Select(months => DateTime.Now.AddMonths(months));
    }

    public static Gen<string> ValidCvvCodes()
    {
        return Gen.Choose(100, 999).Select(cvv => cvv.ToString());
    }

    #endregion

    #region Search and Filter Generators

    public static Gen<string> ValidSearchTerms()
    {
        var terms = new[] { "jacket", "boot", "backpack", "tent", "outdoor", "hiking", "camping", "waterproof" };
        return Gen.Elements(terms);
    }

    public static Gen<int> ValidPageSizes()
    {
        return Gen.Elements(5, 10, 20, 50, 100);
    }

    public static Gen<int> ValidPageIndices()
    {
        return Gen.Choose(0, 10); // First 10 pages
    }

    #endregion

    #region Configuration Generators

    public static Gen<T> ValidEnumValues<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        return Gen.Elements(values);
    }

    public static Gen<string> ValidCorrelationIds()
    {
        return Gen.Fresh(() => Guid.NewGuid().ToString());
    }

    public static Gen<DateTime> ValidTimestamps()
    {
        return Gen.Choose(0, 365) // Within the last year
                  .Select(days => DateTime.UtcNow.AddDays(-days));
    }

    #endregion
}