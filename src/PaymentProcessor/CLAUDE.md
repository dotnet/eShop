# PaymentProcessor

## Purpose
Background worker service that simulates payment processing. In production, this would integrate with real payment gateways.

## Tech Stack
- .NET Worker Service (BackgroundService)
- RabbitMQ consumer/publisher

## Key Responsibilities
- Receive payment requests
- Simulate payment processing
- Publish payment result events

## Integration Events
- **Subscribes**: `OrderStatusChangedToStockConfirmedIntegrationEvent`
- **Publishes**: `OrderPaymentSucceededIntegrationEvent` or `OrderPaymentFailedIntegrationEvent`

## Note
This is a simulation for demo purposes. Replace with actual payment gateway integration (Stripe, PayPal, etc.) for production.
