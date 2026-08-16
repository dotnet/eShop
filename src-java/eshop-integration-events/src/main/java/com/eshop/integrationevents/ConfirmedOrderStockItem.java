package com.eshop.integrationevents;

import com.fasterxml.jackson.annotation.JsonProperty;

public record ConfirmedOrderStockItem(
        @JsonProperty("ProductId") int productId,
        @JsonProperty("HasStock") boolean hasStock) {
}
