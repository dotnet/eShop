package com.eshop.defaults;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.EnvironmentPostProcessor;
import org.springframework.core.env.ConfigurableEnvironment;
import org.springframework.core.env.MapPropertySource;

import java.util.LinkedHashMap;
import java.util.Map;

public final class AspireEnvironmentPostProcessor implements EnvironmentPostProcessor {
    static final String MAPPED_SOURCE = "eshopAspireMappings";
    static final String DEFAULTS_SOURCE = "eshopServiceDefaults";

    @Override
    public void postProcessEnvironment(ConfigurableEnvironment environment, SpringApplication application) {
        Map<String, Object> mapped = new LinkedHashMap<>();
        map(environment, mapped, "ConnectionStrings__eventbus", "spring.rabbitmq.addresses");
        map(environment, mapped, "ConnectionStrings__redis", "spring.data.redis.url");
        map(environment, mapped, "ConnectionStrings__catalogdb", "eshop.connection-strings.catalogdb");
        map(environment, mapped, "ConnectionStrings__orderingdb", "eshop.connection-strings.orderingdb");
        map(environment, mapped, "ConnectionStrings__webhooksdb", "eshop.connection-strings.webhooksdb");
        map(environment, mapped, "Identity__Url",
                "spring.security.oauth2.resourceserver.jwt.issuer-uri");
        if (!mapped.isEmpty()) {
            environment.getPropertySources().addFirst(new MapPropertySource(MAPPED_SOURCE, mapped));
        }

        Map<String, Object> defaults = new LinkedHashMap<>();
        defaults.put("spring.rabbitmq.listener.simple.acknowledge-mode", "manual");
        defaults.put("spring.rabbitmq.publisher-confirm-type", "correlated");
        defaults.put("spring.rabbitmq.publisher-returns", "true");
        defaults.put("management.endpoints.web.base-path", "/");
        defaults.put("management.endpoints.web.path-mapping.health", "health");
        defaults.put("management.endpoints.web.exposure.include", "health,info");
        defaults.put("management.endpoint.health.probes.enabled", "true");
        defaults.put("management.endpoint.health.group.liveness.additional-path", "server:/alive");
        defaults.put("management.tracing.sampling.probability", "1.0");
        if (!environment.containsProperty("OTEL_EXPORTER_OTLP_ENDPOINT")) {
            defaults.put("management.tracing.export.otlp.enabled", "false");
            defaults.put("management.otlp.metrics.export.enabled", "false");
        }
        environment.getPropertySources().addLast(new MapPropertySource(DEFAULTS_SOURCE, defaults));
    }

    private static void map(
            ConfigurableEnvironment environment,
            Map<String, Object> target,
            String environmentName,
            String springName) {
        String value = environment.getProperty(environmentName);
        if (value != null && !value.isBlank() && !environment.containsProperty(springName)) {
            target.put(springName, value);
        }
    }
}
