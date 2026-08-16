package com.eshop.testsupport;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.io.InputStream;

import static org.junit.jupiter.api.Assertions.assertEquals;

public final class GoldenJsonAssertions {
    private GoldenJsonAssertions() {
    }

    public static void assertMatches(
            ObjectMapper objectMapper, Object actual, Class<?> resourceOwner, String resourceName) {
        try (InputStream stream = resourceOwner.getResourceAsStream(resourceName)) {
            if (stream == null) {
                throw new IllegalArgumentException("Golden JSON resource not found: " + resourceName);
            }
            JsonNode expectedJson = objectMapper.readTree(stream);
            JsonNode actualJson = objectMapper.valueToTree(actual);
            assertEquals(expectedJson, actualJson);
        } catch (IOException exception) {
            throw new IllegalStateException("Unable to read golden JSON " + resourceName, exception);
        }
    }
}
