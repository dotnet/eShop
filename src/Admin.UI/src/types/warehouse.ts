export interface Warehouse {
  id: number;
  name: string;
  address: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  isActive: boolean;
  totalStock: number;
}

export interface WarehouseInventory {
  id: number;
  warehouseId: number;
  warehouseName: string;
  catalogItemId: number;
  quantity: number;
  lastUpdated: string;
}

export interface CreateWarehouseRequest {
  name: string;
  address: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
}

export interface UpdateWarehouseRequest {
  name: string;
  address: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
}

export interface SetInventoryRequest {
  quantity: number;
}

export interface StockAdjustmentRequest {
  amount: number;
}
