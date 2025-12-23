import { useState } from 'react';
import { Link } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useWarehouses, useDeleteWarehouse, useToggleWarehouseStatus } from '@/hooks/useWarehouses';
import { Plus, Pencil, Trash2, Power, PowerOff, AlertCircle, Eye } from 'lucide-react';
import type { Warehouse } from '@/types/warehouse';

export function Warehouses() {
  const { data: warehouses, isLoading, error } = useWarehouses();
  const deleteWarehouse = useDeleteWarehouse();
  const toggleStatus = useToggleWarehouseStatus();
  const [deletingId, setDeletingId] = useState<number | null>(null);

  const handleDelete = async (warehouse: Warehouse) => {
    if (confirm(`Are you sure you want to delete "${warehouse.name}"?`)) {
      setDeletingId(warehouse.id);
      try {
        await deleteWarehouse.mutateAsync(warehouse.id);
      } finally {
        setDeletingId(null);
      }
    }
  };

  const handleToggleStatus = async (warehouse: Warehouse) => {
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

  if (error) {
    return (
      <div className="flex items-center justify-center h-64 text-destructive">
        <AlertCircle className="h-6 w-6 mr-2" />
        Error loading warehouses
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Warehouses</h1>
          <p className="text-muted-foreground">
            Manage your warehouse locations
          </p>
        </div>
        <Link to="/warehouses/new">
          <Button>
            <Plus className="h-4 w-4 mr-2" />
            Add Warehouse
          </Button>
        </Link>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Warehouses</CardTitle>
          <CardDescription>
            {warehouses?.length || 0} total warehouses
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Status</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Location</TableHead>
                <TableHead>Coordinates</TableHead>
                <TableHead className="text-right">Stock</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {warehouses?.map((warehouse) => (
                <TableRow key={warehouse.id}>
                  <TableCell>
                    <div
                      className={`h-3 w-3 rounded-full ${
                        warehouse.isActive ? 'bg-green-500' : 'bg-gray-400'
                      }`}
                      title={warehouse.isActive ? 'Active' : 'Inactive'}
                    />
                  </TableCell>
                  <TableCell className="font-medium">{warehouse.name}</TableCell>
                  <TableCell>
                    <div>
                      <p>{warehouse.address}</p>
                      <p className="text-sm text-muted-foreground">
                        {warehouse.city}, {warehouse.country}
                      </p>
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className="text-sm text-muted-foreground">
                      {warehouse.latitude.toFixed(4)}, {warehouse.longitude.toFixed(4)}
                    </span>
                  </TableCell>
                  <TableCell className="text-right font-medium">
                    {warehouse.totalStock.toLocaleString()}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Link to="/warehouses/$warehouseId" params={{ warehouseId: String(warehouse.id) }}>
                        <Button variant="ghost" size="icon" title="View">
                          <Eye className="h-4 w-4" />
                        </Button>
                      </Link>
                      <Link to="/warehouses/$warehouseId/edit" params={{ warehouseId: String(warehouse.id) }}>
                        <Button variant="ghost" size="icon" title="Edit">
                          <Pencil className="h-4 w-4" />
                        </Button>
                      </Link>
                      <Button
                        variant="ghost"
                        size="icon"
                        title={warehouse.isActive ? 'Deactivate' : 'Activate'}
                        onClick={() => handleToggleStatus(warehouse)}
                        disabled={toggleStatus.isPending}
                      >
                        {warehouse.isActive ? (
                          <PowerOff className="h-4 w-4" />
                        ) : (
                          <Power className="h-4 w-4" />
                        )}
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        title="Delete"
                        onClick={() => handleDelete(warehouse)}
                        disabled={deletingId === warehouse.id}
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
