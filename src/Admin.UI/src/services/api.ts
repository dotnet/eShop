import { getAccessToken } from '@/lib/auth';

const WAREHOUSE_API_URL = import.meta.env.VITE_WAREHOUSE_API_URL || '';

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

export const warehouseApi = {
  // Warehouses
  getAll: async () => {
    const response = await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse`);
    return response.json();
  },

  getById: async (id: number) => {
    const response = await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/${id}`);
    return response.json();
  },

  create: async (data: { name: string; address: string; city: string; country: string; latitude: number; longitude: number }) => {
    const response = await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
    return response.json();
  },

  update: async (id: number, data: { name: string; address: string; city: string; country: string; latitude: number; longitude: number }) => {
    const response = await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
    return response.json();
  },

  delete: async (id: number) => {
    await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/${id}`, {
      method: 'DELETE',
    });
  },

  activate: async (id: number) => {
    await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/${id}/activate`, {
      method: 'PUT',
    });
  },

  deactivate: async (id: number) => {
    await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/${id}/deactivate`, {
      method: 'PUT',
    });
  },

  // Inventory
  getInventory: async (warehouseId: number) => {
    const response = await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/${warehouseId}/inventory`);
    return response.json();
  },

  getProductInventory: async (catalogItemId: number) => {
    const response = await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/inventory/product/${catalogItemId}`);
    return response.json();
  },

  setInventory: async (warehouseId: number, catalogItemId: number, quantity: number) => {
    const response = await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/${warehouseId}/inventory/${catalogItemId}`, {
      method: 'PUT',
      body: JSON.stringify({ quantity }),
    });
    return response.json();
  },

  addStock: async (warehouseId: number, catalogItemId: number, amount: number) => {
    const response = await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/${warehouseId}/inventory/${catalogItemId}/add`, {
      method: 'POST',
      body: JSON.stringify({ amount }),
    });
    return response.json();
  },

  removeStock: async (warehouseId: number, catalogItemId: number, amount: number) => {
    const response = await fetchWithAuth(`${WAREHOUSE_API_URL}/api/warehouse/${warehouseId}/inventory/${catalogItemId}/remove`, {
      method: 'POST',
      body: JSON.stringify({ amount }),
    });
    return response.json();
  },
};
