package com.eshop.integrationevents;

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonProperty;

import java.time.Instant;
import java.util.Objects;
import java.util.UUID;

/**
 * Base envelope shared with .NET integration events.
 */
public class IntegrationEvent {
    private final UUID id;
    private final Instant creationDate;

    public IntegrationEvent() {
        this(UUID.randomUUID(), Instant.now());
    }

    @JsonCreator
    public IntegrationEvent(
            @JsonProperty("Id") UUID id,
            @JsonProperty("CreationDate") Instant creationDate) {
        this.id = Objects.requireNonNull(id, "id");
        this.creationDate = Objects.requireNonNull(creationDate, "creationDate");
    }

    @JsonProperty("Id")
    public UUID id() {
        return id;
    }

    @JsonProperty("CreationDate")
    @JsonFormat(shape = JsonFormat.Shape.STRING)
    public Instant creationDate() {
        return creationDate;
    }
}
