import { useNavigate } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';
import { ArrowLeft } from 'lucide-react';
import { WarehouseForm } from '@/components/warehouses/WarehouseForm';
import { useCreateWarehouse } from '@/hooks/useWarehouses';
import type { CreateWarehouseRequest } from '@/types/warehouse';

export function WarehouseNew() {
  const navigate = useNavigate();
  const createWarehouse = useCreateWarehouse();

  const handleSubmit = async (data: CreateWarehouseRequest) => {
    await createWarehouse.mutateAsync(data);
    navigate({ to: '/warehouses' });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate({ to: '/warehouses' })}
        >
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div>
          <h1 className="text-3xl font-bold">New Warehouse</h1>
          <p className="text-muted-foreground">Add a new warehouse location</p>
        </div>
      </div>

      <WarehouseForm
        onSubmit={handleSubmit}
        isSubmitting={createWarehouse.isPending}
        title="Warehouse Details"
        description="Enter the details for the new warehouse"
        submitLabel="Create Warehouse"
      />
    </div>
  );
}
