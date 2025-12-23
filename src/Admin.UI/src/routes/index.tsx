import { createRouter, createRootRoute, createRoute, Outlet } from '@tanstack/react-router';
import { Layout } from '@/components/layout/Layout';
import { Dashboard } from '@/pages/Dashboard';
import { Warehouses } from '@/pages/Warehouses';
import { WarehouseNew } from '@/pages/WarehouseNew';
import { WarehouseView } from '@/pages/WarehouseView';
import { WarehouseEdit } from '@/pages/WarehouseEdit';
import { Inventory } from '@/pages/Inventory';
import { Login } from '@/pages/Login';
import { Callback } from '@/pages/Callback';

// Root route
const rootRoute = createRootRoute({
  component: () => <Outlet />,
});

// Public routes
const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  component: Login,
});

const callbackRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/callback',
  component: Callback,
});

// Protected layout route
const layoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'layout',
  component: () => (
    <Layout>
      <Outlet />
    </Layout>
  ),
});

// Protected routes
const indexRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/',
  component: Dashboard,
});

const warehousesRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/warehouses',
  component: Warehouses,
});

const warehouseNewRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/warehouses/new',
  component: WarehouseNew,
});

const warehouseViewRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/warehouses/$warehouseId',
  component: WarehouseView,
});

const warehouseEditRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/warehouses/$warehouseId/edit',
  component: WarehouseEdit,
});

const inventoryRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/inventory',
  component: Inventory,
});

// Route tree
const routeTree = rootRoute.addChildren([
  loginRoute,
  callbackRoute,
  layoutRoute.addChildren([
    indexRoute,
    warehousesRoute,
    warehouseNewRoute,
    warehouseViewRoute,
    warehouseEditRoute,
    inventoryRoute,
  ]),
]);

// Create router
export const router = createRouter({ routeTree });

// Type safety
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
