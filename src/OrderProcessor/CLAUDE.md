# OrderProcessor

## Purpose
Background worker service that processes orders asynchronously. Handles order state transitions and integrates with payment processing.

## Tech Stack
- .NET Worker Service (BackgroundService)
- RabbitMQ consumer
- MediatR for command handling

## Key Responsibilities
- Process submitted orders
- Validate order stock
- Coordinate with PaymentProcessor
- Update order status

## Integration Events Handled
- `OrderStockConfirmedIntegrationEvent`
- `OrderPaymentSucceededIntegrationEvent`
- `OrderPaymentFailedIntegrationEvent`

## Flow
1. Receive order submitted event
2. Validate stock availability
3. Request payment processing
4. Update order to paid/cancelled status
