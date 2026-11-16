namespace eShop.Ordering.UnitTests.Application;

[TestClass]
public class IdentifiedCommandHandlerTest
{
    private readonly IRequestManager _requestManager;
    private readonly ICommandHandler<CreateOrderCommand, bool> _commandHandler;
    private readonly ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> _loggerMock;

    public IdentifiedCommandHandlerTest()
    {
        _requestManager = Substitute.For<IRequestManager>();
        _commandHandler = Substitute.For<ICommandHandler<CreateOrderCommand, bool>>();
        _loggerMock = Substitute.For<ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>>>();
    }

    [TestMethod]
    public async Task Handler_sends_command_when_order_no_exists()
    {
        // Arrange
        var fakeGuid = Guid.NewGuid();
        var fakeOrderCmd = new IdentifiedCommand<CreateOrderCommand, bool>(FakeOrderRequest(), fakeGuid);

        _requestManager.ExistAsync(Arg.Any<Guid>())
            .Returns(Task.FromResult(false));

        _commandHandler.Handle(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var handler = new CreateOrderIdentifiedCommandHandler(_commandHandler, _requestManager, _loggerMock);
        var result = await handler.Handle(fakeOrderCmd, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        await _commandHandler.Received().Handle(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handler_sends_no_command_when_order_already_exists()
    {
        // Arrange
        var fakeGuid = Guid.NewGuid();
        var fakeOrderCmd = new IdentifiedCommand<CreateOrderCommand, bool>(FakeOrderRequest(), fakeGuid);

        _requestManager.ExistAsync(Arg.Any<Guid>())
            .Returns(Task.FromResult(true));

        _commandHandler.Handle(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var handler = new CreateOrderIdentifiedCommandHandler(_commandHandler, _requestManager, _loggerMock);
        var result = await handler.Handle(fakeOrderCmd, CancellationToken.None);

        // Assert
       await  _commandHandler.DidNotReceive().Handle(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>());
    }

    private CreateOrderCommand FakeOrderRequest(Dictionary<string, object> args = null)
    {
        return new CreateOrderCommand(
            new List<BasketItem>(),
            userId: args != null && args.ContainsKey("userId") ? (string)args["userId"] : null,
            userName: args != null && args.ContainsKey("userName") ? (string)args["userName"] : null,
            city: args != null && args.ContainsKey("city") ? (string)args["city"] : null,
            street: args != null && args.ContainsKey("street") ? (string)args["street"] : null,
            state: args != null && args.ContainsKey("state") ? (string)args["state"] : null,
            country: args != null && args.ContainsKey("country") ? (string)args["country"] : null,
            zipcode: args != null && args.ContainsKey("zipcode") ? (string)args["zipcode"] : null,
            cardNumber: args != null && args.ContainsKey("cardNumber") ? (string)args["cardNumber"] : "1234",
            cardExpiration: args != null && args.ContainsKey("cardExpiration") ? (DateTime)args["cardExpiration"] : DateTime.MinValue,
            cardSecurityNumber: args != null && args.ContainsKey("cardSecurityNumber") ? (string)args["cardSecurityNumber"] : "123",
            cardHolderName: args != null && args.ContainsKey("cardHolderName") ? (string)args["cardHolderName"] : "XXX",
            cardTypeId: args != null && args.ContainsKey("cardTypeId") ? (int)args["cardTypeId"] : 0);
    }
}
