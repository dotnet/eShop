# Admin.UI - eShop Admin Dashboard

## Overview
React-based admin dashboard for managing eShop warehouse and inventory.

## Tech Stack
- **Vite** - Build tool and dev server
- **React 18** - UI framework
- **TypeScript** - Type safety
- **TanStack Query** - Server state management
- **TanStack Router** - Type-safe routing
- **Tailwind CSS** - Utility-first CSS
- **shadcn/ui** - Component library patterns
- **oidc-client-ts** - OIDC authentication
- **react-oidc-context** - React OIDC integration

## Project Structure
```
src/
├── components/
│   ├── layout/         # Layout components (Sidebar, Layout)
│   └── ui/             # shadcn/ui components
├── hooks/              # TanStack Query hooks
├── lib/                # Utilities (auth, utils)
├── pages/              # Page components
├── routes/             # TanStack Router routes
├── services/           # API service layer
└── types/              # TypeScript types
```

## Development
```bash
npm install
npm run dev
```

## Environment Variables
- `VITE_IDENTITY_URL` - Identity Server URL
- `VITE_WAREHOUSE_API_URL` - Warehouse API URL
- `VITE_CATALOG_API_URL` - Catalog API URL

## Authentication
Uses OIDC with Identity Server. Client ID: `admin-ui`
- PKCE flow (no client secret)
- Scopes: openid, profile, roles, warehouse, orders

## Pages
- `/` - Dashboard with warehouse overview
- `/warehouses` - Warehouse management (CRUD)
- `/inventory` - Inventory management per warehouse
- `/login` - Login page
- `/callback` - OIDC callback handler
