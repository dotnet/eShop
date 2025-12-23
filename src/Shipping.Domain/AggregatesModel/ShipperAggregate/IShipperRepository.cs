namespace eShop.Shipping.Domain.AggregatesModel.ShipperAggregate;

public interface IShipperRepository : IRepository<Shipper>
{
    Shipper Add(Shipper shipper);
    void Update(Shipper shipper);
    void Delete(Shipper shipper);
    Task<Shipper?> GetAsync(int shipperId);
    Task<Shipper?> GetByUserIdAsync(string userId);
    Task<List<Shipper>> GetAllAsync();
    Task<List<Shipper>> GetAvailableAsync();
    Task<Shipper?> GetNearestAvailableAsync(int warehouseId);
}
