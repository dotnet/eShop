package com.eshop.integrationevents;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class IntegrationContractJsonTest {
    private final ObjectMapper mapper = new ObjectMapper().registerModule(new JavaTimeModule());

    @Test
    void eventEnvelopeUsesPascalCaseAndPreservesDotNetValueSemantics() throws Exception {
        UUID id = UUID.fromString("8ac21d1c-dc44-45aa-b568-36f50c47f422");
        Instant created = Instant.parse("2026-08-16T08:00:00.123456Z");
        ProductPriceChangedIntegrationEvent event = new ProductPriceChangedIntegrationEvent(
                id, created, 42, new BigDecimal("12.3400"), new BigDecimal("10.25"));

        String json = mapper.writeValueAsString(event);
        JsonNode tree = mapper.readTree(json);

        assertEquals(id.toString(), tree.get("Id").textValue());
        assertEquals("2026-08-16T08:00:00.123456Z", tree.get("CreationDate").textValue());
        assertTrue(json.contains("\"NewPrice\":12.3400"));
        assertTrue(tree.has("ProductId"));
        assertFalse(tree.has("productId"));

        ProductPriceChangedIntegrationEvent roundTrip =
                mapper.readValue(json, ProductPriceChangedIntegrationEvent.class);
        assertEquals(id, roundTrip.id());
        assertEquals(created, roundTrip.creationDate());
        assertEquals(new BigDecimal("12.3400"), roundTrip.newPrice());
    }

    @Test
    void basketUsesPascalCaseAndRoundTripsExactPrices() throws Exception {
        CustomerBasket basket = new CustomerBasket("buyer-1", List.of(
                new BasketItem("line-1", 7, "Mug", new BigDecimal("9.990"),
                        new BigDecimal("12.00"), 2, "https://example.test/mug.png")));

        String json = mapper.writeValueAsString(basket);
        JsonNode tree = mapper.readTree(json);

        assertEquals("buyer-1", tree.get("BuyerId").textValue());
        assertTrue(json.contains("\"UnitPrice\":9.990"));
        assertFalse(tree.has("buyerId"));
        assertEquals(basket, mapper.readValue(json, CustomerBasket.class));
    }
}
