namespace eShop.Ordering.API.Application.Behaviors;

public class TransactionBehavior<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    OrderingContext dbContext,
    IOrderingIntegrationEventService orderingIntegrationEventService,
    ILogger<TransactionBehavior<TCommand, TResult>> logger) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        var response = default(TResult);
        var typeName = command.GetGenericTypeName();

        try
        {
            if (dbContext.HasActiveTransaction)
            {
                return await inner.Handle(command, cancellationToken);
            }

            var strategy = dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                Guid transactionId;

                await using var transaction = await dbContext.BeginTransactionAsync();
                using (logger.BeginScope(new List<KeyValuePair<string, object>> { new("TransactionContext", transaction.TransactionId) }))
                {
                    logger.LogInformation("Begin transaction {TransactionId} for {CommandName} ({@Command})", transaction.TransactionId, typeName, command);

                    response = await inner.Handle(command, cancellationToken);

                    logger.LogInformation("Commit transaction {TransactionId} for {CommandName}", transaction.TransactionId, typeName);

                    await dbContext.CommitTransactionAsync(transaction);

                    transactionId = transaction.TransactionId;
                }

                await orderingIntegrationEventService.PublishEventsThroughEventBusAsync(transactionId);
            });

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error Handling transaction for {CommandName} ({@Command})", typeName, command);

            throw;
        }
    }
}