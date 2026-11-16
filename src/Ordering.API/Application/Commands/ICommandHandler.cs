namespace eShop.Ordering.API.Application.Commands;

/// <summary>
/// Marker interface for a command that produces a <typeparamref name="TResult"/> when handled.
/// </summary>
public interface ICommand<TResult>;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
}
