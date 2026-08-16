package com.eshop.integrationevents;

import com.fasterxml.jackson.annotation.JsonProperty;

import java.util.List;
import java.util.Objects;

public record CustomerBasket(
        @JsonProperty("BuyerId") String buyerId,
        @JsonProperty("Items") List<BasketItem> items) {

    public CustomerBasket {
        Objects.requireNonNull(buyerId, "buyerId");
        items = List.copyOf(Objects.requireNonNull(items, "items"));
    }
}
