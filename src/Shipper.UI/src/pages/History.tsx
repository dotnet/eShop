import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { History as HistoryIcon, MapPin, Loader2, Calendar, CheckCircle2 } from 'lucide-react';
import { getMyHistory, getStatusColor, formatStatus } from '@/services/api';

export function History() {
  const { data: shipments = [], isLoading } = useQuery({
    queryKey: ['shipment-history'],
    queryFn: getMyHistory,
  });

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

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Delivery History</h1>
        <p className="text-muted-foreground mt-1">Your completed deliveries</p>
      </div>

      {shipments.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <HistoryIcon className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium">No delivery history</h3>
            <p className="text-muted-foreground">Complete deliveries to see them here</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {shipments.map((shipment) => (
            <Card key={shipment.id}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <div className="flex items-center gap-3">
                  <CheckCircle2 className="h-5 w-5 text-green-500" />
                  <CardTitle className="text-lg">
                    Order #{shipment.orderId}
                  </CardTitle>
                </div>
                <span className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(shipment.status)}`}>
                  {formatStatus(shipment.status)}
                </span>
              </CardHeader>
              <CardContent>
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-sm">
                      <MapPin className="h-4 w-4 text-muted-foreground" />
                      <span>{shipment.customerAddress}</span>
                    </div>
                    <p className="text-sm text-muted-foreground pl-6">
                      {shipment.customerCity}, {shipment.customerCountry}
                    </p>
                  </div>
                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-sm">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                      <span>Completed: {formatDate(shipment.completedAt)}</span>
                    </div>
                    <p className="text-sm text-muted-foreground pl-6">
                      Created: {formatDate(shipment.createdAt)}
                    </p>
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
