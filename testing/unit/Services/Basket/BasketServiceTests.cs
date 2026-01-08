using eShop.Basket.API.Grpc;
using eShop.Basket.API.Model;
using eShop.Basket.API.Repositories;
using eShop.UnitTests.Shared.TestData;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace eShop.UnitTests.Services.Basket;

[TestClass]
public class BasketServiceTests
{
    private BasketService _basketService;
    private IBasketRepository _mockRepository;
    private ILogger<BasketService> _mockLogger;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = Substitute.For<IBasketRepository>();
        _mockLogger = Substitute.For<ILogger<BasketService>>();
        _basketService = new BasketService(_mockRepository, _mockLogger);
    }

    [TestMethod]
    public async Task GetBasket_ValidUserId_ReturnsBasketWithItems()
    {
        // Arrange
        var buyerId = "test-buyer-123";
        var customerBasket = BasketTestData.CreateValidCustomerBasket(buyerId);
        _mockRepository.GetBasketAsync(buyerId).Returns(Task.FromResult(customerBasket));

        var serverCallContext = CreateServerCallContext(buyerId);

        // Act
        var response = await _basketService.GetBasket(new GetBasketRequest(), serverCallContext);

        // Assert
        response.ShouldNotBeNull();
        response.Items.Count.ShouldBe(3);
        response.BuyerId.ShouldBe(buyerId);
    }

    [TestMethod]
    public async Task GetBasket_NoUserId_ReturnsEmptyBasket()
    {
        // Arrange
        var serverCallContext = CreateServerCallContext(null);

        // Act
        var response = await _basketService.GetBasket(new GetBasketRequest(), serverCallContext);

        // Assert
        response.ShouldNotBeNull();
        response.Items.ShouldBeEmpty();
        response.BuyerId.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetBasket_NonExistentUser_ReturnsEmptyBasket()
    {
        // Arrange
        var buyerId = "non-existent-buyer";
        _mockRepository.GetBasketAsync(buyerId).Returns(Task.FromResult<CustomerBasket>(null!));

        var serverCallContext = CreateServerCallContext(buyerId);

        // Act
        var response = await _basketService.GetBasket(new GetBasketRequest(), serverCallContext);

        // Assert
        response.ShouldNotBeNull();
        response.Items.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task UpdateBasket_ValidBasket_UpdatesSuccessfully()
    {
        // Arrange
        var buyerId = "test-buyer-123";
        var basketItems = BasketTestData.CreateBasketItems(2);
        var updateRequest = new UpdateBasketRequest
        {
            BuyerId = buyerId
        };
        
        foreach (var item in basketItems)
        {
            updateRequest.Items.Add(new BasketItemResponse
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = (double)item.UnitPrice,
                Quantity = item.Quantity,
                PictureUrl = item.PictureUrl
            });
        }

        var expectedBasket = BasketTestData.CreateValidCustomerBasket(buyerId, basketItems);
        _mockRepository.UpdateBasketAsync(Arg.Any<CustomerBasket>()).Returns(Task.FromResult(expectedBasket));

        var serverCallContext = CreateServerCallContext(buyerId);

        // Act
        var response = await _basketService.UpdateBasket(updateRequest, serverCallContext);

        // Assert
        response.ShouldNotBeNull();
        response.Items.Count.ShouldBe(2);
        response.BuyerId.ShouldBe(buyerId);
        
        await _mockRepository.Received(1).UpdateBasketAsync(Arg.Any<CustomerBasket>());
    }

    [TestMethod]
    public async Task DeleteBasket_ValidUserId_DeletesSuccessfully()
    {
        // Arrange
        var buyerId = "test-buyer-123";
        _mockRepository.DeleteBasketAsync(buyerId).Returns(Task.FromResult(true));

        var serverCallContext = CreateServerCallContext(buyerId);

        // Act
        var response = await _basketService.DeleteBasket(new DeleteBasketRequest(), serverCallContext);

        // Assert
        response.ShouldNotBeNull();
        await _mockRepository.Received(1).DeleteBasketAsync(buyerId);
    }

    [TestMethod]
    public async Task AddItemToBasket_ValidItem_AddsSuccessfully()
    {
        // Arrange
        var buyerId = "test-buyer-123";
        var existingBasket = BasketTestData.CreateValidCustomerBasket(buyerId, new List<BasketItem>());
        var newItem = BasketTestData.CreateBasketItem(id: "new-item", productId: 999);

        _mockRepository.GetBasketAsync(buyerId).Returns(Task.FromResult(existingBasket));
        _mockRepository.UpdateBasketAsync(Arg.Any<CustomerBasket>()).Returns(Task.FromResult(existingBasket));

        var serverCallContext = CreateServerCallContext(buyerId);
        var addItemRequest = new AddItemToBasketRequest
        {
            ProductId = newItem.ProductId,
            ProductName = newItem.ProductName,
            UnitPrice = (double)newItem.UnitPrice,
            Quantity = newItem.Quantity,
            PictureUrl = newItem.PictureUrl
        };

        // Act
        var response = await _basketService.AddItemToBasket(addItemRequest, serverCallContext);

        // Assert
        response.ShouldNotBeNull();
        await _mockRepository.Received(1).UpdateBasketAsync(Arg.Is<CustomerBasket>(
            basket => basket.Items.Any(item => item.ProductId == newItem.ProductId)
        ));
    }

    private static ServerCallContext CreateServerCallContext(string? userId)
    {
        var serverCallContext = Substitute.For<ServerCallContext>();
        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrEmpty(userId))
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId)]));
        }

        serverCallContext.GetHttpContext().Returns(httpContext);
        return serverCallContext;
    }
}