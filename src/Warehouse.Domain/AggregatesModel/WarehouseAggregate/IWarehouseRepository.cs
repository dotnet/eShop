namespace eShop.Warehouse.Domain.AggregatesModel.WarehouseAggregate;

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Warehouse Add(Warehouse warehouse);
    void Update(Warehouse warehouse);
    void Delete(Warehouse warehouse);
    Task<Warehouse?> GetAsync(int warehouseId);
    Task<List<Warehouse>> GetAllAsync();
    Task<List<Warehouse>> GetActiveAsync();
    Task<WarehouseInventory?> GetInventoryAsync(int warehouseId, int catalogItemId);
    Task<List<WarehouseInventory>> GetInventoryByWarehouseAsync(int warehouseId);
    Task<List<WarehouseInventory>> GetInventoryByCatalogItemAsync(int catalogItemId);
    WarehouseInventory AddInventory(WarehouseInventory inventory);
    void UpdateInventory(WarehouseInventory inventory);
}
