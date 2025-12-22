# Ordering.Domain

## Purpose
Domain layer containing business entities, value objects, and domain events following Domain-Driven Design principles.

## Key Concepts
- **Aggregate Roots**: Order, Buyer
- **Value Objects**: Address, PaymentMethod
- **Domain Events**: OrderStartedDomainEvent, OrderStatusChangedDomainEvent

## Entities
- `Order` - Main order aggregate with order items
- `OrderItem` - Line items in an order
- `Buyer` - Customer making the purchase
- `PaymentMethod` - Payment information

## Order States
1. Submitted
2. AwaitingValidation
3. StockConfirmed
4. Paid
5. Shipped
6. Cancelled

## Patterns Used
- Aggregate Root pattern
- Value Object pattern
- Domain Events
- Entity base class with ID
