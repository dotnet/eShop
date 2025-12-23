import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useWarehouses } from '@/hooks/useWarehouses';
import { Warehouse, Package, MapPin, AlertCircle } from 'lucide-react';

export function Dashboard() {
  const { data: warehouses, isLoading, error } = useWarehouses();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-64 text-destructive">
        <AlertCircle className="h-6 w-6 mr-2" />
        Error loading data
      </div>
    );
  }

  const totalWarehouses = warehouses?.length || 0;
  const activeWarehouses = warehouses?.filter(w => w.isActive).length || 0;
  const totalStock = warehouses?.reduce((acc, w) => acc + w.totalStock, 0) || 0;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <p className="text-muted-foreground">
          Welcome to the eShop Admin Dashboard
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Warehouses</CardTitle>
            <Warehouse className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalWarehouses}</div>
            <p className="text-xs text-muted-foreground">
              {activeWarehouses} active
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Warehouses</CardTitle>
            <MapPin className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{activeWarehouses}</div>
            <p className="text-xs text-muted-foreground">
              {totalWarehouses - activeWarehouses} inactive
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Stock</CardTitle>
            <Package className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalStock.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">
              items across all warehouses
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Avg Stock/Warehouse</CardTitle>
            <Package className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {totalWarehouses > 0 ? Math.round(totalStock / totalWarehouses).toLocaleString() : 0}
            </div>
            <p className="text-xs text-muted-foreground">
              items per warehouse
            </p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Warehouses Overview</CardTitle>
          <CardDescription>
            Quick view of all warehouse locations
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {warehouses?.map((warehouse) => (
              <div
                key={warehouse.id}
                className="flex items-center justify-between p-4 rounded-lg border"
              >
                <div className="flex items-center gap-4">
                  <div className={`h-3 w-3 rounded-full ${warehouse.isActive ? 'bg-green-500' : 'bg-gray-400'}`} />
                  <div>
                    <p className="font-medium">{warehouse.name}</p>
                    <p className="text-sm text-muted-foreground">
                      {warehouse.city}, {warehouse.country}
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="font-medium">{warehouse.totalStock.toLocaleString()}</p>
                  <p className="text-sm text-muted-foreground">items in stock</p>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
