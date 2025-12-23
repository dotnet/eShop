namespace eShop.WebApp.Services;

public class ShippingService(HttpClient httpClient)
{
    private readonly string remoteServiceBaseUrl = "/api/shipments/";

    public Task<ShipmentDto?> GetShipmentByOrderIdAsync(int orderId)
    {
        return httpClient.GetFromJsonAsync<ShipmentDto>($"{remoteServiceBaseUrl}order/{orderId}");
    }
}

public record ShipmentDto(
    int Id,
    int OrderId,
    int? ShipperId,
    string Status,
    string CustomerAddress,
    string CustomerCity,
    string CustomerCountry,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    ShipmentWaypointDto[] Waypoints,
    ShipmentStatusHistoryDto[] StatusHistory);

public record ShipmentWaypointDto(
    int Id,
    int ShipmentId,
    int WarehouseId,
    string? WarehouseName,
    int Sequence,
    DateTime? ArrivedAt,
    DateTime? DepartedAt);

public record ShipmentStatusHistoryDto(
    int Id,
    int ShipmentId,
    string Status,
    DateTime Timestamp,
    int? WaypointId,
    string? Notes);
