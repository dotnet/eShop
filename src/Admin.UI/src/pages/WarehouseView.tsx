import { useParams, useNavigate, Link } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ArrowLeft, Pencil, MapPin, Package, AlertCircle, Power, PowerOff } from 'lucide-react';
import { useWarehouse, useWarehouseInventory, useToggleWarehouseStatus } from '@/hooks/useWarehouses';

export function WarehouseView() {
  const { warehouseId } = useParams({ strict: false }) as { warehouseId: string };
  const navigate = useNavigate();
  const { data: warehouse, isLoading, error } = useWarehouse(parseInt(warehouseId));
  const { data: inventory } = useWarehouseInventory(parseInt(warehouseId));
  const toggleStatus = useToggleWarehouseStatus();

  const handleToggleStatus = async () => {
    if (!warehouse) return;
    await toggleStatus.mutateAsync({
      id: warehouse.id,
      activate: !warehouse.isActive,
    });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (error || !warehouse) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-destructive">
        <AlertCircle className="h-8 w-8 mb-2" />
        <p>Error loading warehouse</p>
        <Button variant="link" onClick={() => navigate({ to: '/warehouses' })}>
          Back to warehouses
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => navigate({ to: '/warehouses' })}
          >
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-3xl font-bold">{warehouse.name}</h1>
              <div
                className={`h-3 w-3 rounded-full ${warehouse.isActive ? 'bg-green-500' : 'bg-gray-400'}`}
                title={warehouse.isActive ? 'Active' : 'Inactive'}
              />
            </div>
            <p className="text-muted-foreground">Warehouse details and inventory</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            onClick={handleToggleStatus}
            disabled={toggleStatus.isPending}
          >
            {warehouse.isActive ? (
              <>
                <PowerOff className="h-4 w-4 mr-2" />
                Deactivate
              </>
            ) : (
              <>
                <Power className="h-4 w-4 mr-2" />
                Activate
              </>
            )}
          </Button>
          <Link to="/warehouses/$warehouseId/edit" params={{ warehouseId: String(warehouse.id) }}>
            <Button>
              <Pencil className="h-4 w-4 mr-2" />
              Edit
            </Button>
          </Link>
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <MapPin className="h-5 w-5" />
              Location
            </CardTitle>
            <CardDescription>Warehouse address and coordinates</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm text-muted-foreground">Address</p>
              <p className="font-medium">{warehouse.address || 'Not specified'}</p>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm text-muted-foreground">City</p>
                <p className="font-medium">{warehouse.city}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Country</p>
                <p className="font-medium">{warehouse.country}</p>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm text-muted-foreground">Latitude</p>
                <p className="font-medium">{warehouse.latitude.toFixed(6)}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Longitude</p>
                <p className="font-medium">{warehouse.longitude.toFixed(6)}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Package className="h-5 w-5" />
              Inventory Summary
            </CardTitle>
            <CardDescription>Stock information</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm text-muted-foreground">Total Stock</p>
              <p className="text-3xl font-bold">{warehouse.totalStock.toLocaleString()}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Unique Products</p>
              <p className="text-xl font-medium">{inventory?.length || 0}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Status</p>
              <p className={`font-medium ${warehouse.isActive ? 'text-green-600' : 'text-gray-500'}`}>
                {warehouse.isActive ? 'Active' : 'Inactive'}
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {inventory && inventory.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Inventory Items</CardTitle>
            <CardDescription>{inventory.length} products in this warehouse</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {inventory.map((item) => (
                <div
                  key={item.id}
                  className="flex items-center justify-between p-3 rounded-lg border"
                >
                  <div>
                    <p className="font-medium">Product #{item.catalogItemId}</p>
                    <p className="text-sm text-muted-foreground">
                      Last updated: {new Date(item.lastUpdated).toLocaleDateString()}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-xl font-bold">{item.quantity.toLocaleString()}</p>
                    <p className="text-sm text-muted-foreground">units</p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
