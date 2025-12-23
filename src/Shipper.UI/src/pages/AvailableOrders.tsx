import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ClipboardList, MapPin, Loader2, CheckCircle } from 'lucide-react';
import { getAvailableShipments, claimShipment, getStatusColor, formatStatus } from '@/services/api';

export function AvailableOrders() {
  const queryClient = useQueryClient();

  const { data: shipments = [], isLoading } = useQuery({
    queryKey: ['available-shipments'],
    queryFn: getAvailableShipments,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const claimMutation = useMutation({
    mutationFn: claimShipment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['available-shipments'] });
      queryClient.invalidateQueries({ queryKey: ['assigned-shipments'] });
    },
  });

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
        <h1 className="text-3xl font-bold">Available Orders</h1>
        <p className="text-muted-foreground mt-1">Claim orders to add them to your delivery queue</p>
      </div>

      {shipments.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <ClipboardList className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium">No available orders</h3>
            <p className="text-muted-foreground">All orders have been assigned. Check back later!</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
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
                <div className="space-y-4">
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
                  <Button
                    className="w-full"
                    onClick={() => claimMutation.mutate(shipment.id)}
                    disabled={claimMutation.isPending}
                  >
                    {claimMutation.isPending ? (
                      <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    ) : (
                      <CheckCircle className="h-4 w-4 mr-2" />
                    )}
                    Claim Order
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
