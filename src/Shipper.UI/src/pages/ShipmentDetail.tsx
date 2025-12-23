import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useNavigate } from '@tanstack/react-router';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ArrowLeft, MapPin, Loader2, CheckCircle, Circle, Truck } from 'lucide-react';
import {
  getShipment,
  pickupShipment,
  arriveAtWaypoint,
  departFromWaypoint,
  deliverShipment,
  getStatusColor,
  formatStatus,
} from '@/services/api';
import type { ShipmentWaypoint } from '@/services/api';

export function ShipmentDetail() {
  const params = useParams({ strict: false });
  const shipmentId = params.shipmentId ?? '';
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: shipment, isLoading } = useQuery({
    queryKey: ['shipment', shipmentId],
    queryFn: () => getShipment(parseInt(shipmentId)),
    enabled: !!shipmentId,
  });

  const pickupMutation = useMutation({
    mutationFn: pickupShipment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipment', shipmentId] });
      queryClient.invalidateQueries({ queryKey: ['assigned-shipments'] });
    },
  });

  const arriveMutation = useMutation({
    mutationFn: ({ shipmentId, waypointId }: { shipmentId: number; waypointId: number }) =>
      arriveAtWaypoint(shipmentId, waypointId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipment', shipmentId] });
    },
  });

  const departMutation = useMutation({
    mutationFn: ({ shipmentId, waypointId }: { shipmentId: number; waypointId: number }) =>
      departFromWaypoint(shipmentId, waypointId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipment', shipmentId] });
    },
  });

  const deliverMutation = useMutation({
    mutationFn: deliverShipment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipment', shipmentId] });
      queryClient.invalidateQueries({ queryKey: ['assigned-shipments'] });
      queryClient.invalidateQueries({ queryKey: ['shipment-history'] });
      navigate({ to: '/assigned' });
    },
  });

  const getWaypointAction = (waypoint: ShipmentWaypoint) => {
    if (!shipment) return null;

    const isPending = !waypoint.arrivedAt;
    const isArrived = waypoint.arrivedAt && !waypoint.departedAt;
    const isDeparted = waypoint.departedAt;

    if (isDeparted) {
      return <CheckCircle className="h-5 w-5 text-green-500" />;
    }

    if (isArrived) {
      return (
        <Button
          size="sm"
          onClick={() => departMutation.mutate({ shipmentId: shipment.id, waypointId: waypoint.id })}
          disabled={departMutation.isPending}
        >
          {departMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Depart'}
        </Button>
      );
    }

    if (isPending) {
      const previousWaypoints = shipment.waypoints.filter(w => w.sequence < waypoint.sequence);
      const allPreviousCompleted = previousWaypoints.every(w => w.departedAt);

      if (allPreviousCompleted) {
        return (
          <Button
            size="sm"
            onClick={() => arriveMutation.mutate({ shipmentId: shipment.id, waypointId: waypoint.id })}
            disabled={arriveMutation.isPending}
          >
            {arriveMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Arrive'}
          </Button>
        );
      }
    }

    return <Circle className="h-5 w-5 text-muted-foreground" />;
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!shipment) {
    return (
      <div className="text-center py-12">
        <h2 className="text-xl font-semibold">Shipment not found</h2>
        <Button variant="link" onClick={() => navigate({ to: '/assigned' })}>
          Back to assigned orders
        </Button>
      </div>
    );
  }

  const allWaypointsCompleted = shipment.waypoints.every(w => w.departedAt);
  const canDeliver = shipment.status === 'DeliveringToCustomer' ||
    (allWaypointsCompleted && shipment.status !== 'Delivered');

  return (
    <div className="space-y-6">
      <Button variant="ghost" onClick={() => navigate({ to: '/assigned' })}>
        <ArrowLeft className="h-4 w-4 mr-2" />
        Back to Assigned Orders
      </Button>

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Order #{shipment.orderId}</h1>
          <p className="text-muted-foreground mt-1">Shipment #{shipment.id}</p>
        </div>
        <span className={`px-3 py-1.5 rounded-full text-sm font-medium ${getStatusColor(shipment.status)}`}>
          {formatStatus(shipment.status)}
        </span>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Delivery Address</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-start gap-3">
              <MapPin className="h-5 w-5 text-muted-foreground mt-0.5" />
              <div>
                <p className="font-medium">{shipment.customerAddress}</p>
                <p className="text-muted-foreground">{shipment.customerCity}, {shipment.customerCountry}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Actions</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {shipment.status === 'ShipperAssigned' && (
              <Button
                className="w-full"
                onClick={() => pickupMutation.mutate(shipment.id)}
                disabled={pickupMutation.isPending}
              >
                {pickupMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  <Truck className="h-4 w-4 mr-2" />
                )}
                Pick Up from Warehouse
              </Button>
            )}
            {canDeliver && (
              <Button
                className="w-full"
                onClick={() => deliverMutation.mutate(shipment.id)}
                disabled={deliverMutation.isPending}
              >
                {deliverMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  <CheckCircle className="h-4 w-4 mr-2" />
                )}
                Mark as Delivered
              </Button>
            )}
            {shipment.status === 'Delivered' && (
              <p className="text-center text-green-600 font-medium">
                Delivery completed!
              </p>
            )}
          </CardContent>
        </Card>
      </div>

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
                  <div
                    key={waypoint.id}
                    className="flex items-center justify-between p-3 border rounded-lg"
                  >
                    <div className="flex items-center gap-3">
                      <span className="flex items-center justify-center w-8 h-8 rounded-full bg-muted text-sm font-medium">
                        {index + 1}
                      </span>
                      <div>
                        <p className="font-medium">
                          Warehouse #{waypoint.warehouseId}
                          {waypoint.warehouseName && ` - ${waypoint.warehouseName}`}
                        </p>
                        <p className="text-sm text-muted-foreground">
                          {waypoint.arrivedAt
                            ? `Arrived: ${new Date(waypoint.arrivedAt).toLocaleTimeString()}`
                            : 'Pending'}
                          {waypoint.departedAt && ` | Departed: ${new Date(waypoint.departedAt).toLocaleTimeString()}`}
                        </p>
                      </div>
                    </div>
                    {getWaypointAction(waypoint)}
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      )}

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
                  <div key={history.id} className="flex items-center gap-3 text-sm">
                    <span className={`px-2 py-0.5 rounded text-xs ${getStatusColor(history.status)}`}>
                      {formatStatus(history.status)}
                    </span>
                    <span className="text-muted-foreground">
                      {new Date(history.timestamp).toLocaleString()}
                    </span>
                    {history.notes && <span className="text-muted-foreground">- {history.notes}</span>}
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
