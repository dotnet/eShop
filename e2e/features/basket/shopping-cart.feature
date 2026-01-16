@basket @critical @workflow
Feature: Shopping Cart Management
  As a customer
  I want to manage items in my shopping cart
  So that I can purchase the products I want

  Background:
    Given the eShop web application is available
    And the basket service is running
    And I am logged in as a customer

  @happy-path @end-to-end
  Scenario: Add product to shopping cart
    Given I am on the eShop homepage
    When I click on the "Adventurer GPS Watch" product
    And I click the "Add to shopping bag" button
    And I navigate to the shopping bag
    Then I should see the shopping bag page
    And the cart should contain 1 item
    And the item should be "Adventurer GPS Watch"
    And the total price should be displayed

  @happy-path
  Scenario: Add multiple products to cart
    Given I am on the eShop homepage
    When I add the following products to my cart:
      | productName           |
      | Adventurer GPS Watch  |
    And I navigate to the shopping bag
    Then the cart should contain 1 item
    And the total should reflect all items

  @happy-path
  Scenario: Update product quantity in cart
    Given I have "Adventurer GPS Watch" in my shopping cart
    When I navigate to the shopping bag
    And I update the quantity to 2
    Then the cart should show quantity 2
    And the total price should be updated accordingly

  @happy-path
  Scenario: Remove product from cart
    Given I have "Adventurer GPS Watch" in my shopping cart
    When I navigate to the shopping bag
    And I remove the item from the cart
    Then the cart should be empty
    And I should see an empty cart message

  @error-handling
  Scenario: Cart persists across sessions
    Given I have "Adventurer GPS Watch" in my shopping cart
    When I log out
    And I log back in
    And I navigate to the shopping bag
    Then the cart should still contain "Adventurer GPS Watch"

  @performance
  Scenario: Cart operations respond quickly
    Given I am on the eShop homepage
    When I add a product to my cart
    Then the operation should complete within 2 seconds
    And I should see a success indication
