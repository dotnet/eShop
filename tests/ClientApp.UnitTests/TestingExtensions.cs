using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ClientApp.UnitTests;

public static class TestingExtensions
{
    public static async Task ExecuteUntilComplete(this ICommand command, object? parameter = null)
    {
        if (command is IAsyncRelayCommand arc)
        {
            await arc.ExecuteAsync(parameter);
            return;
        }

        command.Execute(parameter);
    }
}

