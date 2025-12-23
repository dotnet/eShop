import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { shippingApi } from '@/services/shippingApi';
import type { CreateShipperRequest, UpdateShipperRequest, ShipmentStatus } from '@/types/shipping';

// Shipment hooks
export function useShipments() {
  return useQuery({
    queryKey: ['shipments'],
    queryFn: shippingApi.getShipments,
  });
}

export function useShipment(id: number) {
  return useQuery({
    queryKey: ['shipment', id],
    queryFn: () => shippingApi.getShipment(id),
    enabled: !!id,
  });
}

export function useShipmentsByStatus(status: ShipmentStatus) {
  return useQuery({
    queryKey: ['shipments', 'status', status],
    queryFn: () => shippingApi.getShipmentsByStatus(status),
  });
}

export function useAssignShipper() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ shipmentId, shipperId }: { shipmentId: number; shipperId: number }) =>
      shippingApi.assignShipper(shipmentId, shipperId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipments'] });
      queryClient.invalidateQueries({ queryKey: ['shipment'] });
    },
  });
}

export function useCancelShipment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ shipmentId, reason }: { shipmentId: number; reason?: string }) =>
      shippingApi.cancelShipment(shipmentId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipments'] });
      queryClient.invalidateQueries({ queryKey: ['shipment'] });
    },
  });
}

// Shipper hooks
export function useShippers() {
  return useQuery({
    queryKey: ['shippers'],
    queryFn: shippingApi.getShippers,
  });
}

export function useShipper(id: number) {
  return useQuery({
    queryKey: ['shipper', id],
    queryFn: () => shippingApi.getShipper(id),
    enabled: !!id,
  });
}

export function useCreateShipper() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateShipperRequest) => shippingApi.createShipper(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shippers'] });
    },
  });
}

export function useUpdateShipper() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateShipperRequest }) =>
      shippingApi.updateShipper(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shippers'] });
      queryClient.invalidateQueries({ queryKey: ['shipper'] });
    },
  });
}

export function useDeleteShipper() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => shippingApi.deleteShipper(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shippers'] });
    },
  });
}
