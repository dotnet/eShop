using Microsoft.eShopOnContainers.Services.Ordering.API.Application.Commands;
using Microsoft.eShopOnContainers.Services.Ordering.API.Application.Validators;
using Xunit;

namespace Ordering.UnitTests.Application.Validators
{
    public class CompleteOrderCommandValidatorTest
    {
        private readonly CompleteOrderCommandValidator _validator;

        public CompleteOrderCommandValidatorTest()
        {
            _validator = new CompleteOrderCommandValidator();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_InvalidOrderId_ShouldHaveValidationError(int orderId)
        {
            // Arrange
            var command = new CompleteOrderCommand(orderId);

            // Act
            var validationResult = _validator.Validate(command);

            // Assert
            Assert.False(validationResult.IsValid);
            Assert.Contains(validationResult.Errors, err => err.PropertyName == "OrderId");
        }

        [Fact]
        public void Validate_ValidOrderId_ShouldNotHaveValidationError()
        {
            // Arrange
            var command = new CompleteOrderCommand(1);

            // Act
            var validationResult = _validator.Validate(command);

            // Assert
            Assert.True(validationResult.IsValid);
        }
    }
} 