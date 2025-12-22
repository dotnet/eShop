# Catalog.API

## Purpose
Product catalog microservice managing products, brands, and categories. Includes AI-powered semantic search using embeddings.

## Tech Stack
- ASP.NET Core Minimal APIs
- PostgreSQL with pgvector extension
- Entity Framework Core
- AI integration (OpenAI/Ollama) for embeddings

## Key Features
- CRUD operations for catalog items
- Product search (text and semantic)
- Image storage and retrieval
- Catalog brands and types management
- AI-powered product descriptions

## API Endpoints
- `GET /api/v1/catalog/items` - List/search items
- `GET /api/v1/catalog/items/{id}` - Get item details
- `GET /api/v1/catalog/items/{id}/pic` - Get product image
- `GET /api/v1/catalog/catalogtypes` - Get product types
- `GET /api/v1/catalog/catalogbrands` - Get brands

## Database
- Uses `catalogdb` PostgreSQL database
- pgvector for embedding storage and similarity search

## Key Files
- `Setup/catalog.json` - Seed data for catalog
- `Pics/` - Product images
