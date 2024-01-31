using CommunityToolkit.Mvvm.Messaging.Messages;

namespace eShop.ClientApp.Messages;

public class AddProductMessage : ValueChangedMessage<int>
{
    public AddProductMessage(int count) : base(count)
    {
    }
}

