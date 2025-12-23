namespace eShop.Shipping.Domain.AggregatesModel.ShipmentAggregate;

public interface IShipmentRepository : IRepository<Shipment>
{
    Shipment Add(Shipment shipment);
    void Update(Shipment shipment);
    Task<Shipment?> GetAsync(int shipmentId);
    Task<Shipment?> GetByOrderIdAsync(int orderId);
    Task<List<Shipment>> GetByShipperIdAsync(int shipperId);
    Task<List<Shipment>> GetByStatusAsync(ShipmentStatus status);
    Task<List<Shipment>> GetAvailableAsync();
    Task<List<Shipment>> GetAllAsync();
}
