# eShop Reference Application

## Overview
eShop is a reference .NET application implementing an e-commerce website using a services-based architecture with .NET Aspire. It demonstrates modern microservices patterns, cloud-native development practices, and distributed application orchestration.

## Key Features
- **Microservices Architecture**: Multiple independent services (Catalog, Basket, Ordering, Identity, Payment, Webhooks)
- **Event-Driven Communication**: Uses RabbitMQ for asynchronous messaging between services
- **Identity & Authentication**: Duende IdentityServer for OAuth/OpenID Connect
- **Data Storage**: PostgreSQL with pgvector for AI/ML capabilities, Redis for caching
- **AI Integration**: Optional OpenAI/Azure OpenAI and Ollama support for AI features
- **Cross-Platform**: Web app, mobile app (MAUI), and hybrid app support

## Business Domain
The application simulates "AdventureWorks" - a fictional outdoor gear retailer with:
- Product catalog management
- Shopping cart functionality  
- Order processing and fulfillment
- Payment processing
- Webhook notifications
- Customer identity management

## Target Audience
- .NET developers learning microservices patterns
- Teams implementing cloud-native applications
- Developers exploring .NET Aspire orchestration
- Organizations adopting event-driven architectures

## Sample Data
All product data, descriptions, and images are AI-generated for demonstration purposes using GPT-35-Turbo and DALL·E 3.