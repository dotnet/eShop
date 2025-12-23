namespace eShop.Shipping.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly ShippingContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public ShipmentRepository(ShippingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Shipment Add(Shipment shipment)
    {
        return _context.Shipments.Add(shipment).Entity;
    }

    public void Update(Shipment shipment)
    {
        _context.Entry(shipment).State = EntityState.Modified;
    }

    public async Task<Shipment?> GetAsync(int shipmentId)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Waypoints)
            .Include(s => s.StatusHistory)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        return shipment;
    }

    public async Task<Shipment?> GetByOrderIdAsync(int orderId)
    {
        return await _context.Shipments
            .Include(s => s.Waypoints)
            .Include(s => s.StatusHistory)
            .FirstOrDefaultAsync(s => s.OrderId == orderId);
    }

    public async Task<List<Shipment>> GetByShipperIdAsync(int shipperId)
    {
        return await _context.Shipments
            .Include(s => s.Waypoints)
            .Include(s => s.StatusHistory)
            .Where(s => s.ShipperId == shipperId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Shipment>> GetByStatusAsync(ShipmentStatus status)
    {
        return await _context.Shipments
            .Include(s => s.Waypoints)
            .Include(s => s.StatusHistory)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Shipment>> GetAvailableAsync()
    {
        return await _context.Shipments
            .Include(s => s.Waypoints)
            .Include(s => s.StatusHistory)
            .Where(s => s.ShipperId == null && s.Status == ShipmentStatus.Created)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Shipment>> GetAllAsync()
    {
        return await _context.Shipments
            .Include(s => s.Waypoints)
            .Include(s => s.StatusHistory)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }
}
