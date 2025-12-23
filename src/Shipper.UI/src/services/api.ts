import { getAccessToken } from '@/lib/auth';

const shippingApiUrl = import.meta.env.VITE_SHIPPING_API_URL || '';

async function fetchWithAuth(url: string, options: RequestInit = {}) {
  const token = await getAccessToken();

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const error = await response.text().catch(() => response.statusText);
    throw new Error(error || `HTTP ${response.status}`);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

export interface Shipment {
  id: number;
  orderId: number;
  shipperId: number | null;
  status: ShipmentStatus;
  customerAddress: string;
  customerCity: string;
  customerCountry: string;
  createdAt: string;
  completedAt: string | null;
  waypoints: ShipmentWaypoint[];
  statusHistory: ShipmentStatusHistoryItem[];
}

export interface ShipmentWaypoint {
  id: number;
  shipmentId: number;
  warehouseId: number;
  warehouseName?: string;
  sequence: number;
  arrivedAt: string | null;
  departedAt: string | null;
}

export interface ShipmentStatusHistoryItem {
  id: number;
  shipmentId: number;
  status: ShipmentStatus;
  timestamp: string;
  waypointId: number | null;
  notes: string | null;
}

export type ShipmentStatus =
  | 'Created'
  | 'ShipperAssigned'
  | 'PickedUpFromWarehouse'
  | 'InTransitToWarehouse'
  | 'ArrivedAtWarehouse'
  | 'DeliveringToCustomer'
  | 'Delivered'
  | 'Cancelled'
  | 'ReturnedToWarehouse';

export interface Shipper {
  id: number;
  name: string;
  phone: string;
  userId: string;
  currentWarehouseId: number | null;
  isAvailable: boolean;
  isActive: boolean;
}

// Shipper self-service endpoints
export async function getMyProfile(): Promise<Shipper> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shippers/me`);
}

export async function getMyAssignedShipments(): Promise<Shipment[]> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shippers/me/assigned`);
}

export async function getAvailableShipments(): Promise<Shipment[]> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shippers/me/available`);
}

export async function getMyHistory(): Promise<Shipment[]> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shippers/me/history`);
}

export async function claimShipment(shipmentId: number): Promise<void> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shippers/me/claim/${shipmentId}`, {
    method: 'POST',
  });
}

export async function pickupShipment(shipmentId: number): Promise<void> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shippers/me/shipment/${shipmentId}/pickup`, {
    method: 'PUT',
  });
}

export async function arriveAtWaypoint(shipmentId: number, waypointId: number): Promise<void> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shippers/me/shipment/${shipmentId}/arrive/${waypointId}`, {
    method: 'PUT',
  });
}

export async function departFromWaypoint(shipmentId: number, waypointId: number): Promise<void> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shippers/me/shipment/${shipmentId}/depart/${waypointId}`, {
    method: 'PUT',
  });
}

export async function deliverShipment(shipmentId: number): Promise<void> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shippers/me/shipment/${shipmentId}/deliver`, {
    method: 'PUT',
  });
}

// Get shipment by ID
export async function getShipment(id: number): Promise<Shipment> {
  return fetchWithAuth(`${shippingApiUrl}/api/v1/shipments/${id}`);
}

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
