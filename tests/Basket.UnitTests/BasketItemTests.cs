using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eShop.Basket.API.Model
{
    [TestClass]
    public class BasketItemTest
    {
        [TestMethod]
        public void Validate_WithValidQuantity_ReturnsNoValidationErrors()
        {
            // Arrange
            var item = new BasketItem
            {
                Id = "1",
                ProductId = 10,
                ProductName = "Test Product",
                UnitPrice = 5.0m,
                OldUnitPrice = 8.0m,
                Quantity = 2,
                PictureUrl = "test.jpg"
            };
            var context = new ValidationContext(item);

            // Act
            var results = new List<ValidationResult>(item.Validate(context));

            // Assert
            Assert.AreEqual(0, results.Count);
        }
    }
}
