@catalog @critical
Feature: Product Catalog Browsing
  As a customer
  I want to browse available products
  So that I can find items I want to purchase

  Background:
    Given the eShop web application is available
    And the catalog service is running

  @happy-path @smoke
  Scenario: Successfully view product catalog homepage
    Given I am on the eShop homepage
    Then I should see the heading "Ready for a new adventure?"
    And I should see available products displayed

  @happy-path
  Scenario: View product details
    Given I am on the eShop homepage
    When I click on the "Adventurer GPS Watch" product
    Then I should see the product detail page
    And the product name should be "Adventurer GPS Watch"
    And I should see the "Add to shopping bag" button

  @happy-path
  Scenario: Browse multiple products
    Given I am on the eShop homepage
    When I view the available products
    Then I should see at least 3 products displayed
    And each product should have a name
    And each product should have a price
    And each product should have an image

  @performance
  Scenario: Catalog page loads within acceptable time
    Given I am on the eShop homepage
    Then the page should load within 3 seconds
    And all product images should be visible
