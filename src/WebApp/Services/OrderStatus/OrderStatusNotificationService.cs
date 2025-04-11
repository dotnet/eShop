namespace Inked.WebApp.Services;

public class OrderStatusNotificationService
{
    private readonly Dictionary<string, HashSet<Subscription>> _subscriptionsByBuyerId = new();

    // Locking manually because we need multiple values per key, and only need to lock very briefly
    private readonly object _subscriptionsLock = new();

    public IDisposable SubscribeToOrderStatusNotifications(string buyerId, Func<Task> callback)
    {
        var subscription = new Subscription(this, buyerId, callback);

        lock (_subscriptionsLock)
        {
            if (!_subscriptionsByBuyerId.TryGetValue(buyerId, out var subscriptions))
            {
                subscriptions = [];
                _subscriptionsByBuyerId.Add(buyerId, subscriptions);
            }

            subscriptions.Add(subscription);
        }

        return subscription;
    }

    public Task NotifyOrderStatusChangedAsync(string buyerId)
    {
        lock (_subscriptionsLock)
        {
            return _subscriptionsByBuyerId.TryGetValue(buyerId, out var subscriptions)
                ? Task.WhenAll(subscriptions.Select(s => s.NotifyAsync()))
                : Task.CompletedTask;
        }
    }

    private void Unsubscribe(string buyerId, Subscription subscription)
    {
        lock (_subscriptionsLock)
        {
            if (_subscriptionsByBuyerId.TryGetValue(buyerId, out var subscriptions))
            {
                subscriptions.Remove(subscription);
                if (subscriptions.Count == 0)
                {
                    _subscriptionsByBuyerId.Remove(buyerId);
                }
            }
        }
    }

    private class Subscription(OrderStatusNotificationService owner, string buyerId, Func<Task> callback) : IDisposable
    {
        public void Dispose()
        {
            owner.Unsubscribe(buyerId, this);
        }

        public Task NotifyAsync()
        {
            return callback();
        }
    }
}
