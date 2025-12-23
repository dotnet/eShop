namespace eShop.Shipping.API.Apis;

public static class ShipmentApi
{
    public static RouteGroupBuilder MapShipmentApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/shipments").HasApiVersion(1.0);

        // Admin endpoints - Shipment management
        api.MapGet("/", GetAllShipments)
            .WithName("GetAllShipments")
            .WithSummary("Get all shipments")
            .WithDescription("Returns a list of all shipments");

        api.MapGet("/{id:int}", GetShipmentById)
            .WithName("GetShipmentById")
            .WithSummary("Get shipment by ID")
            .WithDescription("Returns a single shipment by its ID");

        api.MapGet("/order/{orderId:int}", GetShipmentByOrderId)
            .WithName("GetShipmentByOrderId")
            .WithSummary("Get shipment by order ID")
            .WithDescription("Returns a shipment for the specified order");

        api.MapGet("/status/{status}", GetShipmentsByStatus)
            .WithName("GetShipmentsByStatus")
            .WithSummary("Get shipments by status")
            .WithDescription("Returns all shipments with the specified status");

        api.MapGet("/{id:int}/route", GetShipmentRoute)
            .WithName("GetShipmentRoute")
            .WithSummary("Get shipment route")
            .WithDescription("Returns the route waypoints for a shipment");

        api.MapGet("/{id:int}/history", GetShipmentStatusHistory)
            .WithName("GetShipmentStatusHistory")
            .WithSummary("Get shipment status history")
            .WithDescription("Returns the status history for a shipment");

        api.MapPost("/{id:int}/assign-shipper", AssignShipper)
            .WithName("AssignShipperToShipment")
            .WithSummary("Assign shipper to shipment")
            .WithDescription("Assigns a shipper to a shipment");

        api.MapPost("/{id:int}/cancel", CancelShipment)
            .WithName("CancelShipment")
            .WithSummary("Cancel shipment")
            .WithDescription("Cancels a shipment");

        return api;
    }

    public static async Task<Ok<List<ShipmentDto>>> GetAllShipments(
        IShipmentRepository repository)
    {
        var shipments = await repository.GetAllAsync();
        var dtos = shipments.Select(ToDto).ToList();
        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<ShipmentDto>, NotFound>> GetShipmentById(
        int id,
        IShipmentRepository repository)
    {
        var shipment = await repository.GetAsync(id);
        if (shipment == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(ToDto(shipment));
    }

    public static async Task<Results<Ok<ShipmentDto>, NotFound>> GetShipmentByOrderId(
        int orderId,
        IShipmentRepository repository)
    {
        var shipment = await repository.GetByOrderIdAsync(orderId);
        if (shipment == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(ToDto(shipment));
    }

    public static async Task<Ok<List<ShipmentDto>>> GetShipmentsByStatus(
        string status,
        IShipmentRepository repository)
    {
        if (!Enum.TryParse<ShipmentStatus>(status, true, out var shipmentStatus))
        {
            return TypedResults.Ok(new List<ShipmentDto>());
        }

        var shipments = await repository.GetByStatusAsync(shipmentStatus);
        var dtos = shipments.Select(ToDto).ToList();
        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<List<ShipmentWaypointDto>>, NotFound>> GetShipmentRoute(
        int id,
        IShipmentRepository repository)
    {
        var shipment = await repository.GetAsync(id);
        if (shipment == null)
        {
            return TypedResults.NotFound();
        }

        var dtos = shipment.Waypoints
            .OrderBy(w => w.Sequence)
            .Select(ToWaypointDto)
            .ToList();

        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<List<ShipmentStatusHistoryDto>>, NotFound>> GetShipmentStatusHistory(
        int id,
        IShipmentRepository repository)
    {
        var shipment = await repository.GetAsync(id);
        if (shipment == null)
        {
            return TypedResults.NotFound();
        }

        var dtos = shipment.StatusHistory
            .OrderByDescending(h => h.Timestamp)
            .Select(ToStatusHistoryDto)
            .ToList();

        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<ShipmentDto>, NotFound, BadRequest<string>>> AssignShipper(
        int id,
        AssignShipperRequest request,
        IShipmentRepository shipmentRepository,
        IShipperRepository shipperRepository)
    {
        var shipment = await shipmentRepository.GetAsync(id);
        if (shipment == null)
        {
            return TypedResults.NotFound();
        }

        var shipper = await shipperRepository.GetAsync(request.ShipperId);
        if (shipper == null)
        {
            return TypedResults.BadRequest("Shipper not found");
        }

        try
        {
            // Free up previous shipper if exists
            if (shipment.ShipperId.HasValue)
            {
                var previousShipper = await shipperRepository.GetAsync(shipment.ShipperId.Value);
                if (previousShipper != null)
                {
                    previousShipper.SetAvailable();
                    shipperRepository.Update(previousShipper);
                }
            }

            shipment.AssignShipper(request.ShipperId);
            shipper.SetBusy();

            shipmentRepository.Update(shipment);
            shipperRepository.Update(shipper);
            await shipmentRepository.UnitOfWork.SaveEntitiesAsync();

            return TypedResults.Ok(ToDto(shipment));
        }
        catch (ShippingDomainException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    public static async Task<Results<Ok, NotFound, BadRequest<string>>> CancelShipment(
        int id,
        IShipmentRepository shipmentRepository,
        IShipperRepository shipperRepository,
        IEventBus eventBus)
    {
        var shipment = await shipmentRepository.GetAsync(id);
        if (shipment == null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var returnWarehouseId = shipment.GetLastWarehouseId();
            shipment.Cancel(returnWarehouseId);

            // Free up the shipper if assigned
            if (shipment.ShipperId.HasValue)
            {
                var shipper = await shipperRepository.GetAsync(shipment.ShipperId.Value);
                if (shipper != null)
                {
                    shipper.SetAvailable();
                    shipper.AssignToWarehouse(returnWarehouseId);
                    shipperRepository.Update(shipper);
                }
            }

            shipmentRepository.Update(shipment);
            await shipmentRepository.UnitOfWork.SaveEntitiesAsync();

            return TypedResults.Ok();
        }
        catch (ShippingDomainException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static ShipmentDto ToDto(Shipment shipment) => new(
        shipment.Id,
        shipment.OrderId,
        shipment.ShipperId,
        shipment.Status.ToString(),
        shipment.CustomerAddress,
        shipment.CustomerCity,
        shipment.CustomerCountry,
        shipment.CreatedAt,
        shipment.CompletedAt,
        shipment.Waypoints.Select(ToWaypointDto).ToList(),
        shipment.StatusHistory.OrderByDescending(h => h.Timestamp).Select(ToStatusHistoryDto).ToList());

    private static ShipmentWaypointDto ToWaypointDto(ShipmentWaypoint waypoint) => new(
        waypoint.Id,
        waypoint.WarehouseId,
        waypoint.WarehouseName,
        waypoint.Sequence,
        waypoint.ArrivedAt,
        waypoint.DepartedAt,
        waypoint.IsCompleted);

    private static ShipmentStatusHistoryDto ToStatusHistoryDto(ShipmentStatusHistory history) => new(
        history.Id,
        history.Status.ToString(),
        history.Timestamp,
        history.WaypointId,
        history.Notes);
}

// DTOs
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
    List<ShipmentWaypointDto> Waypoints,
    List<ShipmentStatusHistoryDto> StatusHistory);

public record ShipmentWaypointDto(
    int Id,
    int WarehouseId,
    string WarehouseName,
    int Sequence,
    DateTime? ArrivedAt,
    DateTime? DepartedAt,
    bool IsCompleted);

public record ShipmentStatusHistoryDto(
    int Id,
    string Status,
    DateTime Timestamp,
    int? WaypointId,
    string? Notes);

public record AssignShipperRequest(int ShipperId);
