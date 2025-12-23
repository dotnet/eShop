import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { warehouseApi } from '@/services/api';
import type { Warehouse, WarehouseInventory, CreateWarehouseRequest, UpdateWarehouseRequest } from '@/types/warehouse';

export function useWarehouses() {
  return useQuery<Warehouse[]>({
    queryKey: ['warehouses'],
    queryFn: warehouseApi.getAll,
  });
}

export function useWarehouse(id: number) {
  return useQuery<Warehouse>({
    queryKey: ['warehouse', id],
    queryFn: () => warehouseApi.getById(id),
    enabled: !!id,
  });
}

export function useCreateWarehouse() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateWarehouseRequest) => warehouseApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['warehouses'] });
    },
  });
}

export function useUpdateWarehouse() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateWarehouseRequest }) =>
      warehouseApi.update(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['warehouses'] });
      queryClient.invalidateQueries({ queryKey: ['warehouse', variables.id] });
    },
  });
}

export function useDeleteWarehouse() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => warehouseApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['warehouses'] });
    },
  });
}

export function useToggleWarehouseStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, activate }: { id: number; activate: boolean }) =>
      activate ? warehouseApi.activate(id) : warehouseApi.deactivate(id),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['warehouses'] });
      queryClient.invalidateQueries({ queryKey: ['warehouse', variables.id] });
    },
  });
}

export function useWarehouseInventory(warehouseId: number) {
  return useQuery<WarehouseInventory[]>({
    queryKey: ['inventory', warehouseId],
    queryFn: () => warehouseApi.getInventory(warehouseId),
    enabled: !!warehouseId,
  });
}

export function useProductInventory(catalogItemId: number) {
  return useQuery<WarehouseInventory[]>({
    queryKey: ['productInventory', catalogItemId],
    queryFn: () => warehouseApi.getProductInventory(catalogItemId),
    enabled: !!catalogItemId,
  });
}

export function useSetInventory() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ warehouseId, catalogItemId, quantity }: { warehouseId: number; catalogItemId: number; quantity: number }) =>
      warehouseApi.setInventory(warehouseId, catalogItemId, quantity),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['inventory', variables.warehouseId] });
      queryClient.invalidateQueries({ queryKey: ['warehouses'] });
    },
  });
}

export function useAddStock() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ warehouseId, catalogItemId, amount }: { warehouseId: number; catalogItemId: number; amount: number }) =>
      warehouseApi.addStock(warehouseId, catalogItemId, amount),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['inventory', variables.warehouseId] });
      queryClient.invalidateQueries({ queryKey: ['warehouses'] });
    },
  });
}

export function useRemoveStock() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ warehouseId, catalogItemId, amount }: { warehouseId: number; catalogItemId: number; amount: number }) =>
      warehouseApi.removeStock(warehouseId, catalogItemId, amount),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['inventory', variables.warehouseId] });
      queryClient.invalidateQueries({ queryKey: ['warehouses'] });
    },
  });
}
