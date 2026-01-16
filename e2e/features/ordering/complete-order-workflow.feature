@ordering @critical @workflow
Feature: Complete Order Workflow
  As a customer
  I want to complete a purchase from cart to order confirmation
  So that I can receive the products I want

  Background:
    Given the eShop web application is available
    And the ordering service is running
    And the payment service is running
    And I am logged in as a customer

  @happy-path @end-to-end
  Scenario: Complete order from cart to confirmation
    Given I have the following items in my cart:
      | productName           | quantity |
      | Adventurer GPS Watch  | 1        |
    When I navigate to the shopping bag
    And I proceed to checkout
    And I confirm my shipping address
    And I confirm my payment method
    And I place the order
    Then I should see the order confirmation page
    And the order should have a unique order number
    And I should see the order summary with correct items
    And I should receive an order confirmation

  @happy-path
  Scenario: Order with multiple items
    Given I have the following items in my cart:
      | productName           | quantity |
      | Adventurer GPS Watch  | 2        |
    When I complete the checkout process
    Then the order should be created successfully
    And the order total should match the cart total
    And all items should be included in the order

  @error-handling
  Scenario: Handle payment failure gracefully
    Given I have items in my cart
    When I proceed to checkout
    And the payment authorization fails
    Then I should see a payment error message
    And the order should not be created
    And my cart should remain unchanged

  @error-handling
  Scenario: Validate required shipping information
    Given I have items in my cart
    When I proceed to checkout
    And I submit without a shipping address
    Then I should see a validation error
    And I should not be able to proceed

  @idempotency
  Scenario: Prevent duplicate order submission
    Given I have items in my cart
    When I complete the checkout process
    And I attempt to submit the order again
    Then only one order should be created
    And I should see the original order confirmation

  @performance
  Scenario: Order placement completes within acceptable time
    Given I have items in my cart
    When I complete the checkout process
    Then the order should be placed within 5 seconds
    And I should see the confirmation page
