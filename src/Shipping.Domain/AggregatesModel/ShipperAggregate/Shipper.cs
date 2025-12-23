namespace eShop.Shipping.Domain.AggregatesModel.ShipperAggregate;

public class Shipper : Entity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public int? CurrentWarehouseId { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsActive { get; private set; }

    protected Shipper() { }

    public Shipper(string name, string phone, string userId, int? currentWarehouseId = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Phone = phone ?? throw new ArgumentNullException(nameof(phone));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        CurrentWarehouseId = currentWarehouseId;
        IsAvailable = true;
        IsActive = true;
    }

    public void Update(string name, string phone)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Phone = phone ?? throw new ArgumentNullException(nameof(phone));
    }

    public void AssignToWarehouse(int warehouseId)
    {
        CurrentWarehouseId = warehouseId;
    }

    public void SetAvailable()
    {
        if (!IsActive)
            throw new ShippingDomainException("Cannot set availability on inactive shipper");

        IsAvailable = true;
    }

    public void SetBusy()
    {
        IsAvailable = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsAvailable = false;
    }
}
