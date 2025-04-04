namespace eShop.Ordering.API.Application.Validations;

public class CompleteOrderByShipmentCommandValidator : AbstractValidator<CompleteOrderByShipmentCommand>
{
    public CompleteOrderByShipmentCommandValidator(ILogger<CompleteOrderByShipmentCommandValidator> logger)
    {
        RuleFor(order => order.OrderNumber).NotEmpty().WithMessage("No orderId found");

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("INSTANCE CREATED - {ClassName}", GetType().Name);
        }
    }
}
