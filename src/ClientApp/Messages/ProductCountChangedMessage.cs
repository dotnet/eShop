using CommunityToolkit.Mvvm.Messaging.Messages;

namespace eShop.ClientApp.Messages;

public class ProductCountChangedMessage(int count) : ValueChangedMessage<int>(count);
