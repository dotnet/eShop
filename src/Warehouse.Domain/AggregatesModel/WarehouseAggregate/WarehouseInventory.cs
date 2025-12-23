namespace eShop.Warehouse.Domain.AggregatesModel.WarehouseAggregate;

public class WarehouseInventory : Entity
{
    public int WarehouseId { get; private set; }
    public int CatalogItemId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime LastUpdated { get; private set; }

    protected WarehouseInventory() { }

    public WarehouseInventory(int warehouseId, int catalogItemId, int quantity)
    {
        if (quantity < 0)
        {
            throw new WarehouseDomainException("Quantity cannot be negative");
        }

        WarehouseId = warehouseId;
        CatalogItemId = catalogItemId;
        Quantity = quantity;
        LastUpdated = DateTime.UtcNow;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity < 0)
        {
            throw new WarehouseDomainException("Quantity cannot be negative");
        }

        Quantity = quantity;
        LastUpdated = DateTime.UtcNow;
    }

    public void AddStock(int amount)
    {
        if (amount < 0)
        {
            throw new WarehouseDomainException("Amount to add cannot be negative");
        }

        Quantity += amount;
        LastUpdated = DateTime.UtcNow;
    }

    public void RemoveStock(int amount)
    {
        if (amount < 0)
        {
            throw new WarehouseDomainException("Amount to remove cannot be negative");
        }

        if (Quantity < amount)
        {
            throw new WarehouseDomainException($"Cannot remove {amount} items, only {Quantity} in stock");
        }

        Quantity -= amount;
        LastUpdated = DateTime.UtcNow;
    }
}
