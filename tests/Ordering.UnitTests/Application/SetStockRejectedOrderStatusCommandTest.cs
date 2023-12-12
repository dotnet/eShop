using System.Text.Json;

namespace eShop.Ordering.UnitTests.Application;

public class SetStockRejectedOrderStatusCommandTest
{
    [Fact]
    public void Set_Stock_Rejected_OrderStatusCommand_Check_Serialization()
    {
        // Arrange
        var command = new SetStockRejectedOrderStatusCommand(123, new List<int> { 1, 2, 3 });

        // Act
        var json = JsonSerializer.Serialize(command);
        var deserializedCommand = JsonSerializer.Deserialize<SetStockRejectedOrderStatusCommand>(json);

        //Assert
        Assert.Equal(command.OrderNumber, deserializedCommand.OrderNumber);

        //Assert for List<int>
        Assert.NotNull(deserializedCommand.OrderStockItems);
        Assert.Equal(command.OrderStockItems.Count, deserializedCommand.OrderStockItems.Count);

        for (int i = 0; i < command.OrderStockItems.Count; i++)
        {
            Assert.Equal(command.OrderStockItems[i], deserializedCommand.OrderStockItems[i]);
        }
    }
}

