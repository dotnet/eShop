namespace eShop.ClientApp.Services;

public class DialogService : IDialogService
{
    public Task ShowAlertAsync(string message, string title, string buttonLabel)
    {
        return AppShell.Current.DisplayAlert(title, message, buttonLabel);
    }
}
