using FluentValidation;
using Microsoft.eShopOnContainers.Services.Ordering.API.Application.Commands;

namespace Microsoft.eShopOnContainers.Services.Ordering.API.Application.Validators
{
    public class CompleteOrderCommandValidator : AbstractValidator<CompleteOrderCommand>
    {
        public CompleteOrderCommandValidator()
        {
            RuleFor(command => command.OrderId)
                .NotEmpty()
                .WithMessage("OrderId is required")
                .GreaterThan(0)
                .WithMessage("OrderId must be greater than 0");
        }
    }
} 