namespace eShop.Shipping.API.Apis;

public static class ShipperApi
{
    public static RouteGroupBuilder MapShipperApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/shippers").HasApiVersion(1.0);

        // Admin endpoints - Shipper management
        api.MapGet("/", GetAllShippers)
            .WithName("GetAllShippers")
            .WithSummary("Get all shippers");

        api.MapGet("/{id:int}", GetShipperById)
            .WithName("GetShipperById")
            .WithSummary("Get shipper by ID");

        api.MapPost("/", CreateShipper)
            .WithName("CreateShipper")
            .WithSummary("Create a new shipper");

        api.MapPut("/{id:int}", UpdateShipper)
            .WithName("UpdateShipper")
            .WithSummary("Update a shipper");

        api.MapDelete("/{id:int}", DeleteShipper)
            .WithName("DeleteShipper")
            .WithSummary("Delete a shipper");

        api.MapPut("/{id:int}/activate", ActivateShipper)
            .WithName("ActivateShipper")
            .WithSummary("Activate a shipper");

        api.MapPut("/{id:int}/deactivate", DeactivateShipper)
            .WithName("DeactivateShipper")
            .WithSummary("Deactivate a shipper");

        // Shipper self-service endpoints
        api.MapGet("/me", GetCurrentShipper)
            .WithName("GetCurrentShipper")
            .WithSummary("Get current shipper profile");

        api.MapGet("/me/assigned", GetMyAssignedShipments)
            .WithName("GetMyAssignedShipments")
            .WithSummary("Get shipments assigned to current shipper");

        api.MapGet("/me/available", GetAvailableShipments)
            .WithName("GetAvailableShipments")
            .WithSummary("Get available shipments to claim");

        api.MapGet("/me/history", GetMyCompletedShipments)
            .WithName("GetMyCompletedShipments")
            .WithSummary("Get completed shipments for current shipper");

        api.MapPost("/me/claim/{shipmentId:int}", ClaimShipment)
            .WithName("ClaimShipment")
            .WithSummary("Claim an available shipment");

        api.MapPut("/me/shipment/{shipmentId:int}/pickup", PickupFromWarehouse)
            .WithName("PickupFromWarehouse")
            .WithSummary("Mark shipment as picked up from warehouse");

        api.MapPut("/me/shipment/{shipmentId:int}/arrive/{waypointId:int}", ArriveAtWaypoint)
            .WithName("ArriveAtWaypoint")
            .WithSummary("Mark arrival at a waypoint");

        api.MapPut("/me/shipment/{shipmentId:int}/depart/{waypointId:int}", DepartFromWaypoint)
            .WithName("DepartFromWaypoint")
            .WithSummary("Mark departure from a waypoint");

        api.MapPut("/me/shipment/{shipmentId:int}/deliver", MarkDelivered)
            .WithName("MarkDelivered")
            .WithSummary("Mark shipment as delivered");

        return api;
    }

    // Admin endpoints
    public static async Task<Ok<List<ShipperDto>>> GetAllShippers(
        IShipperRepository repository)
    {
        var shippers = await repository.GetAllAsync();
        var dtos = shippers.Select(ToDto).ToList();
        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<ShipperDto>, NotFound>> GetShipperById(
        int id,
        IShipperRepository repository)
    {
        var shipper = await repository.GetAsync(id);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(ToDto(shipper));
    }

    public static async Task<Created<ShipperDto>> CreateShipper(
        CreateShipperRequest request,
        IShipperRepository repository)
    {
        var shipper = new Shipper(
            request.Name,
            request.Phone,
            request.UserId,
            request.CurrentWarehouseId);

        repository.Add(shipper);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.Created($"/api/shippers/{shipper.Id}", ToDto(shipper));
    }

    public static async Task<Results<Ok<ShipperDto>, NotFound>> UpdateShipper(
        int id,
        UpdateShipperRequest request,
        IShipperRepository repository)
    {
        var shipper = await repository.GetAsync(id);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        shipper.Update(request.Name, request.Phone);
        if (request.CurrentWarehouseId.HasValue)
        {
            shipper.AssignToWarehouse(request.CurrentWarehouseId.Value);
        }

        repository.Update(shipper);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.Ok(ToDto(shipper));
    }

    public static async Task<Results<NoContent, NotFound>> DeleteShipper(
        int id,
        IShipperRepository repository)
    {
        var shipper = await repository.GetAsync(id);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        repository.Delete(shipper);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok, NotFound>> ActivateShipper(
        int id,
        IShipperRepository repository)
    {
        var shipper = await repository.GetAsync(id);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        shipper.Activate();
        repository.Update(shipper);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.Ok();
    }

    public static async Task<Results<Ok, NotFound>> DeactivateShipper(
        int id,
        IShipperRepository repository)
    {
        var shipper = await repository.GetAsync(id);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        shipper.Deactivate();
        repository.Update(shipper);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.Ok();
    }

    // Shipper self-service endpoints
    public static async Task<Results<Ok<ShipperDto>, NotFound>> GetCurrentShipper(
        IIdentityService identityService,
        IShipperRepository repository)
    {
        var userId = identityService.GetUserIdentity();
        var shipper = await repository.GetByUserIdAsync(userId);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(ToDto(shipper));
    }

    public static async Task<Results<Ok<List<ShipmentDto>>, NotFound>> GetMyAssignedShipments(
        IIdentityService identityService,
        IShipperRepository shipperRepository,
        IShipmentRepository shipmentRepository)
    {
        var userId = identityService.GetUserIdentity();
        var shipper = await shipperRepository.GetByUserIdAsync(userId);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        var shipments = await shipmentRepository.GetByShipperIdAsync(shipper.Id);
        var activeShipments = shipments
            .Where(s => s.Status != ShipmentStatus.Delivered &&
                       s.Status != ShipmentStatus.Cancelled &&
                       s.Status != ShipmentStatus.ReturnedToWarehouse)
            .ToList();

        var dtos = activeShipments.Select(ToShipmentDto).ToList();
        return TypedResults.Ok(dtos);
    }

    public static async Task<Ok<List<ShipmentDto>>> GetAvailableShipments(
        IShipmentRepository repository)
    {
        var shipments = await repository.GetAvailableAsync();
        var dtos = shipments.Select(ToShipmentDto).ToList();
        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<List<ShipmentDto>>, NotFound>> GetMyCompletedShipments(
        IIdentityService identityService,
        IShipperRepository shipperRepository,
        IShipmentRepository shipmentRepository)
    {
        var userId = identityService.GetUserIdentity();
        var shipper = await shipperRepository.GetByUserIdAsync(userId);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        var shipments = await shipmentRepository.GetByShipperIdAsync(shipper.Id);
        var completedShipments = shipments
            .Where(s => s.Status == ShipmentStatus.Delivered ||
                       s.Status == ShipmentStatus.ReturnedToWarehouse)
            .ToList();

        var dtos = completedShipments.Select(ToShipmentDto).ToList();
        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<ShipmentDto>, NotFound, BadRequest<string>>> ClaimShipment(
        int shipmentId,
        IIdentityService identityService,
        IShipperRepository shipperRepository,
        IShipmentRepository shipmentRepository)
    {
        var userId = identityService.GetUserIdentity();
        var shipper = await shipperRepository.GetByUserIdAsync(userId);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        var shipment = await shipmentRepository.GetAsync(shipmentId);
        if (shipment == null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            shipment.AssignShipper(shipper.Id);
            shipper.SetBusy();

            shipmentRepository.Update(shipment);
            shipperRepository.Update(shipper);
            await shipmentRepository.UnitOfWork.SaveEntitiesAsync();

            return TypedResults.Ok(ToShipmentDto(shipment));
        }
        catch (ShippingDomainException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    public static async Task<Results<Ok<ShipmentDto>, NotFound, BadRequest<string>>> PickupFromWarehouse(
        int shipmentId,
        IIdentityService identityService,
        IShipperRepository shipperRepository,
        IShipmentRepository shipmentRepository)
    {
        var userId = identityService.GetUserIdentity();
        var shipper = await shipperRepository.GetByUserIdAsync(userId);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        var shipment = await shipmentRepository.GetAsync(shipmentId);
        if (shipment == null || shipment.ShipperId != shipper.Id)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var currentWaypoint = shipment.GetCurrentWaypoint();
            if (currentWaypoint == null)
            {
                return TypedResults.BadRequest("No waypoint available for pickup");
            }

            shipment.PickupFromWarehouse(currentWaypoint.Id);
            shipmentRepository.Update(shipment);
            await shipmentRepository.UnitOfWork.SaveEntitiesAsync();

            return TypedResults.Ok(ToShipmentDto(shipment));
        }
        catch (ShippingDomainException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    public static async Task<Results<Ok<ShipmentDto>, NotFound, BadRequest<string>>> ArriveAtWaypoint(
        int shipmentId,
        int waypointId,
        IIdentityService identityService,
        IShipperRepository shipperRepository,
        IShipmentRepository shipmentRepository)
    {
        var userId = identityService.GetUserIdentity();
        var shipper = await shipperRepository.GetByUserIdAsync(userId);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        var shipment = await shipmentRepository.GetAsync(shipmentId);
        if (shipment == null || shipment.ShipperId != shipper.Id)
        {
            return TypedResults.NotFound();
        }

        try
        {
            shipment.ArriveAtWarehouse(waypointId);
            shipmentRepository.Update(shipment);
            await shipmentRepository.UnitOfWork.SaveEntitiesAsync();

            return TypedResults.Ok(ToShipmentDto(shipment));
        }
        catch (ShippingDomainException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    public static async Task<Results<Ok<ShipmentDto>, NotFound, BadRequest<string>>> DepartFromWaypoint(
        int shipmentId,
        int waypointId,
        IIdentityService identityService,
        IShipperRepository shipperRepository,
        IShipmentRepository shipmentRepository)
    {
        var userId = identityService.GetUserIdentity();
        var shipper = await shipperRepository.GetByUserIdAsync(userId);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        var shipment = await shipmentRepository.GetAsync(shipmentId);
        if (shipment == null || shipment.ShipperId != shipper.Id)
        {
            return TypedResults.NotFound();
        }

        try
        {
            shipment.DepartFromWarehouse(waypointId);
            shipmentRepository.Update(shipment);
            await shipmentRepository.UnitOfWork.SaveEntitiesAsync();

            return TypedResults.Ok(ToShipmentDto(shipment));
        }
        catch (ShippingDomainException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    public static async Task<Results<Ok<ShipmentDto>, NotFound, BadRequest<string>>> MarkDelivered(
        int shipmentId,
        IIdentityService identityService,
        IShipperRepository shipperRepository,
        IShipmentRepository shipmentRepository,
        IEventBus eventBus)
    {
        var userId = identityService.GetUserIdentity();
        var shipper = await shipperRepository.GetByUserIdAsync(userId);
        if (shipper == null)
        {
            return TypedResults.NotFound();
        }

        var shipment = await shipmentRepository.GetAsync(shipmentId);
        if (shipment == null || shipment.ShipperId != shipper.Id)
        {
            return TypedResults.NotFound();
        }

        try
        {
            shipment.MarkDelivered();
            shipper.SetAvailable();

            shipmentRepository.Update(shipment);
            shipperRepository.Update(shipper);
            await shipmentRepository.UnitOfWork.SaveEntitiesAsync();

            // Publish completion event to trigger order shipped
            var completedEvent = new ShipmentCompletedIntegrationEvent
            {
                ShipmentId = shipment.Id,
                OrderId = shipment.OrderId,
                CompletedAt = shipment.CompletedAt ?? DateTime.UtcNow
            };
            await eventBus.PublishAsync(completedEvent);

            return TypedResults.Ok(ToShipmentDto(shipment));
        }
        catch (ShippingDomainException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static ShipperDto ToDto(Shipper shipper) => new(
        shipper.Id,
        shipper.Name,
        shipper.Phone,
        shipper.UserId,
        shipper.CurrentWarehouseId,
        shipper.IsAvailable,
        shipper.IsActive);

    private static ShipmentDto ToShipmentDto(Shipment shipment) => new(
        shipment.Id,
        shipment.OrderId,
        shipment.ShipperId,
        shipment.Status.ToString(),
        shipment.CustomerAddress,
        shipment.CustomerCity,
        shipment.CustomerCountry,
        shipment.CreatedAt,
        shipment.CompletedAt,
        shipment.Waypoints.OrderBy(w => w.Sequence).Select(w => new ShipmentWaypointDto(
            w.Id,
            w.WarehouseId,
            w.WarehouseName,
            w.Sequence,
            w.ArrivedAt,
            w.DepartedAt,
            w.IsCompleted)).ToList(),
        shipment.StatusHistory.OrderByDescending(h => h.Timestamp).Select(h => new ShipmentStatusHistoryDto(
            h.Id,
            h.Status.ToString(),
            h.Timestamp,
            h.WaypointId,
            h.Notes)).ToList());
}

// DTOs
public record ShipperDto(
    int Id,
    string Name,
    string Phone,
    string UserId,
    int? CurrentWarehouseId,
    bool IsAvailable,
    bool IsActive);

public record CreateShipperRequest(
    string Name,
    string Phone,
    string UserId,
    int? CurrentWarehouseId);

public record UpdateShipperRequest(
    string Name,
    string Phone,
    int? CurrentWarehouseId);
