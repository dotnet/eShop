# Ordering.API

## Purpose
Order management API implementing CQRS pattern. Handles order creation, updates, and queries.

## Tech Stack
- ASP.NET Core Minimal APIs
- MediatR for CQRS
- Entity Framework Core
- RabbitMQ for integration events

## Architecture
Follows Clean Architecture with separate Domain and Infrastructure layers:
- `Ordering.Domain` - Domain entities and business logic
- `Ordering.Infrastructure` - Data access and external services

## Key Features
- Create new orders
- Get order details and history
- Cancel orders
- Order status management

## API Endpoints
- `GET /api/v1/orders` - Get user's orders
- `GET /api/v1/orders/{orderId}` - Get order details
- `POST /api/v1/orders` - Create order
- `PUT /api/v1/orders/cancel` - Cancel order

## Database
- Uses `orderingdb` PostgreSQL database

## CQRS Commands/Queries
- `CreateOrderCommand`
- `CancelOrderCommand`
- `GetOrderByIdQuery`
- `GetOrdersFromUserQuery`
