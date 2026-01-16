@identity @critical
Feature: User Authentication
  As a customer
  I want to securely log in and out of the application
  So that I can access my account and make purchases

  Background:
    Given the eShop web application is available
    And the identity service is running

  @happy-path @smoke
  Scenario: Successful user login
    Given I am on the eShop homepage
    When I click the "Sign in" button
    Then I should see the login page
    When I enter valid credentials
      | username | alice |
      | password | Pass123$ |
    And I click the "Login" button
    Then I should be redirected to the homepage
    And I should see my account name displayed
    And I should be authenticated

  @happy-path
  Scenario: Successful user logout
    Given I am logged in as a customer
    When I click the logout button
    Then I should be logged out
    And I should see the "Sign in" button
    And I should not have access to protected features

  @error-handling
  Scenario: Login with invalid credentials
    Given I am on the login page
    When I enter invalid credentials
      | username | invalid_user |
      | password | wrong_pass   |
    And I click the "Login" button
    Then I should see an error message
    And I should remain on the login page
    And I should not be authenticated

  @error-handling
  Scenario: Login with empty credentials
    Given I am on the login page
    When I submit the login form without credentials
    Then I should see validation errors
    And I should not be authenticated

  @security
  Scenario: Protected pages require authentication
    Given I am not logged in
    When I attempt to access the checkout page directly
    Then I should be redirected to the login page
    And I should see a message indicating authentication is required

  @security
  Scenario: Session persists across page navigation
    Given I am logged in as a customer
    When I navigate to different pages
      | page              |
      | Homepage          |
      | Product Details   |
      | Shopping Bag      |
    Then I should remain authenticated on all pages
    And my session should be maintained

  @performance
  Scenario: Login completes within acceptable time
    Given I am on the login page
    When I enter valid credentials and submit
    Then the login should complete within 2 seconds
    And I should be redirected to the homepage
