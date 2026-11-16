using Order = eShop.Ordering.Domain.AggregatesModel.OrderAggregate.Order;

namespace eShop.Ordering.API.Application;

public class OrderApplicationService(
    OrderingContext dbContext,
    IOrderRepository orderRepository,
    IBuyerRepository buyerRepository,
    IRequestManager requestManager,
    IOrderingIntegrationEventService integrationEvents,
    IValidator<CreateOrderCommand> createOrderValidator,
    IValidator<CancelOrderCommand> cancelOrderValidator,
    IValidator<ShipOrderCommand> shipOrderValidator,
    ILogger<OrderApplicationService> logger)
{
    public Task<bool> CreateOrderAsync(CreateOrderCommand command, Guid requestId, CancellationToken cancellationToken = default)
    {
        if (requestId == Guid.Empty)
        {
            throw new OrderingDomainException("RequestId is missing.");
        }

        return ExecuteInTransactionAsync(nameof(CreateOrderAsync), async () =>
        {
            if (await requestManager.ExistAsync(requestId))
            {
                return true;
            }

            await requestManager.CreateRequestForCommandAsync<CreateOrderCommand>(requestId);
            CheckValidation(await createOrderValidator.ValidateAsync(command, cancellationToken), nameof(CreateOrderCommand));

            await integrationEvents.AddAndSaveEventAsync(new OrderStartedIntegrationEvent(command.UserId));

            var address = new Address(command.Street, command.City, command.State, command.Country, command.ZipCode);
            var order = new Order(address);
            foreach (var item in command.OrderItems)
            {
                order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
            }

            logger.LogInformation("Creating Order - Order: {@Order}", order);
            orderRepository.Add(order);

            var buyer = await buyerRepository.FindAsync(command.UserId);
            var buyerExisted = buyer is not null;
            buyer ??= new Buyer(command.UserId, command.UserName);

            var payment = buyer.VerifyOrAddPaymentMethod(
                command.CardTypeId != 0 ? command.CardTypeId : 1,
                $"Payment Method on {DateTime.UtcNow}",
                command.CardNumber, command.CardSecurityNumber, command.CardHolderName, command.CardExpiration);

            if (!buyerExisted)
            {
                buyerRepository.Add(buyer);
            }

            // Persist first so both new and existing payment methods have their database IDs.
            await dbContext.SaveChangesAsync(cancellationToken);
            order.SetPaymentMethodVerified(buyer.Id, payment.Id);
            OrderingApiTrace.LogOrderPaymentMethodUpdated(logger, order.Id, nameof(payment), payment.Id);
            await dbContext.SaveChangesAsync(cancellationToken);

            await integrationEvents.AddAndSaveEventAsync(
                new OrderStatusChangedToSubmittedIntegrationEvent(order.Id, order.OrderStatus, buyer.Name, buyer.IdentityGuid));
            OrderingApiTrace.LogOrderBuyerAndPaymentValidatedOrUpdated(logger, buyer.Id, order.Id);
            return true;
        }, cancellationToken, returnFalseOnDomainFailure: true);
    }

    public Task<bool> CancelOrderAsync(CancelOrderCommand command, Guid? requestId = null, CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(nameof(CancelOrderAsync), async () =>
        {
            if (requestId is { } id)
            {
                if (await requestManager.ExistAsync(id))
                {
                    return true;
                }

                await requestManager.CreateRequestForCommandAsync<CancelOrderCommand>(id);
            }

            CheckValidation(await cancelOrderValidator.ValidateAsync(command, cancellationToken), nameof(CancelOrderCommand));
            var order = await orderRepository.GetAsync(command.OrderNumber);
            if (order is null)
            {
                return false;
            }

            order.SetCancelledStatus();
            OrderingApiTrace.LogOrderStatusUpdated(logger, order.Id, OrderStatus.Cancelled);
            var buyer = await buyerRepository.FindByIdAsync(order.BuyerId.Value);
            await integrationEvents.AddAndSaveEventAsync(
                new OrderStatusChangedToCancelledIntegrationEvent(order.Id, order.OrderStatus, buyer.Name, buyer.IdentityGuid));
            return true;
        }, cancellationToken, returnFalseOnDomainFailure: requestId.HasValue);
    }

    public Task<bool> ShipOrderAsync(ShipOrderCommand command, Guid requestId, CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(nameof(ShipOrderAsync), async () =>
        {
            if (await requestManager.ExistAsync(requestId))
            {
                return true;
            }

            await requestManager.CreateRequestForCommandAsync<ShipOrderCommand>(requestId);
            CheckValidation(await shipOrderValidator.ValidateAsync(command, cancellationToken), nameof(ShipOrderCommand));
            var order = await orderRepository.GetAsync(command.OrderNumber);
            if (order is null)
            {
                return false;
            }

            order.SetShippedStatus();
            OrderingApiTrace.LogOrderStatusUpdated(logger, order.Id, OrderStatus.Shipped);
            var buyer = await buyerRepository.FindByIdAsync(order.BuyerId.Value);
            await integrationEvents.AddAndSaveEventAsync(
                new OrderStatusChangedToShippedIntegrationEvent(order.Id, order.OrderStatus, buyer.Name, buyer.IdentityGuid));
            return true;
        }, cancellationToken, returnFalseOnDomainFailure: true);
    }

    public Task<bool> SetAwaitingValidationOrderStatusAsync(SetAwaitingValidationOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(nameof(SetAwaitingValidationOrderStatusAsync), async () =>
        {
            var order = await orderRepository.GetAsync(command.OrderNumber);
            if (order is null)
            {
                return false;
            }

            var previousStatus = order.OrderStatus;
            order.SetAwaitingValidationStatus();
            if (previousStatus == OrderStatus.Submitted)
            {
                OrderingApiTrace.LogOrderStatusUpdated(logger, order.Id, OrderStatus.AwaitingValidation);
                var buyer = await buyerRepository.FindByIdAsync(order.BuyerId.Value);
                var stock = order.OrderItems.Select(item => new OrderStockItem(item.ProductId, item.Units));
                await integrationEvents.AddAndSaveEventAsync(
                    new OrderStatusChangedToAwaitingValidationIntegrationEvent(order.Id, order.OrderStatus, buyer.Name, buyer.IdentityGuid, stock));
            }

            return true;
        }, cancellationToken);
    }

    public Task<bool> SetStockConfirmedOrderStatusAsync(SetStockConfirmedOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(nameof(SetStockConfirmedOrderStatusAsync), async () =>
        {
            await Task.Delay(10000, cancellationToken);
            var order = await orderRepository.GetAsync(command.OrderNumber);
            if (order is null)
            {
                return false;
            }

            var previousStatus = order.OrderStatus;
            order.SetStockConfirmedStatus();
            if (previousStatus == OrderStatus.AwaitingValidation)
            {
                OrderingApiTrace.LogOrderStatusUpdated(logger, order.Id, OrderStatus.StockConfirmed);
                var buyer = await buyerRepository.FindByIdAsync(order.BuyerId.Value);
                await integrationEvents.AddAndSaveEventAsync(
                    new OrderStatusChangedToStockConfirmedIntegrationEvent(order.Id, order.OrderStatus, buyer.Name, buyer.IdentityGuid));
            }

            return true;
        }, cancellationToken);
    }

    public Task<bool> SetPaidOrderStatusAsync(SetPaidOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(nameof(SetPaidOrderStatusAsync), async () =>
        {
            await Task.Delay(10000, cancellationToken);
            var order = await orderRepository.GetAsync(command.OrderNumber);
            if (order is null)
            {
                return false;
            }

            var previousStatus = order.OrderStatus;
            order.SetPaidStatus();
            if (previousStatus == OrderStatus.StockConfirmed)
            {
                OrderingApiTrace.LogOrderStatusUpdated(logger, order.Id, OrderStatus.Paid);
                var buyer = await buyerRepository.FindByIdAsync(order.BuyerId.Value);
                var stock = order.OrderItems.Select(item => new OrderStockItem(item.ProductId, item.Units));
                await integrationEvents.AddAndSaveEventAsync(
                    new OrderStatusChangedToPaidIntegrationEvent(order.Id, order.OrderStatus, buyer.Name, buyer.IdentityGuid, stock));
            }

            return true;
        }, cancellationToken);
    }

    public Task<bool> SetStockRejectedOrderStatusAsync(SetStockRejectedOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(nameof(SetStockRejectedOrderStatusAsync), async () =>
        {
            await Task.Delay(10000, cancellationToken);
            var order = await orderRepository.GetAsync(command.OrderNumber);
            if (order is null)
            {
                return false;
            }

            order.SetCancelledStatusWhenStockIsRejected(command.OrderStockItems);
            return true;
        }, cancellationToken);
    }

    public Task<OrderDraftDTO> CreateOrderDraftAsync(CreateOrderDraftCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Creating order draft for buyer {BuyerId}", command.BuyerId);
        var order = Order.NewDraft();
        foreach (var item in command.Items.ToOrderItemsDTO())
        {
            order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
        }

        return Task.FromResult(OrderDraftDTO.FromOrder(order));
    }

    private void CheckValidation(FluentValidation.Results.ValidationResult result, string commandName)
    {
        logger.LogInformation("Validating command {CommandType}", commandName);
        if (!result.IsValid)
        {
            logger.LogWarning("Validation errors - {CommandType} - Errors: {@ValidationErrors}", commandName, result.Errors);
            throw new OrderingDomainException(
                $"Command Validation Errors for type {commandName}", new ValidationException("Validation exception", result.Errors));
        }
    }

    private async Task<bool> ExecuteInTransactionAsync(
        string operation, Func<Task<bool>> action, CancellationToken cancellationToken, bool returnFalseOnDomainFailure = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Handling order operation {Operation}", operation);
        try
        {
            var response = false;
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async _ =>
            {
                await using var transaction = await dbContext.BeginTransactionAsync();
                using var scope = logger.BeginScope(new List<KeyValuePair<string, object>> { new("TransactionContext", transaction.TransactionId) });
                try
                {
                    logger.LogInformation("Begin transaction {TransactionId} for {Operation}", transaction.TransactionId, operation);
                    response = await action();
                    await dbContext.CommitTransactionAsync(transaction);
                    logger.LogInformation("Committed transaction {TransactionId} for {Operation}", transaction.TransactionId, operation);
                }
                catch
                {
                    dbContext.RollbackTransaction();
                    dbContext.ChangeTracker.Clear();
                    throw;
                }

                await integrationEvents.PublishEventsThroughEventBusAsync(transaction.TransactionId);
            }, cancellationToken);

            logger.LogInformation("Order operation {Operation} handled - response: {Response}", operation, response);
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error handling order operation {Operation}", operation);
            if (returnFalseOnDomainFailure && ex is OrderingDomainException)
            {
                // Identified HTTP operations retain their boolean result for validation and domain failures.
                return false;
            }

            throw;
        }
    }
}
