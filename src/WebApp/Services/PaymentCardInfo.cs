using System.ComponentModel.DataAnnotations;

namespace eShop.WebApp.Services;

#pragma warning disable ASP0029 // Microsoft.Extensions.Validation is experimental
[Microsoft.Extensions.Validation.ValidatableType]
#pragma warning restore ASP0029
public class PaymentCardInfo
{
    [Required]
    [StringLength(19, MinimumLength = 13)]
    [RegularExpression(@"^\d{13,19}$")]
    public string? CardNumber { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? CardHolderName { get; set; }

    [Required]
    [RegularExpression(@"^\d{3,4}$")]
    public string? CardSecurityNumber { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [FutureDate]
    public DateTime? CardExpiration { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int CardTypeId { get; set; }
}

public class FutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime date)
        {
            if (date <= DateTime.UtcNow)
            {
                return new ValidationResult(ErrorMessage ?? "The field must be a date in the future.");
            }
        }
        return ValidationResult.Success;
    }
}
