package com.eshop.integrationevents;

import com.fasterxml.jackson.annotation.JsonProperty;

public record OrderStockItem(
        @JsonProperty("ProductId") int productId,
        @JsonProperty("Units") int units) {

    public OrderStockItem {
        if (units < 0) {
            throw new IllegalArgumentException("units must not be negative");
        }
    }
}
