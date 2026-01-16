@integration @cross-service @critical
Feature: Cross-Service Integration
  As a system
  I want all microservices to work together seamlessly
  So that customers have a smooth end-to-end experience

  Background:
    Given all eShop microservices are running
    And the message bus is operational

  @data-consistency @end-to-end
  Scenario: Complete purchase workflow across all services
    Given I am logged in as a customer
    When I browse the catalog and select "Adventurer GPS Watch"
    And I add the product to my basket
    And I proceed to checkout
    And I complete the payment
    Then the order should be created in the ordering service
    And the basket should be cleared
    And inventory should be updated in the catalog service
    And I should receive order confirmation
    And all services should use the same correlation ID

  @event-driven
  Scenario: Order events propagate correctly
    Given I am logged in as a customer
    And I have items in my cart
    When I complete an order
    Then an OrderStarted event should be published
    And the payment processor should receive the event
    And the order processor should receive the event
    And the order status should be updated accordingly

  @resilience
  Scenario: System handles service degradation gracefully
    Given the catalog service is running
    And I am on the eShop homepage
    When I browse products
    Then I should see the product catalog
    And the page should load successfully

  @data-consistency
  Scenario: Basket data syncs with Redis
    Given I am logged in as a customer
    When I add "Adventurer GPS Watch" to my cart
    And I refresh the page
    Then my cart should still contain "Adventurer GPS Watch"
    And the quantity should be preserved

  @idempotency
  Scenario: Duplicate order events are handled correctly
    Given I have completed an order with ID "ORD-12345"
    When the same order event is processed again
    Then only one order should exist in the system
    And no duplicate charges should occur
    And the system should log the duplicate event

  @performance
  Scenario: End-to-end workflow completes within SLA
    Given I am logged in as a customer
    When I complete a full purchase workflow
    Then the entire process should complete within 10 seconds
    And all service calls should succeed
    And no timeouts should occur
