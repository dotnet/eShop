# Webhooks.API

## Overview
The Webhooks API manages webhook subscriptions for external systems. It allows third-party applications to register for notifications about specific events in the eShop system.

## Technology Stack
- **Framework**: ASP.NET Core 10
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10
- **HTTP Client**: IHttpClientFactory

## Key Components

### API Endpoints
- `GET /api/v1/webhooks` - Get all webhook subscriptions
- `GET /api/v1/webhooks/{id}` - Get webhook by ID
- `POST /api/v1/webhooks` - Create webhook subscription
- `DELETE /api/v1/webhooks/{id}` - Delete webhook subscription

### Webhook Types
| Type | Event Trigger |
|------|---------------|
| `OrderShipped` | Order status changes to shipped |
| `OrderPaid` | Order status changes to paid |
| `ProductPriceChange` | Product price is updated |

### Models
- `WebhookSubscription` - Subscription entity with URL, type, secret
- `WebhookData` - Payload sent to webhook endpoints

## Event Integration

### Subscribed Events
| Event | Handler | Action |
|-------|---------|--------|
| `ProductPriceChangedIntegrationEvent` | Handler | Notifies subscribers of price changes |
| `OrderStatusChangedToShippedIntegrationEvent` | Handler | Notifies subscribers of shipped orders |
| `OrderStatusChangedToPaidIntegrationEvent` | Handler | Notifies subscribers of paid orders |

### Published Events
None - Webhooks.API is a consumer and notifier.

## Project Structure
```
Webhooks.API/
├── IntegrationEvents/
│   ├── Events/              # Subscribed event definitions
│   └── EventHandling/       # Event handlers
├── Model/                   # Domain models
├── Services/                # Webhook sending service
├── Infrastructure/          # EF Core context
├── Extensions.cs            # Service configuration
└── Program.cs               # Application entry point
```

## Webhook Payload
When an event occurs, the webhook service:
1. Queries matching subscriptions
2. Builds webhook payload with event data
3. Sends HTTP POST to subscription URL
4. Includes HMAC signature for verification

### Payload Example
```json
{
  "when": "2024-01-15T10:30:00Z",
  "type": "OrderShipped",
  "payload": {
    "orderId": 123,
    "orderStatus": "Shipped"
  }
}
```

## Security
- **HMAC Signature**: Each subscription has a secret key
- **Signature Header**: `X-eShop-Webhook-Signature`
- Subscribers should validate the signature

## Database Schema
| Table | Description |
|-------|-------------|
| `WebhookSubscriptions` | Subscription configurations |
| `IntegrationEventLog` | Outbox pattern event log |

## Dependencies
- **PostgreSQL**: Webhook subscription database
- **Identity.API**: For authentication
- **RabbitMQ**: For receiving integration events
- **HttpClient**: For sending webhook requests

## Configuration
Key settings:
- PostgreSQL connection (via Aspire)
- RabbitMQ connection for event bus
- HTTP client retry policies

## Running Locally
This service is orchestrated by `eShop.AppHost` via .NET Aspire. Run the AppHost project to start all services with proper configuration.

## Notes
- Implements retry logic for failed webhook deliveries
- Consider implementing exponential backoff
- Dead letter queue for persistently failing webhooks
