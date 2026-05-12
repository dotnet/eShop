using System.ComponentModel.DataAnnotations;

namespace eShop.WebApp.Services;

#pragma warning disable ASP0029 // Microsoft.Extensions.Validation is experimental
[Microsoft.Extensions.Validation.ValidatableType]
#pragma warning restore ASP0029
public class BasketCheckoutInfo
{
    [Required]
    public string? Street { get; set; }

    [Required]
    public string? City { get; set; }

    [Required]
    public string? State { get; set; }

    [Required]
    public string? Country { get; set; }

    [Required]
    public string? ZipCode { get; set; }

    [Required]
    public PaymentCardInfo? PaymentCard { get; set; }

    public string? Buyer { get; set; }
    public Guid RequestId { get; set; }
}
