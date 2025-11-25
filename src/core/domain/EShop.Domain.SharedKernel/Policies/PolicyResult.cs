using EShop.Domain.SharedKernel.Events;

namespace EShop.Domain.SharedKernel.Policies
{
    public class PolicyResult
    {
        public bool IsSuccess { get; }
        public string? Message { get; }
        public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

        private PolicyResult(bool isSuccess, string? message, IEnumerable<IDomainEvent>? events)
        {
            IsSuccess = isSuccess;
            Message = message;
            DomainEvents = events?.ToList() ?? new List<IDomainEvent>();
        }

        public static PolicyResult Success(params IDomainEvent[] events) =>
            new(true, null, events);

        public static PolicyResult Fail(string message, params IDomainEvent[] events) =>
            new(false, message, events);
    }
}