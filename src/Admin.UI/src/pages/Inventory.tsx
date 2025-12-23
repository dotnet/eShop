import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useWarehouses, useWarehouseInventory, useAddStock, useRemoveStock } from '@/hooks/useWarehouses';
import { AlertCircle, Plus, Minus } from 'lucide-react';

export function Inventory() {
  const { data: warehouses, isLoading: warehousesLoading } = useWarehouses();
  const [selectedWarehouse, setSelectedWarehouse] = useState<number | null>(null);
  const { data: inventory, isLoading: inventoryLoading } = useWarehouseInventory(selectedWarehouse || 0);
  const addStock = useAddStock();
  const removeStock = useRemoveStock();
  const [amounts, setAmounts] = useState<Record<number, number>>({});

  const handleAddStock = async (catalogItemId: number) => {
    if (!selectedWarehouse) return;
    const amount = amounts[catalogItemId] || 1;
    await addStock.mutateAsync({
      warehouseId: selectedWarehouse,
      catalogItemId,
      amount,
    });
    setAmounts({ ...amounts, [catalogItemId]: 1 });
  };

  const handleRemoveStock = async (catalogItemId: number) => {
    if (!selectedWarehouse) return;
    const amount = amounts[catalogItemId] || 1;
    await removeStock.mutateAsync({
      warehouseId: selectedWarehouse,
      catalogItemId,
      amount,
    });
    setAmounts({ ...amounts, [catalogItemId]: 1 });
  };

  if (warehousesLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Inventory</h1>
        <p className="text-muted-foreground">
          Manage inventory across warehouses
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Select Warehouse</CardTitle>
          <CardDescription>
            Choose a warehouse to view and manage its inventory
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {warehouses?.map((warehouse) => (
              <Button
                key={warehouse.id}
                variant={selectedWarehouse === warehouse.id ? 'default' : 'outline'}
                onClick={() => setSelectedWarehouse(warehouse.id)}
              >
                {warehouse.name}
                <span className="ml-2 text-xs opacity-70">
                  ({warehouse.totalStock})
                </span>
              </Button>
            ))}
          </div>
        </CardContent>
      </Card>

      {selectedWarehouse && (
        <Card>
          <CardHeader>
            <CardTitle>
              Inventory - {warehouses?.find(w => w.id === selectedWarehouse)?.name}
            </CardTitle>
            <CardDescription>
              {inventory?.length || 0} items in stock
            </CardDescription>
          </CardHeader>
          <CardContent>
            {inventoryLoading ? (
              <div className="flex items-center justify-center h-32">
                <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
              </div>
            ) : inventory?.length === 0 ? (
              <div className="flex items-center justify-center h-32 text-muted-foreground">
                <AlertCircle className="h-5 w-5 mr-2" />
                No inventory items found
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Catalog Item ID</TableHead>
                    <TableHead className="text-right">Quantity</TableHead>
                    <TableHead>Last Updated</TableHead>
                    <TableHead className="text-right">Adjust Stock</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {inventory?.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell className="font-medium">
                        Product #{item.catalogItemId}
                      </TableCell>
                      <TableCell className="text-right font-bold">
                        {item.quantity.toLocaleString()}
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        {new Date(item.lastUpdated).toLocaleDateString()}
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex items-center justify-end gap-2">
                          <Input
                            type="number"
                            min="1"
                            value={amounts[item.catalogItemId] || 1}
                            onChange={(e) => setAmounts({
                              ...amounts,
                              [item.catalogItemId]: parseInt(e.target.value) || 1
                            })}
                            className="w-20"
                          />
                          <Button
                            variant="outline"
                            size="icon"
                            onClick={() => handleRemoveStock(item.catalogItemId)}
                            disabled={removeStock.isPending}
                          >
                            <Minus className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="outline"
                            size="icon"
                            onClick={() => handleAddStock(item.catalogItemId)}
                            disabled={addStock.isPending}
                          >
                            <Plus className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
