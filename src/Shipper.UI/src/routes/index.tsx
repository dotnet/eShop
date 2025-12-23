import { createRouter, createRootRoute, createRoute, Outlet } from '@tanstack/react-router';
import { Layout } from '@/components/layout/Layout';
import { Dashboard } from '@/pages/Dashboard';
import { AssignedOrders } from '@/pages/AssignedOrders';
import { AvailableOrders } from '@/pages/AvailableOrders';
import { History } from '@/pages/History';
import { ShipmentDetail } from '@/pages/ShipmentDetail';
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

const assignedRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/assigned',
  component: AssignedOrders,
});

const availableRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/available',
  component: AvailableOrders,
});

const historyRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/history',
  component: History,
});

const shipmentDetailRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/shipment/$shipmentId',
  component: ShipmentDetail,
});

// Route tree
const routeTree = rootRoute.addChildren([
  loginRoute,
  callbackRoute,
  layoutRoute.addChildren([
    indexRoute,
    assignedRoute,
    availableRoute,
    historyRoute,
    shipmentDetailRoute,
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
