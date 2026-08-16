package com.eshop.defaults;

import org.junit.jupiter.api.Test;
import org.springframework.core.env.MapPropertySource;
import org.springframework.core.env.StandardEnvironment;

import java.util.Map;

import static org.junit.jupiter.api.Assertions.assertEquals;

class AspireEnvironmentPostProcessorTest {

    @Test
    void mapsAspireConnectionStringsAndIdentityUrl() {
        StandardEnvironment environment = new StandardEnvironment();
        environment.getPropertySources().addFirst(new MapPropertySource("testEnvironment", Map.of(
                "ConnectionStrings__eventbus", "amqp://guest:guest@rabbit:5672",
                "ConnectionStrings__redis", "redis://redis:6379",
                "ConnectionStrings__catalogdb", "Host=postgres;Database=catalogdb",
                "ConnectionStrings__orderingdb", "Host=postgres;Database=orderingdb",
                "ConnectionStrings__webhooksdb", "Host=postgres;Database=webhooksdb",
                "Identity__Url", "https://identity.test",
                "OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318")));

        new AspireEnvironmentPostProcessor().postProcessEnvironment(environment, null);

        assertEquals("amqp://guest:guest@rabbit:5672",
                environment.getProperty("spring.rabbitmq.addresses"));
        assertEquals("redis://redis:6379", environment.getProperty("spring.data.redis.url"));
        assertEquals("Host=postgres;Database=catalogdb",
                environment.getProperty("eshop.connection-strings.catalogdb"));
        assertEquals("Host=postgres;Database=orderingdb",
                environment.getProperty("eshop.connection-strings.orderingdb"));
        assertEquals("Host=postgres;Database=webhooksdb",
                environment.getProperty("eshop.connection-strings.webhooksdb"));
        assertEquals("https://identity.test",
                environment.getProperty("spring.security.oauth2.resourceserver.jwt.issuer-uri"));
        assertEquals("manual",
                environment.getProperty("spring.rabbitmq.listener.simple.acknowledge-mode"));
    }

    @Test
    void disablesOtlpExportWhenAspireDoesNotProvideAnEndpoint() {
        StandardEnvironment environment = new StandardEnvironment();

        new AspireEnvironmentPostProcessor().postProcessEnvironment(environment, null);

        assertEquals("false", environment.getProperty("management.tracing.export.otlp.enabled"));
        assertEquals("false", environment.getProperty("management.otlp.metrics.export.enabled"));
    }
}
