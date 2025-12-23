import { useParams, useNavigate } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';
import { ArrowLeft, AlertCircle } from 'lucide-react';
import { WarehouseForm } from '@/components/warehouses/WarehouseForm';
import { useWarehouse, useUpdateWarehouse } from '@/hooks/useWarehouses';
import type { UpdateWarehouseRequest } from '@/types/warehouse';

export function WarehouseEdit() {
  const { warehouseId } = useParams({ strict: false }) as { warehouseId: string };
  const navigate = useNavigate();
  const { data: warehouse, isLoading, error } = useWarehouse(parseInt(warehouseId));
  const updateWarehouse = useUpdateWarehouse();

  const handleSubmit = async (data: UpdateWarehouseRequest) => {
    await updateWarehouse.mutateAsync({
      id: parseInt(warehouseId),
      data,
    });
    navigate({ to: `/warehouses/${warehouseId}` });
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
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate({ to: `/warehouses/${warehouseId}` })}
        >
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div>
          <h1 className="text-3xl font-bold">Edit Warehouse</h1>
          <p className="text-muted-foreground">Update {warehouse.name}</p>
        </div>
      </div>

      <WarehouseForm
        warehouse={warehouse}
        onSubmit={handleSubmit}
        isSubmitting={updateWarehouse.isPending}
        title="Warehouse Details"
        description="Update the warehouse information"
        submitLabel="Save Changes"
      />
    </div>
  );
}
