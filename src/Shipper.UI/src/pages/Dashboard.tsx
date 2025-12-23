import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Package, PackageCheck, Truck, Clock } from 'lucide-react';
import { getMyAssignedShipments, getMyHistory, getMyProfile } from '@/services/api';

export function Dashboard() {
  const { data: profile } = useQuery({
    queryKey: ['shipper-profile'],
    queryFn: getMyProfile,
  });

  const { data: assigned = [] } = useQuery({
    queryKey: ['assigned-shipments'],
    queryFn: getMyAssignedShipments,
  });

  const { data: history = [] } = useQuery({
    queryKey: ['shipment-history'],
    queryFn: getMyHistory,
  });

  const todayDeliveries = history.filter(s => {
    const completedDate = s.completedAt ? new Date(s.completedAt) : null;
    if (!completedDate) return false;
    const today = new Date();
    return completedDate.toDateString() === today.toDateString();
  });

  const inProgressCount = assigned.filter(s =>
    s.status !== 'ShipperAssigned' && s.status !== 'Delivered'
  ).length;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Welcome back, {profile?.name || 'Shipper'}</h1>
        <p className="text-muted-foreground mt-1">Here's your delivery overview</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Assigned Orders</CardTitle>
            <Package className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{assigned.length}</div>
            <p className="text-xs text-muted-foreground">
              Orders waiting for delivery
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">In Progress</CardTitle>
            <Truck className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{inProgressCount}</div>
            <p className="text-xs text-muted-foreground">
              Currently delivering
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Delivered Today</CardTitle>
            <PackageCheck className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{todayDeliveries.length}</div>
            <p className="text-xs text-muted-foreground">
              Completed deliveries
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Delivered</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{history.length}</div>
            <p className="text-xs text-muted-foreground">
              All time deliveries
            </p>
          </CardContent>
        </Card>
      </div>

      {assigned.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Current Assignments</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {assigned.slice(0, 5).map((shipment) => (
                <div key={shipment.id} className="flex items-center justify-between p-3 border rounded-lg">
                  <div>
                    <p className="font-medium">Order #{shipment.orderId}</p>
                    <p className="text-sm text-muted-foreground">
                      {shipment.customerAddress}, {shipment.customerCity}
                    </p>
                  </div>
                  <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                    shipment.status === 'ShipperAssigned' ? 'bg-blue-100 text-blue-800' :
                    shipment.status === 'PickedUpFromWarehouse' ? 'bg-indigo-100 text-indigo-800' :
                    shipment.status === 'InTransitToWarehouse' ? 'bg-yellow-100 text-yellow-800' :
                    shipment.status === 'DeliveringToCustomer' ? 'bg-orange-100 text-orange-800' :
                    'bg-gray-100 text-gray-800'
                  }`}>
                    {shipment.status}
                  </span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
