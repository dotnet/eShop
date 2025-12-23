import { useState } from 'react';
import { Link } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useShipments, useShippers, useAssignShipper, useCancelShipment } from '@/hooks/useShipping';
import { getStatusColor, formatStatus } from '@/services/shippingApi';
import { Eye, UserPlus, XCircle, AlertCircle, Loader2, Truck } from 'lucide-react';
import type { Shipment, Shipper } from '@/types/shipping';

export function Shipments() {
  const { data: shipments, isLoading, error } = useShipments();
  const { data: shippers } = useShippers();
  const assignShipper = useAssignShipper();
  const cancelShipment = useCancelShipment();
  const [assigningId, setAssigningId] = useState<number | null>(null);

  const availableShippers = shippers?.filter(s => s.isActive && s.isAvailable) || [];

  const handleAssignShipper = async (shipment: Shipment, shipper: Shipper) => {
    setAssigningId(shipment.id);
    try {
      await assignShipper.mutateAsync({ shipmentId: shipment.id, shipperId: shipper.id });
    } finally {
      setAssigningId(null);
    }
  };

  const handleCancel = async (shipment: Shipment) => {
    if (confirm(`Are you sure you want to cancel shipment #${shipment.id}?`)) {
      await cancelShipment.mutateAsync({ shipmentId: shipment.id, reason: 'Cancelled by admin' });
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
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
        Error loading shipments
      </div>
    );
  }

  const pendingShipments = shipments?.filter(s => s.status === 'Created' || !s.shipperId) || [];
  const activeShipments = shipments?.filter(s => s.shipperId && s.status !== 'Delivered' && s.status !== 'Cancelled') || [];
  const completedShipments = shipments?.filter(s => s.status === 'Delivered' || s.status === 'Cancelled') || [];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Shipments</h1>
          <p className="text-muted-foreground">
            Manage order shipments and assignments
          </p>
        </div>
        <Link to="/shippers">
          <Button variant="outline">
            <Truck className="h-4 w-4 mr-2" />
            Manage Shippers
          </Button>
        </Link>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Pending Assignment</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{pendingShipments.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">In Progress</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{activeShipments.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Completed</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{completedShipments.length}</div>
          </CardContent>
        </Card>
      </div>

      {/* Pending Shipments */}
      {pendingShipments.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Pending Assignment</CardTitle>
            <CardDescription>
              Shipments waiting for shipper assignment
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>ID</TableHead>
                  <TableHead>Order</TableHead>
                  <TableHead>Destination</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {pendingShipments.map((shipment) => (
                  <TableRow key={shipment.id}>
                    <TableCell className="font-medium">#{shipment.id}</TableCell>
                    <TableCell>Order #{shipment.orderId}</TableCell>
                    <TableCell>
                      <div>
                        <p>{shipment.customerAddress}</p>
                        <p className="text-sm text-muted-foreground">
                          {shipment.customerCity}, {shipment.customerCountry}
                        </p>
                      </div>
                    </TableCell>
                    <TableCell>{formatDate(shipment.createdAt)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        {availableShippers.length > 0 ? (
                          <select
                            className="text-sm border rounded px-2 py-1"
                            disabled={assigningId === shipment.id}
                            onChange={(e) => {
                              const shipperId = parseInt(e.target.value);
                              const shipper = shippers?.find(s => s.id === shipperId);
                              if (shipper) {
                                handleAssignShipper(shipment, shipper);
                              }
                            }}
                            defaultValue=""
                          >
                            <option value="" disabled>Assign shipper...</option>
                            {availableShippers.map(s => (
                              <option key={s.id} value={s.id}>{s.name}</option>
                            ))}
                          </select>
                        ) : (
                          <span className="text-sm text-muted-foreground">No available shippers</span>
                        )}
                        {assigningId === shipment.id && (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {/* All Shipments */}
      <Card>
        <CardHeader>
          <CardTitle>All Shipments</CardTitle>
          <CardDescription>
            {shipments?.length || 0} total shipments
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead>Order</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Shipper</TableHead>
                <TableHead>Destination</TableHead>
                <TableHead>Created</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {shipments?.map((shipment) => {
                const shipper = shippers?.find(s => s.id === shipment.shipperId);
                return (
                  <TableRow key={shipment.id}>
                    <TableCell className="font-medium">#{shipment.id}</TableCell>
                    <TableCell>Order #{shipment.orderId}</TableCell>
                    <TableCell>
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(shipment.status)}`}>
                        {formatStatus(shipment.status)}
                      </span>
                    </TableCell>
                    <TableCell>
                      {shipper ? shipper.name : <span className="text-muted-foreground">Unassigned</span>}
                    </TableCell>
                    <TableCell>
                      <div>
                        <p className="truncate max-w-[200px]">{shipment.customerAddress}</p>
                        <p className="text-sm text-muted-foreground">
                          {shipment.customerCity}
                        </p>
                      </div>
                    </TableCell>
                    <TableCell>{formatDate(shipment.createdAt)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Link to="/shipments/$shipmentId" params={{ shipmentId: String(shipment.id) }}>
                          <Button variant="ghost" size="icon" title="View Details">
                            <Eye className="h-4 w-4" />
                          </Button>
                        </Link>
                        {!shipment.shipperId && shipment.status !== 'Cancelled' && (
                          <Button
                            variant="ghost"
                            size="icon"
                            title="Assign Shipper"
                            disabled={availableShippers.length === 0}
                          >
                            <UserPlus className="h-4 w-4" />
                          </Button>
                        )}
                        {shipment.status !== 'Delivered' && shipment.status !== 'Cancelled' && (
                          <Button
                            variant="ghost"
                            size="icon"
                            title="Cancel Shipment"
                            onClick={() => handleCancel(shipment)}
                          >
                            <XCircle className="h-4 w-4 text-destructive" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
