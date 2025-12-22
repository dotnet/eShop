# WebApp

## Purpose
Main customer-facing web application built with Blazor Server. Provides the e-commerce shopping experience.

## Tech Stack
- Blazor Server (Interactive Server rendering)
- Razor Components
- HttpClient for API communication
- OpenID Connect authentication

## Key Features
- Product catalog browsing
- Shopping cart management
- User authentication
- Order placement and history
- AI-powered product search (when enabled)

## Pages
- Home/Catalog - Product listing
- Item details - Product information
- Cart - Shopping basket
- Checkout - Order placement
- Orders - Order history

## Authentication
- Integrates with Identity.API via OpenID Connect
- Cookie-based session management

## Dependencies
- References WebAppComponents for shared UI
- Communicates with all backend APIs
