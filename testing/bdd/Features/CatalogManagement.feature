@catalog-management @allure.label.epic:Catalog_Management @allure.label.owner:eShop_Team
Feature: Product Catalog Management
  As an eShop customer
  I want to browse and search the product catalog
  So that I can find and purchase outdoor gear

  Background:
    Given the catalog service is available
    And test data is seeded in the system

  @happy-path @regression @allure.label.severity:critical @allure.label.story:Product_Browsing
  Scenario: Browse products in catalog
    Given the catalog contains outdoor gear products
    When I request the product catalog with page size 10
    Then I should receive a list of products
    And the response should contain valid product data
    And each product should have a name, price, and description
    And the response execution time should be acceptable

  @happy-path @allure.label.severity:critical @allure.label.story:Product_Details
  Scenario: View product details
    Given a product exists with ID 1
    When I request the product details for ID 1
    Then I should receive the product information
    And the product should have complete details
    And the product should have stock information
    And the response should indicate success

  @search-functionality @allure.label.severity:major @allure.label.story:Product_Search
  Scenario Outline: Search products by name
    Given the catalog contains products with various names
    When I search for products with name "<searchTerm>"
    Then I should receive products matching the search criteria
    And all returned products should contain "<searchTerm>" in their name
    And the search results should be relevant

    Examples:
      | searchTerm |
      | Jacket     |
      | Boot       |
      | Backpack   |
      | Tent       |

  @filtering @allure.label.severity:major @allure.label.story:Product_Filtering
  Scenario: Filter products by brand
    Given the catalog contains products from multiple brands
    When I filter products by brand "AdventureWorks"
    Then I should receive only products from "AdventureWorks" brand
    And the filtered results should maintain proper pagination
    And the response should indicate success

  @filtering @allure.label.severity:major @allure.label.story:Product_Filtering
  Scenario: Filter products by category
    Given the catalog contains products in multiple categories
    When I filter products by category "Clothing"
    Then I should receive only products in "Clothing" category
    And the filtered results should be properly categorized
    And the response should indicate success

  @pagination @allure.label.severity:normal @allure.label.story:Product_Pagination
  Scenario: Navigate through product pages
    Given the catalog contains more than 20 products
    When I request page 2 with page size 10
    Then I should receive the second set of 10 products
    And the pagination information should be correct
    And the products should be different from page 1

  @error-handling @allure.label.severity:major @allure.label.story:Error_Handling
  Scenario: Request non-existent product
    When I request product details for ID 99999
    Then I should receive a not found response
    And the error message should be descriptive
    And the response should maintain proper error format

  @performance @allure.label.severity:normal @allure.label.story:Performance
  Scenario: Catalog response time validation
    Given the catalog service is under normal load
    When I request the product catalog
    Then the response time should be less than 2 seconds
    And the response should be properly formatted
    And all required fields should be present