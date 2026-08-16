package com.eshop.integrationevents;

import com.fasterxml.jackson.annotation.JsonProperty;

import java.math.BigDecimal;
import java.util.Objects;

public record BasketItem(
        @JsonProperty("Id") String id,
        @JsonProperty("ProductId") int productId,
        @JsonProperty("ProductName") String productName,
        @JsonProperty("UnitPrice") BigDecimal unitPrice,
        @JsonProperty("OldUnitPrice") BigDecimal oldUnitPrice,
        @JsonProperty("Quantity") int quantity,
        @JsonProperty("PictureUrl") String pictureUrl) {

    public BasketItem {
        Objects.requireNonNull(id, "id");
        Objects.requireNonNull(productName, "productName");
        Objects.requireNonNull(unitPrice, "unitPrice");
        Objects.requireNonNull(oldUnitPrice, "oldUnitPrice");
        if (quantity < 0) {
            throw new IllegalArgumentException("quantity must not be negative");
        }
    }
}
