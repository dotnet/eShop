using WarehouseEntity = eShop.Warehouse.Domain.AggregatesModel.WarehouseAggregate.Warehouse;

namespace eShop.Warehouse.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly WarehouseContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public WarehouseRepository(WarehouseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public WarehouseEntity Add(WarehouseEntity warehouse)
    {
        return _context.Warehouses.Add(warehouse).Entity;
    }

    public void Update(WarehouseEntity warehouse)
    {
        _context.Entry(warehouse).State = EntityState.Modified;
    }

    public void Delete(WarehouseEntity warehouse)
    {
        _context.Warehouses.Remove(warehouse);
    }

    public async Task<WarehouseEntity?> GetAsync(int warehouseId)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.Inventory)
            .SingleOrDefaultAsync(w => w.Id == warehouseId);

        return warehouse;
    }

    public async Task<List<WarehouseEntity>> GetAllAsync()
    {
        return await _context.Warehouses
            .Include(w => w.Inventory)
            .ToListAsync();
    }

    public async Task<List<WarehouseEntity>> GetActiveAsync()
    {
        return await _context.Warehouses
            .Where(w => w.IsActive)
            .Include(w => w.Inventory)
            .ToListAsync();
    }

    public async Task<WarehouseInventory?> GetInventoryAsync(int warehouseId, int catalogItemId)
    {
        return await _context.WarehouseInventories
            .SingleOrDefaultAsync(i => i.WarehouseId == warehouseId && i.CatalogItemId == catalogItemId);
    }

    public async Task<List<WarehouseInventory>> GetInventoryByWarehouseAsync(int warehouseId)
    {
        return await _context.WarehouseInventories
            .Where(i => i.WarehouseId == warehouseId)
            .ToListAsync();
    }

    public async Task<List<WarehouseInventory>> GetInventoryByCatalogItemAsync(int catalogItemId)
    {
        return await _context.WarehouseInventories
            .Where(i => i.CatalogItemId == catalogItemId)
            .ToListAsync();
    }

    public WarehouseInventory AddInventory(WarehouseInventory inventory)
    {
        return _context.WarehouseInventories.Add(inventory).Entity;
    }

    public void UpdateInventory(WarehouseInventory inventory)
    {
        _context.Entry(inventory).State = EntityState.Modified;
    }
}
