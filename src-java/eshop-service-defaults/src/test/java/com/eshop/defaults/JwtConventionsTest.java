package com.eshop.defaults;

import org.junit.jupiter.api.Test;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationToken;

import java.time.Instant;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;

class JwtConventionsTest {

    @Test
    void usesSubjectAsPrincipalAndDoesNotValidateAudience() {
        Instant now = Instant.now();
        Jwt jwt = Jwt.withTokenValue("token")
                .header("alg", "none")
                .issuer("https://identity.test")
                .subject("customer-42")
                .audience(List.of("some-other-service"))
                .issuedAt(now.minusSeconds(5))
                .expiresAt(now.plusSeconds(300))
                .build();

        JwtAuthenticationToken authentication =
                (JwtAuthenticationToken) JwtConventions.authenticationConverter().convert(jwt);

        assertEquals("customer-42", authentication.getName());
        assertFalse(JwtConventions.issuerValidator("https://identity.test")
                .validate(jwt).hasErrors());
    }
}
