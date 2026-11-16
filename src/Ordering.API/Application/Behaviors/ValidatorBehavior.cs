namespace eShop.Ordering.API.Application.Behaviors;

public class ValidatorBehavior<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators,
    ILogger<ValidatorBehavior<TCommand, TResult>> logger) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        var typeName = command.GetGenericTypeName();

        logger.LogInformation("Validating command {CommandType}", typeName);

        var validationTasks = validators.Select(v => v.ValidateAsync(command, cancellationToken));
        var validationResults = await Task.WhenAll(validationTasks);

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();

        if (failures.Count != 0)
        {
            logger.LogWarning("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", typeName, command, failures);

            throw new OrderingDomainException(
                $"Command Validation Errors for type {typeof(TCommand).Name}", new ValidationException("Validation exception", failures));
        }

        return await inner.Handle(command, cancellationToken);
    }
}