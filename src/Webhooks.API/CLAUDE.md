# Webhooks.API

## Purpose
Webhook management service allowing external systems to subscribe to eShop events.

## Tech Stack
- ASP.NET Core Minimal APIs
- PostgreSQL for subscription storage
- RabbitMQ for event consumption
- HttpClient for webhook delivery

## Key Features
- Webhook subscription management
- Event filtering by type
- Webhook delivery with retries
- Subscription validation

## API Endpoints
- `GET /api/v1/webhooks` - List subscriptions
- `POST /api/v1/webhooks` - Create subscription
- `DELETE /api/v1/webhooks/{id}` - Delete subscription

## Database
- Uses `webhooksdb` PostgreSQL database
- Stores webhook URLs, event types, and secrets
