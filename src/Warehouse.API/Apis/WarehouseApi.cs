using WarehouseEntity = eShop.Warehouse.Domain.AggregatesModel.WarehouseAggregate.Warehouse;

namespace eShop.Warehouse.API.Apis;

public static class WarehouseApi
{
    public static RouteGroupBuilder MapWarehouseApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/warehouse").HasApiVersion(1.0);

        // Warehouse endpoints
        api.MapGet("/", GetAllWarehouses)
            .WithName("GetAllWarehouses")
            .WithSummary("Get all warehouses")
            .WithDescription("Returns a list of all warehouses");

        api.MapGet("/{id:int}", GetWarehouseById)
            .WithName("GetWarehouseById")
            .WithSummary("Get warehouse by ID")
            .WithDescription("Returns a single warehouse by its ID");

        api.MapPost("/", CreateWarehouse)
            .WithName("CreateWarehouse")
            .WithSummary("Create a new warehouse")
            .WithDescription("Creates a new warehouse location");

        api.MapPut("/{id:int}", UpdateWarehouse)
            .WithName("UpdateWarehouse")
            .WithSummary("Update a warehouse")
            .WithDescription("Updates an existing warehouse");

        api.MapDelete("/{id:int}", DeleteWarehouse)
            .WithName("DeleteWarehouse")
            .WithSummary("Delete a warehouse")
            .WithDescription("Deletes a warehouse");

        api.MapPut("/{id:int}/activate", ActivateWarehouse)
            .WithName("ActivateWarehouse")
            .WithSummary("Activate a warehouse");

        api.MapPut("/{id:int}/deactivate", DeactivateWarehouse)
            .WithName("DeactivateWarehouse")
            .WithSummary("Deactivate a warehouse");

        // Inventory endpoints
        api.MapGet("/{warehouseId:int}/inventory", GetWarehouseInventory)
            .WithName("GetWarehouseInventory")
            .WithSummary("Get inventory for a warehouse")
            .WithDescription("Returns all inventory items in a specific warehouse");

        api.MapGet("/inventory/product/{catalogItemId:int}", GetProductInventory)
            .WithName("GetProductInventory")
            .WithSummary("Get product inventory across warehouses")
            .WithDescription("Returns inventory for a specific product across all warehouses");

        api.MapPut("/{warehouseId:int}/inventory/{catalogItemId:int}", SetInventory)
            .WithName("SetInventory")
            .WithSummary("Set inventory quantity")
            .WithDescription("Sets the inventory quantity for a product in a warehouse");

        api.MapPost("/{warehouseId:int}/inventory/{catalogItemId:int}/add", AddStock)
            .WithName("AddStock")
            .WithSummary("Add stock to inventory")
            .WithDescription("Adds stock to an existing inventory item");

        api.MapPost("/{warehouseId:int}/inventory/{catalogItemId:int}/remove", RemoveStock)
            .WithName("RemoveStock")
            .WithSummary("Remove stock from inventory")
            .WithDescription("Removes stock from an existing inventory item");

        return api;
    }

    // Warehouse handlers
    public static async Task<Ok<List<WarehouseDto>>> GetAllWarehouses(
        IWarehouseRepository repository)
    {
        var warehouses = await repository.GetAllAsync();
        var dtos = warehouses.Select(w => ToDto(w)).ToList();
        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<WarehouseDto>, NotFound>> GetWarehouseById(
        int id,
        IWarehouseRepository repository)
    {
        var warehouse = await repository.GetAsync(id);
        if (warehouse == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(ToDto(warehouse));
    }

    public static async Task<Results<Created<WarehouseDto>, BadRequest<string>>> CreateWarehouse(
        CreateWarehouseRequest request,
        IWarehouseRepository repository)
    {
        var warehouse = new WarehouseEntity(
            request.Name,
            request.Address,
            request.City,
            request.Country,
            request.Latitude,
            request.Longitude);

        repository.Add(warehouse);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.Created($"/api/warehouse/{warehouse.Id}", ToDto(warehouse));
    }

    public static async Task<Results<Ok<WarehouseDto>, NotFound, BadRequest<string>>> UpdateWarehouse(
        int id,
        UpdateWarehouseRequest request,
        IWarehouseRepository repository)
    {
        var warehouse = await repository.GetAsync(id);
        if (warehouse == null)
        {
            return TypedResults.NotFound();
        }

        warehouse.Update(
            request.Name,
            request.Address,
            request.City,
            request.Country,
            request.Latitude,
            request.Longitude);

        repository.Update(warehouse);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.Ok(ToDto(warehouse));
    }

    public static async Task<Results<NoContent, NotFound>> DeleteWarehouse(
        int id,
        IWarehouseRepository repository)
    {
        var warehouse = await repository.GetAsync(id);
        if (warehouse == null)
        {
            return TypedResults.NotFound();
        }

        repository.Delete(warehouse);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok, NotFound>> ActivateWarehouse(
        int id,
        IWarehouseRepository repository)
    {
        var warehouse = await repository.GetAsync(id);
        if (warehouse == null)
        {
            return TypedResults.NotFound();
        }

        warehouse.Activate();
        repository.Update(warehouse);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.Ok();
    }

    public static async Task<Results<Ok, NotFound>> DeactivateWarehouse(
        int id,
        IWarehouseRepository repository)
    {
        var warehouse = await repository.GetAsync(id);
        if (warehouse == null)
        {
            return TypedResults.NotFound();
        }

        warehouse.Deactivate();
        repository.Update(warehouse);
        await repository.UnitOfWork.SaveChangesAsync();

        return TypedResults.Ok();
    }

    // Inventory handlers
    public static async Task<Results<Ok<List<InventoryDto>>, NotFound>> GetWarehouseInventory(
        int warehouseId,
        IWarehouseRepository repository)
    {
        var warehouse = await repository.GetAsync(warehouseId);
        if (warehouse == null)
        {
            return TypedResults.NotFound();
        }

        var inventory = warehouse.Inventory.Select(i => ToInventoryDto(i, warehouse.Name)).ToList();
        return TypedResults.Ok(inventory);
    }

    public static async Task<Ok<List<InventoryDto>>> GetProductInventory(
        int catalogItemId,
        IWarehouseRepository repository)
    {
        var inventoryItems = await repository.GetInventoryByCatalogItemAsync(catalogItemId);
        var warehouses = await repository.GetAllAsync();

        var dtos = inventoryItems.Select(i =>
        {
            var warehouse = warehouses.FirstOrDefault(w => w.Id == i.WarehouseId);
            return ToInventoryDto(i, warehouse?.Name ?? "Unknown");
        }).ToList();

        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<InventoryDto>, NotFound, BadRequest<string>>> SetInventory(
        int warehouseId,
        int catalogItemId,
        SetInventoryRequest request,
        IWarehouseRepository repository)
    {
        var warehouse = await repository.GetAsync(warehouseId);
        if (warehouse == null)
        {
            return TypedResults.NotFound();
        }

        if (request.Quantity < 0)
        {
            return TypedResults.BadRequest("Quantity cannot be negative");
        }

        warehouse.SetInventory(catalogItemId, request.Quantity);
        repository.Update(warehouse);
        await repository.UnitOfWork.SaveChangesAsync();

        var inventory = warehouse.Inventory.First(i => i.CatalogItemId == catalogItemId);
        return TypedResults.Ok(ToInventoryDto(inventory, warehouse.Name));
    }

    public static async Task<Results<Ok<InventoryDto>, NotFound, BadRequest<string>>> AddStock(
        int warehouseId,
        int catalogItemId,
        StockAdjustmentRequest request,
        IWarehouseRepository repository)
    {
        var warehouse = await repository.GetAsync(warehouseId);
        if (warehouse == null)
        {
            return TypedResults.NotFound();
        }

        if (request.Amount <= 0)
        {
            return TypedResults.BadRequest("Amount must be positive");
        }

        try
        {
            var inventory = warehouse.AddInventory(catalogItemId, request.Amount);
            repository.Update(warehouse);
            await repository.UnitOfWork.SaveChangesAsync();

            return TypedResults.Ok(ToInventoryDto(inventory, warehouse.Name));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    public static async Task<Results<Ok<InventoryDto>, NotFound, BadRequest<string>>> RemoveStock(
        int warehouseId,
        int catalogItemId,
        StockAdjustmentRequest request,
        IWarehouseRepository repository)
    {
        var warehouse = await repository.GetAsync(warehouseId);
        if (warehouse == null)
        {
            return TypedResults.NotFound();
        }

        if (request.Amount <= 0)
        {
            return TypedResults.BadRequest("Amount must be positive");
        }

        try
        {
            warehouse.RemoveInventory(catalogItemId, request.Amount);
            repository.Update(warehouse);
            await repository.UnitOfWork.SaveChangesAsync();

            var inventory = warehouse.Inventory.First(i => i.CatalogItemId == catalogItemId);
            return TypedResults.Ok(ToInventoryDto(inventory, warehouse.Name));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    // Helper methods
    private static WarehouseDto ToDto(WarehouseEntity warehouse) => new(
        warehouse.Id,
        warehouse.Name,
        warehouse.Address,
        warehouse.City,
        warehouse.Country,
        warehouse.Latitude,
        warehouse.Longitude,
        warehouse.IsActive,
        warehouse.Inventory.Sum(i => i.Quantity));

    private static InventoryDto ToInventoryDto(WarehouseInventory inventory, string warehouseName) => new(
        inventory.Id,
        inventory.WarehouseId,
        warehouseName,
        inventory.CatalogItemId,
        inventory.Quantity,
        inventory.LastUpdated);
}

// DTOs
public record WarehouseDto(
    int Id,
    string Name,
    string Address,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    bool IsActive,
    int TotalStock);

public record InventoryDto(
    int Id,
    int WarehouseId,
    string WarehouseName,
    int CatalogItemId,
    int Quantity,
    DateTime LastUpdated);

public record CreateWarehouseRequest(
    string Name,
    string Address,
    string City,
    string Country,
    double Latitude,
    double Longitude);

public record UpdateWarehouseRequest(
    string Name,
    string Address,
    string City,
    string Country,
    double Latitude,
    double Longitude);

public record SetInventoryRequest(int Quantity);

public record StockAdjustmentRequest(int Amount);
