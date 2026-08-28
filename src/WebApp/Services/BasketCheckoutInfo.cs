using System.ComponentModel.DataAnnotations;

namespace eShop.WebApp.Services;

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
    public string PaymentMethodId { get; set; } = "pm_sample_visa";

    public string? Buyer { get; set; }
    public Guid RequestId { get; set; }
}
