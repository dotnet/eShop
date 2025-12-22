# IntegrationEventLogEF

## Purpose
Entity Framework Core implementation for storing integration events in the database, enabling the Outbox pattern for reliable event publishing.

## Key Components
- `IntegrationEventLogEntry` - Event log entity
- `IntegrationEventLogService` - Event persistence service
- `IntegrationEventLogContext` - DbContext for event log

## Pattern: Outbox
Stores events in the same transaction as business data, ensuring events are published only when the transaction commits.

## Usage
Used by services that need reliable event publishing with transactional guarantees.
