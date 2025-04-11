namespace Inked.Ordering.API.Application.Commands;

public class IdentifiedCommand<T, R> : IRequest<R>
    where T : IRequest<R>
{
    public IdentifiedCommand(T command, Guid id)
    {
        Command = command;
        Id = id;
    }

    public T Command { get; }
    public Guid Id { get; }
}
