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

        if (Quantity < 1)
        {
            results.Add(new ValidationResult($"Invalid number of units. Minimum value is 1.", new[] { "Quantity" }));
        }

        if (Quantity > 100)
        {
            results.Add(new ValidationResult($"Invalid number of units. Maximum value is 100.", new[] { "Quantity" }));
        }

        return results;
    }
}
