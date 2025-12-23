namespace eShop.Warehouse.Domain.AggregatesModel.WarehouseAggregate;

public class Warehouse : Entity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string City { get; private set; }
    public string Country { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<WarehouseInventory> _inventory = new();
    public IReadOnlyCollection<WarehouseInventory> Inventory => _inventory.AsReadOnly();

    protected Warehouse()
    {
        Name = string.Empty;
        Address = string.Empty;
        City = string.Empty;
        Country = string.Empty;
    }

    public Warehouse(string name, string address, string city, string country, double latitude, double longitude)
        : this()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address ?? throw new ArgumentNullException(nameof(address));
        City = city ?? throw new ArgumentNullException(nameof(city));
        Country = country ?? throw new ArgumentNullException(nameof(country));
        Latitude = latitude;
        Longitude = longitude;
        IsActive = true;
    }

    public void Update(string name, string address, string city, string country, double latitude, double longitude)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address ?? throw new ArgumentNullException(nameof(address));
        City = city ?? throw new ArgumentNullException(nameof(city));
        Country = country ?? throw new ArgumentNullException(nameof(country));
        Latitude = latitude;
        Longitude = longitude;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void UpdateLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public WarehouseInventory AddInventory(int catalogItemId, int quantity)
    {
        var existingInventory = _inventory.SingleOrDefault(i => i.CatalogItemId == catalogItemId);

        if (existingInventory != null)
        {
            existingInventory.AddStock(quantity);
            return existingInventory;
        }

        var inventory = new WarehouseInventory(Id, catalogItemId, quantity);
        _inventory.Add(inventory);
        return inventory;
    }

    public void RemoveInventory(int catalogItemId, int quantity)
    {
        var inventory = _inventory.SingleOrDefault(i => i.CatalogItemId == catalogItemId);

        if (inventory == null)
        {
            throw new WarehouseDomainException($"Inventory for catalog item {catalogItemId} not found in warehouse {Name}");
        }

        inventory.RemoveStock(quantity);
    }

    public void SetInventory(int catalogItemId, int quantity)
    {
        var inventory = _inventory.SingleOrDefault(i => i.CatalogItemId == catalogItemId);

        if (inventory == null)
        {
            var newInventory = new WarehouseInventory(Id, catalogItemId, quantity);
            _inventory.Add(newInventory);
        }
        else
        {
            inventory.SetQuantity(quantity);
        }
    }
}
