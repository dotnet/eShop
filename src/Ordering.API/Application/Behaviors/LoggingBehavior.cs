namespace eShop.Ordering.API.Application.Behaviors;

public class LoggingBehavior<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<LoggingBehavior<TCommand, TResult>> logger) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handling command {CommandName} ({@Command})", command.GetGenericTypeName(), command);
        var response = await inner.Handle(command, cancellationToken);
        logger.LogInformation("Command {CommandName} handled - response: {@Response}", command.GetGenericTypeName(), response);

        return response;
    }
}