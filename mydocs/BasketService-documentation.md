# BasketService Documentation

## Overview

`BasketService` is a gRPC service implementation that manages shopping basket operations in the eShop application. It inherits from `Basket.BasketBase` and provides three core operations: retrieving, updating, and deleting user baskets.

## Location

**File Path:** `/src/Basket.API/Grpc/BasketService.cs`

**Namespace:** `eShop.Basket.API.Grpc`

## Dependencies

The service relies on the following dependencies injected via constructor:

- **`IBasketRepository`**: Repository for basket data persistence operations
- **`ILogger<BasketService>`**: Logger for diagnostic and debugging information

## Class Structure

```csharp
public class BasketService(
    IBasketRepository repository,
    ILogger<BasketService> logger) : Basket.BasketBase
```

## Public Methods

### 1. GetBasket

**Signature:**
```csharp
[AllowAnonymous]
public override async Task<CustomerBasketResponse> GetBasket(
    GetBasketRequest request, 
    ServerCallContext context)
```

**Purpose:** Retrieves a customer's basket based on their user identity.

**Authentication:** Allows anonymous access (though returns empty basket if user is not authenticated).

**Behavior:**
- Extracts user identity from the gRPC server call context
- Returns empty basket if user identity is not available
- Logs debug information when debug logging is enabled
- Fetches basket data from repository using user ID
- Maps repository data to `CustomerBasketResponse` format
- Returns empty response if no basket exists

**Returns:** `CustomerBasketResponse` containing basket items or empty response

---

### 2. UpdateBasket

**Signature:**
```csharp
public override async Task<UpdateBasketResponse> UpdateBasket(
    UpdateBasketRequest request, 
    ServerCallContext context)
```

**Purpose:** Updates or creates a customer's basket with new items.

**Authentication:** Requires authenticated user (throws exception if not authenticated).

**Behavior:**
- Extracts and validates user identity from context
- Throws `RpcException` with `Unauthenticated` status if user is not authenticated
- Logs debug information about the update operation
- Converts request data to `CustomerBasket` model
- Updates basket in repository
- Throws `RpcException` with `NotFound` status if update fails
- Maps updated basket to response format

**Returns:** `CustomerBasketResponse` containing updated basket items

**Exceptions:**
- `RpcException(StatusCode.Unauthenticated)`: When user is not authenticated
- `RpcException(StatusCode.NotFound)`: When basket update fails

---

### 3. DeleteBasket

**Signature:**
```csharp
public override async Task<DeleteBasketResponse> DeleteBasket(
    DeleteBasketRequest request, 
    ServerCallContext context)
```

**Purpose:** Deletes a customer's basket.

**Authentication:** Requires authenticated user (throws exception if not authenticated).

**Behavior:**
- Extracts and validates user identity from context
- Throws `RpcException` with `Unauthenticated` status if user is not authenticated
- Deletes basket from repository using user ID
- Returns empty success response

**Returns:** `DeleteBasketResponse` (empty on success)

**Exceptions:**
- `RpcException(StatusCode.Unauthenticated)`: When user is not authenticated

---

## Private Helper Methods

### ThrowNotAuthenticated

```csharp
[DoesNotReturn]
private static void ThrowNotAuthenticated()
```

Throws a standardized `RpcException` indicating the caller is not authenticated.

### ThrowBasketDoesNotExist

```csharp
[DoesNotReturn]
private static void ThrowBasketDoesNotExist(string userId)
```

Throws a standardized `RpcException` indicating the basket for the given user ID does not exist.

### MapToCustomerBasketResponse

```csharp
private static CustomerBasketResponse MapToCustomerBasketResponse(
    CustomerBasket customerBasket)
```

Maps internal `CustomerBasket` model to gRPC `CustomerBasketResponse` message format.

### MapToCustomerBasket

```csharp
private static CustomerBasket MapToCustomerBasket(
    string userId, 
    UpdateBasketRequest customerBasketRequest)
```

Maps gRPC `UpdateBasketRequest` message to internal `CustomerBasket` model, associating it with the provided user ID.

---

## Data Models

### CustomerBasket (Internal Model)

Properties:
- `BuyerId`: User identifier
- `Items`: Collection of basket items

### BasketItem

Properties:
- `ProductId`: Identifier of the product
- `Quantity`: Quantity of the product in the basket

---

## Error Handling

The service uses gRPC status codes for error responses:

| Status Code | Scenario | Description |
|-------------|----------|-------------|
| `Unauthenticated` | User not authenticated | Thrown when operations requiring authentication are called without valid user identity |
| `NotFound` | Basket not found | Thrown when attempting to update a non-existent basket |

---

## Logging

The service implements debug-level logging for diagnostic purposes:

- Logs basket retrieval operations with method name and user ID
- Logs basket update operations with method name and user ID
- Only logs when `LogLevel.Debug` is enabled

---

## Security Considerations

1. **Authentication:** Most operations require authenticated users except `GetBasket` which allows anonymous access
2. **User Identity:** User identity is extracted from the gRPC server call context using `context.GetUserIdentity()`
3. **Authorization:** Service ensures users can only access their own baskets by deriving user ID from authentication context

---

## Usage Example

This is a gRPC service, typically called from client applications using generated gRPC client code:

```csharp
// Example client usage (pseudo-code)
var client = new Basket.BasketClient(channel);

// Get basket
var getResponse = await client.GetBasketAsync(new GetBasketRequest());

// Update basket
var updateRequest = new UpdateBasketRequest();
updateRequest.Items.Add(new BasketItem 
{ 
    ProductId = 123, 
    Quantity = 2 
});
var updateResponse = await client.UpdateBasketAsync(updateRequest);

// Delete basket
var deleteResponse = await client.DeleteBasketAsync(new DeleteBasketRequest());
```

---

## Related Components

- **Repository:** `IBasketRepository` - Handles data persistence
- **Extensions:** `context.GetUserIdentity()` - Extension method for extracting user identity
- **Models:** `CustomerBasket`, `BasketItem` - Domain models in `eShop.Basket.API.Model`

---

## Notes

- The service follows the gRPC service pattern with strongly-typed request/response messages
- All async operations use proper async/await patterns
- Defensive programming with null checks and empty responses
- Uses `[DoesNotReturn]` attribute for exception-throwing helper methods
