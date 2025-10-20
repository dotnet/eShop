namespace eShop.Basket.API.Model;

public class BasketItem : IValidatableObject
{
    public string Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal OldUnitPrice { get; set; }
    public int Quantity { get; set; }
    public string PictureUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (Quantity < 1 || Quantity > 100)
        {
            results.Add(new ValidationResult("Invalid number of units", new[] { "Quantity" }));
        }

        if (string.IsNullOrEmpty(ProductName))
        {
            results.Add(new ValidationResult("Wir brauchen immer einen Produktnamen!", new[] { "ProductName" }));
        }

        return results;
    }
}
