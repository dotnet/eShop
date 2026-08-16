package com.eshop.integrationevents;

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonProperty;

import java.time.Instant;
import java.util.List;
import java.util.Objects;
import java.util.UUID;

public final class OrderStartedIntegrationEvent extends IntegrationEvent {
    private final UUID orderId;
    private final String userId;
    private final String userName;
    private final List<OrderStockItem> orderStockItems;

    public OrderStartedIntegrationEvent(
            UUID orderId, String userId, String userName, List<OrderStockItem> orderStockItems) {
        super();
        this.orderId = Objects.requireNonNull(orderId, "orderId");
        this.userId = Objects.requireNonNull(userId, "userId");
        this.userName = Objects.requireNonNull(userName, "userName");
        this.orderStockItems = List.copyOf(Objects.requireNonNull(orderStockItems, "orderStockItems"));
    }

    @JsonCreator
    public OrderStartedIntegrationEvent(
            @JsonProperty("Id") UUID id,
            @JsonProperty("CreationDate") Instant creationDate,
            @JsonProperty("OrderId") UUID orderId,
            @JsonProperty("UserId") String userId,
            @JsonProperty("UserName") String userName,
            @JsonProperty("OrderStockItems") List<OrderStockItem> orderStockItems) {
        super(id, creationDate);
        this.orderId = Objects.requireNonNull(orderId, "orderId");
        this.userId = Objects.requireNonNull(userId, "userId");
        this.userName = Objects.requireNonNull(userName, "userName");
        this.orderStockItems = List.copyOf(Objects.requireNonNull(orderStockItems, "orderStockItems"));
    }

    @JsonProperty("OrderId")
    public UUID orderId() {
        return orderId;
    }

    @JsonProperty("UserId")
    public String userId() {
        return userId;
    }

    @JsonProperty("UserName")
    public String userName() {
        return userName;
    }

    @JsonProperty("OrderStockItems")
    public List<OrderStockItem> orderStockItems() {
        return orderStockItems;
    }
}
