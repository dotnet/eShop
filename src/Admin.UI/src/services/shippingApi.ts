import { getAccessToken } from '@/lib/auth';
import type { Shipment, Shipper, CreateShipperRequest, UpdateShipperRequest, ShipmentStatus } from '@/types/shipping';

const SHIPPING_API_URL = import.meta.env.VITE_SHIPPING_API_URL || '';

async function fetchWithAuth(url: string, options: RequestInit = {}): Promise<Response> {
  const token = await getAccessToken();

  const headers = new Headers(options.headers);
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  headers.set('Content-Type', 'application/json');

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || `HTTP error! status: ${response.status}`);
  }

  return response;
}

export const shippingApi = {
  // Shipments
  getShipments: async (): Promise<Shipment[]> => {
    const response = await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shipments`);
    return response.json();
  },

  getShipment: async (id: number): Promise<Shipment> => {
    const response = await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shipments/${id}`);
    return response.json();
  },

  getShipmentByOrder: async (orderId: number): Promise<Shipment> => {
    const response = await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shipments/order/${orderId}`);
    return response.json();
  },

  getShipmentsByStatus: async (status: ShipmentStatus): Promise<Shipment[]> => {
    const response = await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shipments/status/${status}`);
    return response.json();
  },

  assignShipper: async (shipmentId: number, shipperId: number): Promise<void> => {
    await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shipments/${shipmentId}/assign-shipper`, {
      method: 'POST',
      body: JSON.stringify({ shipperId }),
    });
  },

  cancelShipment: async (shipmentId: number, reason?: string): Promise<void> => {
    await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shipments/${shipmentId}/cancel`, {
      method: 'POST',
      body: JSON.stringify({ reason }),
    });
  },

  // Shippers
  getShippers: async (): Promise<Shipper[]> => {
    const response = await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shippers`);
    return response.json();
  },

  getShipper: async (id: number): Promise<Shipper> => {
    const response = await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shippers/${id}`);
    return response.json();
  },

  createShipper: async (data: CreateShipperRequest): Promise<Shipper> => {
    const response = await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shippers`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
    return response.json();
  },

  updateShipper: async (id: number, data: UpdateShipperRequest): Promise<void> => {
    await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shippers/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  },

  deleteShipper: async (id: number): Promise<void> => {
    await fetchWithAuth(`${SHIPPING_API_URL}/api/v1/shippers/${id}`, {
      method: 'DELETE',
    });
  },
};

export function getStatusColor(status: ShipmentStatus): string {
  switch (status) {
    case 'Created':
      return 'bg-gray-100 text-gray-800';
    case 'ShipperAssigned':
      return 'bg-blue-100 text-blue-800';
    case 'PickedUpFromWarehouse':
      return 'bg-indigo-100 text-indigo-800';
    case 'InTransitToWarehouse':
      return 'bg-yellow-100 text-yellow-800';
    case 'ArrivedAtWarehouse':
      return 'bg-purple-100 text-purple-800';
    case 'DeliveringToCustomer':
      return 'bg-orange-100 text-orange-800';
    case 'Delivered':
      return 'bg-green-100 text-green-800';
    case 'Cancelled':
      return 'bg-red-100 text-red-800';
    case 'ReturnedToWarehouse':
      return 'bg-pink-100 text-pink-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
}

export function formatStatus(status: ShipmentStatus): string {
  switch (status) {
    case 'Created':
      return 'Created';
    case 'ShipperAssigned':
      return 'Assigned';
    case 'PickedUpFromWarehouse':
      return 'Picked Up';
    case 'InTransitToWarehouse':
      return 'In Transit';
    case 'ArrivedAtWarehouse':
      return 'At Warehouse';
    case 'DeliveringToCustomer':
      return 'Delivering';
    case 'Delivered':
      return 'Delivered';
    case 'Cancelled':
      return 'Cancelled';
    case 'ReturnedToWarehouse':
      return 'Returned';
    default:
      return status;
  }
}
