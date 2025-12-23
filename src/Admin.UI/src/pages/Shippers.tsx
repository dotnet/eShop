import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useShippers, useCreateShipper, useUpdateShipper, useDeleteShipper } from '@/hooks/useShipping';
import { Plus, Edit, Trash2, AlertCircle, Loader2, Truck, CheckCircle, XCircle, X } from 'lucide-react';
import type { Shipper, CreateShipperRequest, UpdateShipperRequest } from '@/types/shipping';

export function Shippers() {
  const { data: shippers, isLoading, error } = useShippers();
  const createShipper = useCreateShipper();
  const updateShipper = useUpdateShipper();
  const deleteShipper = useDeleteShipper();

  const [isCreating, setIsCreating] = useState(false);
  const [editingShipper, setEditingShipper] = useState<Shipper | null>(null);

  const [formData, setFormData] = useState<CreateShipperRequest>({
    name: '',
    phone: '',
    userId: '',
  });

  const handleCreate = async () => {
    await createShipper.mutateAsync(formData);
    setFormData({ name: '', phone: '', userId: '' });
    setIsCreating(false);
  };

  const handleUpdate = async () => {
    if (!editingShipper) return;
    const updateData: UpdateShipperRequest = {
      name: formData.name,
      phone: formData.phone,
      isAvailable: editingShipper.isAvailable,
    };
    await updateShipper.mutateAsync({ id: editingShipper.id, data: updateData });
    setEditingShipper(null);
    setFormData({ name: '', phone: '', userId: '' });
  };

  const handleDelete = async (shipper: Shipper) => {
    if (confirm(`Are you sure you want to delete ${shipper.name}?`)) {
      await deleteShipper.mutateAsync(shipper.id);
    }
  };

  const handleToggleAvailability = async (shipper: Shipper) => {
    const updateData: UpdateShipperRequest = {
      name: shipper.name,
      phone: shipper.phone,
      isAvailable: !shipper.isAvailable,
    };
    await updateShipper.mutateAsync({ id: shipper.id, data: updateData });
  };

  const openEditForm = (shipper: Shipper) => {
    setEditingShipper(shipper);
    setFormData({
      name: shipper.name,
      phone: shipper.phone,
      userId: shipper.userId,
    });
    setIsCreating(false);
  };

  const cancelEdit = () => {
    setEditingShipper(null);
    setIsCreating(false);
    setFormData({ name: '', phone: '', userId: '' });
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
        Error loading shippers
      </div>
    );
  }

  const activeShippers = shippers?.filter(s => s.isActive) || [];
  const availableCount = activeShippers.filter(s => s.isAvailable).length;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Shippers</h1>
          <p className="text-muted-foreground">
            Manage delivery personnel
          </p>
        </div>
        {!isCreating && !editingShipper && (
          <Button onClick={() => setIsCreating(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Add Shipper
          </Button>
        )}
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Total Shippers</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{activeShippers.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Available</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{availableCount}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Busy</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-orange-600">
              {activeShippers.length - availableCount}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Create/Edit Form */}
      {(isCreating || editingShipper) && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>{editingShipper ? 'Edit Shipper' : 'Add New Shipper'}</CardTitle>
              <Button variant="ghost" size="icon" onClick={cancelEdit}>
                <X className="h-4 w-4" />
              </Button>
            </div>
            <CardDescription>
              {editingShipper ? 'Update shipper information' : 'Create a new shipper account for delivery personnel'}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-3">
              <div className="grid gap-2">
                <Label htmlFor="name">Name</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="John Doe"
                />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="phone">Phone</Label>
                <Input
                  id="phone"
                  value={formData.phone}
                  onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                  placeholder="+1 234 567 8900"
                />
              </div>
              {isCreating && (
                <div className="grid gap-2">
                  <Label htmlFor="userId">User ID</Label>
                  <Input
                    id="userId"
                    value={formData.userId}
                    onChange={(e) => setFormData({ ...formData, userId: e.target.value })}
                    placeholder="User's identity ID"
                  />
                </div>
              )}
            </div>
            <div className="flex gap-2 mt-4">
              <Button variant="outline" onClick={cancelEdit}>
                Cancel
              </Button>
              {editingShipper ? (
                <Button onClick={handleUpdate} disabled={updateShipper.isPending}>
                  {updateShipper.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  ) : null}
                  Save Changes
                </Button>
              ) : (
                <Button onClick={handleCreate} disabled={createShipper.isPending}>
                  {createShipper.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  ) : null}
                  Create Shipper
                </Button>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Shippers Table */}
      <Card>
        <CardHeader>
          <CardTitle>All Shippers</CardTitle>
          <CardDescription>
            {shippers?.length || 0} registered shippers
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Phone</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Availability</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {shippers?.map((shipper) => (
                <TableRow key={shipper.id}>
                  <TableCell className="font-medium">#{shipper.id}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <Truck className="h-4 w-4 text-muted-foreground" />
                      {shipper.name}
                    </div>
                  </TableCell>
                  <TableCell>{shipper.phone}</TableCell>
                  <TableCell>
                    {shipper.isActive ? (
                      <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">
                        <CheckCircle className="h-3 w-3" />
                        Active
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
                        <XCircle className="h-3 w-3" />
                        Inactive
                      </span>
                    )}
                  </TableCell>
                  <TableCell>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleToggleAvailability(shipper)}
                      disabled={!shipper.isActive}
                    >
                      {shipper.isAvailable ? (
                        <span className="text-green-600">Available</span>
                      ) : (
                        <span className="text-orange-600">Busy</span>
                      )}
                    </Button>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => openEditForm(shipper)}
                        title="Edit"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(shipper)}
                        title="Delete"
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
