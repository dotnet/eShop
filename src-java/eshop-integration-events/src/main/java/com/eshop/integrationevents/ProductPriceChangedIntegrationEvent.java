package com.eshop.integrationevents;

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonProperty;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.Objects;
import java.util.UUID;

public final class ProductPriceChangedIntegrationEvent extends IntegrationEvent {
    private final int productId;
    private final BigDecimal newPrice;
    private final BigDecimal oldPrice;

    public ProductPriceChangedIntegrationEvent(int productId, BigDecimal newPrice, BigDecimal oldPrice) {
        super();
        this.productId = productId;
        this.newPrice = Objects.requireNonNull(newPrice, "newPrice");
        this.oldPrice = Objects.requireNonNull(oldPrice, "oldPrice");
    }

    @JsonCreator
    public ProductPriceChangedIntegrationEvent(
            @JsonProperty("Id") UUID id,
            @JsonProperty("CreationDate") Instant creationDate,
            @JsonProperty("ProductId") int productId,
            @JsonProperty("NewPrice") BigDecimal newPrice,
            @JsonProperty("OldPrice") BigDecimal oldPrice) {
        super(id, creationDate);
        this.productId = productId;
        this.newPrice = Objects.requireNonNull(newPrice, "newPrice");
        this.oldPrice = Objects.requireNonNull(oldPrice, "oldPrice");
    }

    @JsonProperty("ProductId")
    public int productId() {
        return productId;
    }

    @JsonProperty("NewPrice")
    public BigDecimal newPrice() {
        return newPrice;
    }

    @JsonProperty("OldPrice")
    public BigDecimal oldPrice() {
        return oldPrice;
    }
}
