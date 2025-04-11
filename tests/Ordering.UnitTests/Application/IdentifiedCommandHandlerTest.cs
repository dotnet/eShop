namespace Inked.Ordering.UnitTests.Application;

[TestClass]
public class IdentifiedCommandHandlerTest
{
    private readonly ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> _loggerMock;
    private readonly IMediator _mediator;
    private readonly IRequestManager _requestManager;

    public IdentifiedCommandHandlerTest()
    {
        _requestManager = Substitute.For<IRequestManager>();
        _mediator = Substitute.For<IMediator>();
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

        _mediator.Send(Arg.Any<IRequest<bool>>())
            .Returns(Task.FromResult(true));

        // Act
        var handler = new CreateOrderIdentifiedCommandHandler(_mediator, _requestManager, _loggerMock);
        var result = await handler.Handle(fakeOrderCmd, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        await _mediator.Received().Send(Arg.Any<IRequest<bool>>());
    }

    [TestMethod]
    public async Task Handler_sends_no_command_when_order_already_exists()
    {
        // Arrange
        var fakeGuid = Guid.NewGuid();
        var fakeOrderCmd = new IdentifiedCommand<CreateOrderCommand, bool>(FakeOrderRequest(), fakeGuid);

        _requestManager.ExistAsync(Arg.Any<Guid>())
            .Returns(Task.FromResult(true));

        _mediator.Send(Arg.Any<IRequest<bool>>())
            .Returns(Task.FromResult(true));

        // Act
        var handler = new CreateOrderIdentifiedCommandHandler(_mediator, _requestManager, _loggerMock);
        var result = await handler.Handle(fakeOrderCmd, CancellationToken.None);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<IRequest<bool>>());
    }

    private CreateOrderCommand FakeOrderRequest(Dictionary<string, object> args = null)
    {
        return new CreateOrderCommand(
            new List<BasketItem>(),
            args != null && args.ContainsKey("userId") ? (string)args["userId"] : null,
            args != null && args.ContainsKey("userName") ? (string)args["userName"] : null,
            args != null && args.ContainsKey("city") ? (string)args["city"] : null,
            args != null && args.ContainsKey("street") ? (string)args["street"] : null,
            args != null && args.ContainsKey("state") ? (string)args["state"] : null,
            args != null && args.ContainsKey("country") ? (string)args["country"] : null,
            args != null && args.ContainsKey("zipcode") ? (string)args["zipcode"] : null,
            args != null && args.ContainsKey("cardNumber") ? (string)args["cardNumber"] : "1234",
            cardExpiration: args != null && args.ContainsKey("cardExpiration")
                ? (DateTime)args["cardExpiration"]
                : DateTime.MinValue,
            cardSecurityNumber: args != null && args.ContainsKey("cardSecurityNumber")
                ? (string)args["cardSecurityNumber"]
                : "123",
            cardHolderName: args != null && args.ContainsKey("cardHolderName") ? (string)args["cardHolderName"] : "XXX",
            cardTypeId: args != null && args.ContainsKey("cardTypeId") ? (int)args["cardTypeId"] : 0);
    }
}
