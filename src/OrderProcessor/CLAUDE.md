# OrderProcessor

## Overview
The OrderProcessor is a background worker service that handles order grace period management. It monitors submitted orders and publishes events when the grace period expires, allowing the order processing workflow to continue.

## Technology Stack
- **Framework**: .NET 10 Worker Service
- **Database**: PostgreSQL (read-only connection to ordering database)
- **Messaging**: RabbitMQ via EventBus

## Purpose
When an order is created, it enters a "grace period" during which the customer can cancel it. The OrderProcessor:
1. Polls the database for orders that have exceeded their grace period
2. Publishes `GracePeriodConfirmedIntegrationEvent` for each expired order
3. This triggers the Ordering.API to move the order to the next status

## Key Components

### Background Service
- `GracePeriodManagerService` - Hosted service that runs on a timer
- Queries orders with `Submitted` status older than grace period threshold
- Publishes integration events for expired orders

### Configuration
- `GracePeriodTime` - Duration of grace period (default: configurable)
- `CheckUpdateTime` - Polling interval for checking orders

## Event Integration

### Subscribed Events
None - OrderProcessor only publishes events.

### Published Events
| Event | Trigger |
|-------|---------|
| `GracePeriodConfirmedIntegrationEvent` | When order grace period expires |

## Project Structure
```
OrderProcessor/
├── IntegrationEvents/
│   └── Events/
│       └── GracePeriodConfirmedIntegrationEvent.cs
├── Extensions.cs            # Service configuration
├── Program.cs               # Application entry point
└── appsettings.json         # Configuration
```

## Workflow
```
1. Order created in Ordering.API (status: Submitted)
2. OrderProcessor polls database periodically
3. Finds orders where: CreatedDate + GracePeriod < Now
4. Publishes GracePeriodConfirmedIntegrationEvent(orderId)
5. Ordering.API receives event
6. Order status changes to AwaitingValidation
7. Order processing continues...
```

## Database Access
- Read-only connection to ordering database
- Queries `orders` table for submitted orders
- Uses direct SQL queries (not EF Core)

## Dependencies
- **PostgreSQL**: Read-only access to ordering database
- **RabbitMQ**: For publishing integration events
- **eShop.ServiceDefaults**: Shared service configuration

## Configuration
```json
{
  "GracePeriodTime": "00:01:00",  // 1 minute grace period
  "CheckUpdateTime": "00:00:15"   // Check every 15 seconds
}
```

## Running Locally
This service is orchestrated by `eShop.AppHost` via .NET Aspire. Run the AppHost project to start all services with proper configuration.

## Notes
- This is a stateless worker that can be scaled horizontally
- Uses optimistic concurrency to handle multiple instances
- Critical for order workflow progression
