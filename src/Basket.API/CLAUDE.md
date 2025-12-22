# Basket.API

## Purpose
Microservice managing shopping basket/cart functionality. Stores basket data in Redis for fast access.

## Tech Stack
- ASP.NET Core Minimal APIs
- Redis for basket storage
- gRPC for inter-service communication
- RabbitMQ for integration events

## Key Features
- Add/update/remove items from basket
- Get basket by customer ID
- Checkout basket (publishes integration event)
- Identity integration for user context

## API Endpoints
- `GET /api/v1/basket/{id}` - Get basket
- `POST /api/v1/basket` - Update basket
- `DELETE /api/v1/basket/{id}` - Delete basket
- `POST /api/v1/basket/checkout` - Checkout basket

## Dependencies
- Redis (via Aspire)
- RabbitMQ (EventBus)
- Identity.API (authentication)
