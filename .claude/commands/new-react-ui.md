---
description: Create a new React UI service with shadcn/ui and TanStack
argument-hint: [ServiceName]
allowed-tools: Bash, Write, Read, Glob
---

# Create New React UI Service

Create a new React frontend service named **$ARGUMENTS.UI** in the `src/` folder.

## Tech Stack Requirements
- **Vite** + **React 18** + **TypeScript**
- **shadcn/ui** for components
- **TanStack Query** for server state
- **TanStack Router** for routing
- **Tailwind CSS** for styling

## Steps

1. Create project in `src/$ARGUMENTS.UI/` using Vite:
   ```bash
   cd src && npm create vite@latest $ARGUMENTS.UI -- --template react-ts
   ```

2. Install dependencies:
   ```bash
   cd src/$ARGUMENTS.UI
   npm install
   npm install @tanstack/react-query @tanstack/react-query-devtools
   npm install @tanstack/react-router
   npm install -D tailwindcss postcss autoprefixer @types/node
   npm install class-variance-authority clsx tailwind-merge lucide-react
   npx tailwindcss init -p
   ```

3. Create directory structure:
   - `src/components/ui/` - shadcn components
   - `src/components/features/` - feature components
   - `src/components/layout/` - layout components
   - `src/hooks/` - TanStack Query hooks
   - `src/services/` - API service layer
   - `src/lib/` - utilities (cn function)
   - `src/routes/` - TanStack Router routes
   - `src/types/` - TypeScript types

4. Configure Tailwind with shadcn theme variables (dark mode support)

5. Create `src/lib/utils.ts` with `cn()` function for class merging

6. Create `src/services/api.ts` with typed fetch wrapper

7. Setup TanStack Query provider in App.tsx

8. Create a sample Button component following shadcn patterns

9. Configure vite.config.ts with:
   - Path alias `@/` pointing to `./src`
   - Proxy for `/api` to backend

10. Create CLAUDE.md documenting the project

## After Creation
Remind user to:
- Add to AppHost if needed for Aspire integration
- Install additional shadcn components from https://ui.shadcn.com
