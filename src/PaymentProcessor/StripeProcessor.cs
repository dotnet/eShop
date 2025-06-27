using Stripe;  
using System;  
using System.Threading.Tasks;  
  
public class StripeProcessor  
{  
    private readonly string _apiKey;  
  
    public StripeProcessor(string apiKey)  
    {  
        _apiKey = apiKey;  
        StripeConfiguration.ApiKey = _apiKey; // Set the API key for the Stripe SDK  
    }  
  
    /// <summary>  
    /// Creates a new customer in Stripe.  
    /// </summary>  
    /// <param name="email">The customer's email address.</param>  
    /// <param name="name">The customer's name.</param>  
    /// <returns>The created Stripe customer object.</returns>  
    public async Task<Customer> CreateCustomerAsync(string email, string name)  
    {  
        var customerService = new CustomerService();  
        var customer = await customerService.CreateAsync(new CustomerCreateOptions  
        {  
            Email = email,  
            Name = name  
        });  
  
        return customer;  
    }  
  
    /// <summary>  
    /// Charges a customer or payment method.  
    /// </summary>  
    /// <param name="amount">The amount to charge in cents (e.g., $10.00 = 1000).</param>  
    /// <param name="currency">The currency (e.g., "usd").</param>  
    /// <param name="paymentMethodId">The payment method ID.</param>  
    /// <param name="customerId">Optional: The customer ID (if the payment method is attached to a customer).</param>  
    /// <returns>The created payment intent object.</returns>  
    public async Task<PaymentIntent> ProcessPaymentAsync(long amount, string currency, string paymentMethodId, string customerId = null)  
    {  
        var paymentIntentService = new PaymentIntentService();  
  
        var options = new PaymentIntentCreateOptions  
        {  
            Amount = amount,  
            Currency = currency,  
            PaymentMethod = paymentMethodId,  
            Confirm = true, // Automatically confirm the payment  
            Customer = customerId, // Optional: attach to a customer  
        };  
  
        var paymentIntent = await paymentIntentService.CreateAsync(options);  
  
        return paymentIntent;  
    }  
  
    /// <summary>  
    /// Refunds a payment.  
    /// </summary>  
    /// <param name="paymentIntentId">The ID of the payment intent to refund.</param>  
    /// <param name="amount">Optional: The amount to refund in cents (if null, refunds the full amount).</param>  
    /// <returns>The created refund object.</returns>  
    public async Task<Refund> RefundPaymentAsync(string paymentIntentId, long? amount = null)  
    {  
        var refundService = new RefundService();  
  
        var options = new RefundCreateOptions  
        {  
            PaymentIntent = paymentIntentId,  
            Amount = amount // Optional: refund partial amount  
        };  
  
        var refund = await refundService.CreateAsync(options);  
  
        return refund;  
    }  
}  
