import { useParams, useNavigate } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useShipment, useShippers, useAssignShipper, useCancelShipment } from '@/hooks/useShipping';
import { getStatusColor, formatStatus } from '@/services/shippingApi';
import { ArrowLeft, MapPin, User, Clock, AlertCircle, Loader2, CheckCircle, Circle } from 'lucide-react';
import type { Shipper } from '@/types/shipping';

export function ShipmentDetail() {
  const params = useParams({ strict: false });
  const shipmentId = params.shipmentId ? parseInt(params.shipmentId) : 0;
  const navigate = useNavigate();

  const { data: shipment, isLoading, error } = useShipment(shipmentId);
  const { data: shippers } = useShippers();
  const assignShipper = useAssignShipper();
  const cancelShipment = useCancelShipment();

  const availableShippers = shippers?.filter(s => s.isActive && s.isAvailable) || [];
  const currentShipper = shippers?.find(s => s.id === shipment?.shipperId);

  const handleAssignShipper = async (shipper: Shipper) => {
    if (shipment) {
      await assignShipper.mutateAsync({ shipmentId: shipment.id, shipperId: shipper.id });
    }
  };

  const handleCancel = async () => {
    if (shipment && confirm(`Are you sure you want to cancel this shipment?`)) {
      await cancelShipment.mutateAsync({ shipmentId: shipment.id, reason: 'Cancelled by admin' });
    }
  };

  const formatDate = (dateString: string | null) => {
    if (!dateString) return 'N/A';
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
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (error || !shipment) {
    return (
      <div className="flex flex-col items-center justify-center h-64">
        <AlertCircle className="h-8 w-8 text-destructive mb-2" />
        <p>Shipment not found</p>
        <Button variant="link" onClick={() => navigate({ to: '/shipments' })}>
          Back to shipments
        </Button>
      </div>
    );
  }

  const canAssign = !shipment.shipperId && shipment.status !== 'Cancelled' && shipment.status !== 'Delivered';
  const canCancel = shipment.status !== 'Delivered' && shipment.status !== 'Cancelled';

  return (
    <div className="space-y-6">
      <Button variant="ghost" onClick={() => navigate({ to: '/shipments' })}>
        <ArrowLeft className="h-4 w-4 mr-2" />
        Back to Shipments
      </Button>

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Shipment #{shipment.id}</h1>
          <p className="text-muted-foreground">Order #{shipment.orderId}</p>
        </div>
        <span className={`px-3 py-1.5 rounded-full text-sm font-medium ${getStatusColor(shipment.status)}`}>
          {formatStatus(shipment.status)}
        </span>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        {/* Delivery Info */}
        <Card>
          <CardHeader>
            <CardTitle>Delivery Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-start gap-3">
              <MapPin className="h-5 w-5 text-muted-foreground mt-0.5" />
              <div>
                <p className="font-medium">{shipment.customerAddress}</p>
                <p className="text-muted-foreground">{shipment.customerCity}, {shipment.customerCountry}</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <Clock className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="text-sm text-muted-foreground">Created</p>
                <p>{formatDate(shipment.createdAt)}</p>
              </div>
            </div>
            {shipment.completedAt && (
              <div className="flex items-center gap-3">
                <CheckCircle className="h-5 w-5 text-green-500" />
                <div>
                  <p className="text-sm text-muted-foreground">Completed</p>
                  <p>{formatDate(shipment.completedAt)}</p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Shipper Info */}
        <Card>
          <CardHeader>
            <CardTitle>Shipper</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {currentShipper ? (
              <div className="flex items-center gap-3">
                <User className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="font-medium">{currentShipper.name}</p>
                  <p className="text-sm text-muted-foreground">{currentShipper.phone}</p>
                </div>
              </div>
            ) : (
              <p className="text-muted-foreground">No shipper assigned</p>
            )}

            {canAssign && availableShippers.length > 0 && (
              <div className="pt-4 border-t">
                <p className="text-sm font-medium mb-2">Assign Shipper</p>
                <select
                  className="w-full border rounded px-3 py-2"
                  disabled={assignShipper.isPending}
                  onChange={(e) => {
                    const shipperId = parseInt(e.target.value);
                    const shipper = shippers?.find(s => s.id === shipperId);
                    if (shipper) {
                      handleAssignShipper(shipper);
                    }
                  }}
                  defaultValue=""
                >
                  <option value="" disabled>Select a shipper...</option>
                  {availableShippers.map(s => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </select>
              </div>
            )}

            {canCancel && (
              <div className="pt-4">
                <Button
                  variant="destructive"
                  className="w-full"
                  onClick={handleCancel}
                  disabled={cancelShipment.isPending}
                >
                  {cancelShipment.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  ) : null}
                  Cancel Shipment
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Waypoints */}
      {shipment.waypoints.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Route Waypoints</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {shipment.waypoints
                .sort((a, b) => a.sequence - b.sequence)
                .map((waypoint, index) => (
                  <div key={waypoint.id} className="flex items-center gap-4 p-3 border rounded-lg">
                    <span className="flex items-center justify-center w-8 h-8 rounded-full bg-muted text-sm font-medium">
                      {index + 1}
                    </span>
                    <div className="flex-1">
                      <p className="font-medium">
                        Warehouse #{waypoint.warehouseId}
                        {waypoint.warehouseName && ` - ${waypoint.warehouseName}`}
                      </p>
                      <p className="text-sm text-muted-foreground">
                        {waypoint.arrivedAt ? `Arrived: ${formatDate(waypoint.arrivedAt)}` : 'Pending'}
                        {waypoint.departedAt && ` | Departed: ${formatDate(waypoint.departedAt)}`}
                      </p>
                    </div>
                    {waypoint.departedAt ? (
                      <CheckCircle className="h-5 w-5 text-green-500" />
                    ) : waypoint.arrivedAt ? (
                      <Circle className="h-5 w-5 text-blue-500 fill-blue-500" />
                    ) : (
                      <Circle className="h-5 w-5 text-muted-foreground" />
                    )}
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Status History */}
      {shipment.statusHistory.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Status History</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {shipment.statusHistory
                .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
                .map((history) => (
                  <div key={history.id} className="flex items-center gap-4 text-sm">
                    <span className={`px-2 py-0.5 rounded text-xs ${getStatusColor(history.status)}`}>
                      {formatStatus(history.status)}
                    </span>
                    <span className="text-muted-foreground">
                      {formatDate(history.timestamp)}
                    </span>
                    {history.notes && (
                      <span className="text-muted-foreground">- {history.notes}</span>
                    )}
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
