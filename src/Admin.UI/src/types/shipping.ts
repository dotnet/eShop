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

export interface Shipper {
  id: number;
  name: string;
  phone: string;
  userId: string;
  currentWarehouseId: number | null;
  isAvailable: boolean;
  isActive: boolean;
}

export interface CreateShipperRequest {
  name: string;
  phone: string;
  userId: string;
  currentWarehouseId?: number | null;
}

export interface UpdateShipperRequest {
  name: string;
  phone: string;
  currentWarehouseId?: number | null;
  isAvailable: boolean;
}
