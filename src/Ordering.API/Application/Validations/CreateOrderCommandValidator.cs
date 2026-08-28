namespace eShop.Ordering.API.Application.Validations;
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator(ILogger<CreateOrderCommandValidator> logger)
    {
        RuleFor(command => command.City).NotEmpty();
        RuleFor(command => command.Street).NotEmpty();
        RuleFor(command => command.State).NotEmpty();
        RuleFor(command => command.Country).NotEmpty();
        RuleFor(command => command.ZipCode).NotEmpty();
        RuleFor(command => command.PaymentMethodId).NotEmpty().MaximumLength(200);
        RuleFor(command => command.OrderItems).Must(ContainOrderItems).WithMessage("No order items found");

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("INSTANCE CREATED - {ClassName}", GetType().Name);
        }
    }

    private bool ContainOrderItems(IEnumerable<OrderItemDTO> orderItems)
    {
        return orderItems.Any();
    }
}
