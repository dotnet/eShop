package com.eshop.defaults;

import org.springframework.boot.autoconfigure.AutoConfiguration;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.boot.autoconfigure.condition.ConditionalOnMissingBean;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.boot.autoconfigure.condition.ConditionalOnWebApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationConverter;
import org.springframework.security.web.SecurityFilterChain;

@AutoConfiguration
@ConditionalOnClass(SecurityFilterChain.class)
public class SecurityDefaultsAutoConfiguration {

    @Bean
    @ConditionalOnMissingBean
    JwtAuthenticationConverter eshopJwtAuthenticationConverter() {
        return JwtConventions.authenticationConverter();
    }

    @Bean
    @ConditionalOnMissingBean(SecurityFilterChain.class)
    @ConditionalOnWebApplication(type = ConditionalOnWebApplication.Type.SERVLET)
    @ConditionalOnProperty("spring.security.oauth2.resourceserver.jwt.issuer-uri")
    SecurityFilterChain eshopSecurityFilterChain(
            HttpSecurity http, JwtAuthenticationConverter converter) throws Exception {
        return http
                .authorizeHttpRequests(authorize -> authorize
                        .requestMatchers("/health", "/alive").permitAll()
                        .anyRequest().authenticated())
                .oauth2ResourceServer(resourceServer ->
                        resourceServer.jwt(jwt -> jwt.jwtAuthenticationConverter(converter)))
                .build();
    }
}
