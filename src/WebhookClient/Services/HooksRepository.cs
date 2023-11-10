namespace WebhookClient.Services;

public class HooksRepository
{
    private readonly List<WebHookReceived> _data = new();

    public event EventHandler? OnReceived;

    public Task AddNew(WebHookReceived hook)
    {
        _data.Add(hook);
        OnReceived?.Invoke(this, default!);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<WebHookReceived>> GetAll()
    {
        return Task.FromResult(_data.AsEnumerable());
    }
}
