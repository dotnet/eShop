import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Package, MapPin, Loader2, Eye } from 'lucide-react';
import { getMyAssignedShipments, pickupShipment, deliverShipment, getStatusColor, formatStatus } from '@/services/api';
import type { Shipment } from '@/services/api';

export function AssignedOrders() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const { data: shipments = [], isLoading } = useQuery({
    queryKey: ['assigned-shipments'],
    queryFn: getMyAssignedShipments,
  });

  const pickupMutation = useMutation({
    mutationFn: pickupShipment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assigned-shipments'] });
    },
  });

  const deliverMutation = useMutation({
    mutationFn: deliverShipment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assigned-shipments'] });
      queryClient.invalidateQueries({ queryKey: ['shipment-history'] });
    },
  });

  const getNextAction = (shipment: Shipment) => {
    switch (shipment.status) {
      case 'ShipperAssigned':
        return (
          <Button
            size="sm"
            onClick={() => pickupMutation.mutate(shipment.id)}
            disabled={pickupMutation.isPending}
          >
            {pickupMutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              'Pick Up'
            )}
          </Button>
        );
      case 'DeliveringToCustomer':
        return (
          <Button
            size="sm"
            variant="default"
            onClick={() => deliverMutation.mutate(shipment.id)}
            disabled={deliverMutation.isPending}
          >
            {deliverMutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              'Mark Delivered'
            )}
          </Button>
        );
      default:
        return (
          <Button
            size="sm"
            variant="outline"
            onClick={() => navigate({ to: '/shipment/$shipmentId', params: { shipmentId: String(shipment.id) } })}
          >
            <Eye className="h-4 w-4 mr-1" />
            View Details
          </Button>
        );
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Assigned Orders</h1>
        <p className="text-muted-foreground mt-1">Orders assigned to you for delivery</p>
      </div>

      {shipments.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Package className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium">No assigned orders</h3>
            <p className="text-muted-foreground">Check available orders to claim new deliveries</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4">
          {shipments.map((shipment) => (
            <Card key={shipment.id}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-lg">
                  Order #{shipment.orderId}
                </CardTitle>
                <span className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(shipment.status)}`}>
                  {formatStatus(shipment.status)}
                </span>
              </CardHeader>
              <CardContent>
                <div className="flex items-start justify-between">
                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-sm">
                      <MapPin className="h-4 w-4 text-muted-foreground" />
                      <span>{shipment.customerAddress}</span>
                    </div>
                    <p className="text-sm text-muted-foreground">
                      {shipment.customerCity}, {shipment.customerCountry}
                    </p>
                    {shipment.waypoints.length > 0 && (
                      <p className="text-xs text-muted-foreground">
                        {shipment.waypoints.length} waypoint(s) in route
                      </p>
                    )}
                  </div>
                  <div className="flex gap-2">
                    {getNextAction(shipment)}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
