@basket-management @allure.label.epic:Basket_Management @allure.label.owner:eShop_Team
Feature: Shopping Basket Management
  As an eShop customer
  I want to manage items in my shopping basket
  So that I can collect products before checkout

  Background:
    Given the basket service is available
    And I am authenticated as a customer
    And test data is seeded in the system

  @happy-path @regression @allure.label.severity:critical @allure.label.story:Basket_Operations
  Scenario: Add item to empty basket
    Given I have an empty basket
    When I add a product with ID 1 and quantity 2 to my basket
    Then my basket should contain 1 item
    And the item should have quantity 2
    And the basket total should reflect the item price
    And the response should indicate success

  @happy-path @allure.label.severity:critical @allure.label.story:Basket_Operations
  Scenario: Add multiple items to basket
    Given I have an empty basket
    When I add the following items to my basket:
      | ProductId | ProductName    | Quantity | UnitPrice |
      | 1         | Hiking Jacket  | 1        | 199.99    |
      | 2         | Hiking Boots   | 1        | 149.99    |
      | 3         | Backpack       | 2        | 89.99     |
    Then my basket should contain 3 different items
    And the basket total should be 529.96
    And each item should maintain its individual properties

  @basket-updates @allure.label.severity:major @allure.label.story:Basket_Updates
  Scenario: Update item quantity in basket
    Given I have a basket with the following items:
      | ProductId | ProductName   | Quantity | UnitPrice |
      | 1         | Hiking Jacket | 1        | 199.99    |
    When I update the quantity of product 1 to 3
    Then the item quantity should be updated to 3
    And the basket total should be 599.97
    And the response should indicate success

  @basket-updates @allure.label.severity:major @allure.label.story:Basket_Operations
  Scenario: Remove item from basket
    Given I have a basket with the following items:
      | ProductId | ProductName    | Quantity | UnitPrice |
      | 1         | Hiking Jacket  | 1        | 199.99    |
      | 2         | Hiking Boots   | 1        | 149.99    |
    When I remove product 1 from my basket
    Then my basket should contain 1 item
    And the remaining item should be product 2
    And the basket total should be 149.99

  @basket-validation @allure.label.severity:major @allure.label.story:Basket_Validation
  Scenario: Add item with invalid quantity
    Given I have an empty basket
    When I attempt to add a product with ID 1 and quantity 0
    Then I should receive a validation error
    And the error message should indicate invalid quantity
    And my basket should remain empty

  @basket-validation @allure.label.severity:major @allure.label.story:Basket_Validation
  Scenario: Add non-existent product to basket
    Given I have an empty basket
    When I attempt to add a product with ID 99999 and quantity 1
    Then I should receive a not found error
    And the error message should indicate product not found
    And my basket should remain empty

  @basket-persistence @allure.label.severity:normal @allure.label.story:Basket_Persistence
  Scenario: Basket persists across sessions
    Given I have a basket with items
    When I simulate a new session for the same user
    And I retrieve my basket
    Then my basket should contain the same items
    And the basket total should be preserved
    And all item details should be intact

  @basket-checkout @allure.label.severity:critical @allure.label.story:Basket_Checkout
  Scenario: Prepare basket for checkout
    Given I have a basket with valid items
    When I initiate the checkout process
    Then the basket should be validated for checkout
    And all items should have current pricing
    And stock availability should be confirmed
    And the checkout data should be properly formatted

  @error-handling @allure.label.severity:major @allure.label.story:Error_Handling
  Scenario: Handle concurrent basket modifications
    Given I have a basket with items
    When multiple operations attempt to modify the basket simultaneously
    Then the basket should maintain data consistency
    And conflicting operations should be handled gracefully
    And the final basket state should be valid

  @performance @allure.label.severity:normal @allure.label.story:Performance
  Scenario: Basket operations performance
    Given the basket service is under normal load
    When I perform basket operations
    Then each operation should complete within 1 second
    And the response should be properly formatted
    And all data should be accurate