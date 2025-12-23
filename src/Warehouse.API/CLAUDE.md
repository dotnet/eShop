# Warehouse.API - eShop Warehouse Management Service

## Overview
ASP.NET Core Minimal API for managing warehouse locations and inventory tracking.

## Tech Stack
- **ASP.NET Core Minimal APIs** - HTTP endpoints
- **Entity Framework Core** - Data access with PostgreSQL
- **.NET Aspire** - Service defaults and orchestration
- **Clean Architecture** - Domain/Infrastructure/API separation

## Project Structure
```
Warehouse.API/
├── Apis/                    # Minimal API endpoint definitions
├── Extensions/              # DI and service registration
├── Infrastructure/          # DB seeding
└── Program.cs               # Application entry point

Warehouse.Domain/
├── AggregatesModel/
│   └── WarehouseAggregate/  # Warehouse + WarehouseInventory entities
├── SeedWork/                # Base classes (Entity, IAggregateRoot)
└── Exceptions/              # Domain exceptions

Warehouse.Infrastructure/
├── EntityConfigurations/    # EF Core type configurations
├── Repositories/            # Repository implementations
├── Migrations/              # EF Core migrations
└── WarehouseContext.cs      # DbContext
```

## Domain Model
- **Warehouse** - Aggregate root with location info (name, address, coordinates)
- **WarehouseInventory** - Inventory items linked to catalog products

## API Endpoints
Base path: `/api/warehouse`

### Warehouse Management
- `GET /` - List all warehouses
- `GET /{id}` - Get warehouse by ID
- `POST /` - Create warehouse
- `PUT /{id}` - Update warehouse
- `DELETE /{id}` - Delete warehouse
- `PUT /{id}/activate` - Activate warehouse
- `PUT /{id}/deactivate` - Deactivate warehouse

### Inventory Management
- `GET /{warehouseId}/inventory` - Get warehouse inventory
- `GET /inventory/product/{catalogItemId}` - Get product stock across warehouses
- `PUT /{warehouseId}/inventory/{catalogItemId}` - Set inventory quantity
- `POST /{warehouseId}/inventory/{catalogItemId}/add` - Add stock
- `POST /{warehouseId}/inventory/{catalogItemId}/remove` - Remove stock

## Database
- Connection: `warehousedb` (PostgreSQL via Aspire)
- Tables: `Warehouses`, `WarehouseInventory`

## Authentication
Requires JWT authentication via Identity.API. All endpoints require authorization.

## Running Locally
```powershell
# Via Aspire host (recommended)
dotnet run --project src/eShop.AppHost

# Standalone (requires connection strings)
dotnet run --project src/Warehouse.API
```

## Adding Migrations
```powershell
cd src/Warehouse.Infrastructure
dotnet ef migrations add <MigrationName> -s ../Warehouse.API
```
