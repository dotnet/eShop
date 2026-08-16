package com.eshop.defaults;

import org.springframework.security.oauth2.core.OAuth2TokenValidator;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.security.oauth2.jwt.JwtValidators;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationConverter;

public final class JwtConventions {
    private JwtConventions() {
    }

    public static JwtAuthenticationConverter authenticationConverter() {
        JwtAuthenticationConverter converter = new JwtAuthenticationConverter();
        converter.setPrincipalClaimName("sub");
        return converter;
    }

    /**
     * Validates standard timestamps and issuer. Audience is deliberately not validated,
     * matching the eShop service-defaults convention.
     */
    public static OAuth2TokenValidator<Jwt> issuerValidator(String issuer) {
        return JwtValidators.createDefaultWithIssuer(issuer);
    }
}
