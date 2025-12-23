namespace eShop.Shipping.Infrastructure.Repositories;

public class ShipperRepository : IShipperRepository
{
    private readonly ShippingContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public ShipperRepository(ShippingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Shipper Add(Shipper shipper)
    {
        return _context.Shippers.Add(shipper).Entity;
    }

    public void Update(Shipper shipper)
    {
        _context.Entry(shipper).State = EntityState.Modified;
    }

    public void Delete(Shipper shipper)
    {
        _context.Shippers.Remove(shipper);
    }

    public async Task<Shipper?> GetAsync(int shipperId)
    {
        return await _context.Shippers.FindAsync(shipperId);
    }

    public async Task<Shipper?> GetByUserIdAsync(string userId)
    {
        return await _context.Shippers
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<List<Shipper>> GetAllAsync()
    {
        return await _context.Shippers
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Shipper>> GetAvailableAsync()
    {
        return await _context.Shippers
            .Where(s => s.IsActive && s.IsAvailable)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Shipper?> GetNearestAvailableAsync(int warehouseId)
    {
        // For demo purposes, we just get any available shipper at the specified warehouse
        // In a real implementation, you would calculate distance
        var shipper = await _context.Shippers
            .Where(s => s.IsActive && s.IsAvailable && s.CurrentWarehouseId == warehouseId)
            .FirstOrDefaultAsync();

        // If no shipper at that warehouse, get any available shipper
        if (shipper == null)
        {
            shipper = await _context.Shippers
                .Where(s => s.IsActive && s.IsAvailable)
                .FirstOrDefaultAsync();
        }

        return shipper;
    }
}
