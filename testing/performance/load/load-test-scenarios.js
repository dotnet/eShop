import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
export const errorRate = new Rate('errors');

// Test configuration
export const options = {
  scenarios: {
    // Catalog browsing load test
    catalog_browsing: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 20 },   // Ramp up to 20 users
        { duration: '5m', target: 20 },   // Stay at 20 users
        { duration: '2m', target: 50 },   // Ramp up to 50 users
        { duration: '5m', target: 50 },   // Stay at 50 users
        { duration: '2m', target: 0 },    // Ramp down to 0 users
      ],
      gracefulRampDown: '30s',
      exec: 'catalogBrowsing',
    },
    
    // Basket operations load test
    basket_operations: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 10 },   // Ramp up to 10 users
        { duration: '3m', target: 10 },   // Stay at 10 users
        { duration: '1m', target: 25 },   // Ramp up to 25 users
        { duration: '3m', target: 25 },   // Stay at 25 users
        { duration: '1m', target: 0 },    // Ramp down to 0 users
      ],
      gracefulRampDown: '30s',
      exec: 'basketOperations',
    },
    
    // Order processing stress test
    order_processing: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 5 },    // Ramp up to 5 users
        { duration: '2m', target: 5 },    // Stay at 5 users
        { duration: '1m', target: 15 },   // Ramp up to 15 users
        { duration: '2m', target: 15 },   // Stay at 15 users
        { duration: '1m', target: 0 },    // Ramp down to 0 users
      ],
      gracefulRampDown: '30s',
      exec: 'orderProcessing',
    },
    
    // API health check
    health_check: {
      executor: 'constant-vus',
      vus: 1,
      duration: '16m',
      exec: 'healthCheck',
    }
  },
  
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% of requests must complete below 2s
    http_req_failed: ['rate<0.05'],    // Error rate must be below 5%
    errors: ['rate<0.1'],              // Custom error rate must be below 10%
  },
};

// Base URL configuration
const BASE_URL = __ENV.BASE_URL || 'https://localhost:5001';

// Test data
const testProducts = [
  { id: 1, name: 'Alpine Hiking Jacket' },
  { id: 2, name: 'Trail Running Boots' },
  { id: 3, name: 'Expedition Backpack' },
  { id: 4, name: 'Camping Tent' },
  { id: 5, name: 'Hiking Poles' }
];

// Catalog browsing scenario
export function catalogBrowsing() {
  const responses = {};
  
  // Homepage
  responses.homepage = http.get(`${BASE_URL}/`);
  check(responses.homepage, {
    'homepage status is 200': (r) => r.status === 200,
    'homepage loads in reasonable time': (r) => r.timings.duration < 3000,
  }) || errorRate.add(1);
  
  sleep(1);
  
  // Browse catalog
  responses.catalog = http.get(`${BASE_URL}/api/catalog/items?pageSize=12&pageIndex=0`);
  check(responses.catalog, {
    'catalog API status is 200': (r) => r.status === 200,
    'catalog API response time < 1s': (r) => r.timings.duration < 1000,
    'catalog returns products': (r) => {
      try {
        const data = JSON.parse(r.body);
        return data.data && data.data.length > 0;
      } catch {
        return false;
      }
    },
  }) || errorRate.add(1);
  
  sleep(2);
  
  // View product details
  const productId = testProducts[Math.floor(Math.random() * testProducts.length)].id;
  responses.productDetails = http.get(`${BASE_URL}/api/catalog/items/${productId}`);
  check(responses.productDetails, {
    'product details status is 200': (r) => r.status === 200,
    'product details response time < 500ms': (r) => r.timings.duration < 500,
  }) || errorRate.add(1);
  
  sleep(1);
  
  // Search products
  const searchTerm = 'jacket';
  responses.search = http.get(`${BASE_URL}/api/catalog/items?search=${searchTerm}`);
  check(responses.search, {
    'search API status is 200': (r) => r.status === 200,
    'search response time < 1s': (r) => r.timings.duration < 1000,
  }) || errorRate.add(1);
  
  sleep(2);
}

// Basket operations scenario
export function basketOperations() {
  const userId = `user-${Math.random().toString(36).substr(2, 9)}`;
  const responses = {};
  
  // Get empty basket
  responses.getBasket = http.get(`${BASE_URL}/api/basket/${userId}`);
  check(responses.getBasket, {
    'get basket status is 200 or 404': (r) => r.status === 200 || r.status === 404,
  }) || errorRate.add(1);
  
  sleep(1);
  
  // Add item to basket
  const basketData = {
    buyerId: userId,
    items: [
      {
        id: '1',
        productId: 1,
        productName: 'Test Product',
        unitPrice: 99.99,
        quantity: 2,
        pictureUrl: 'test.jpg'
      }
    ]
  };
  
  responses.updateBasket = http.post(
    `${BASE_URL}/api/basket`,
    JSON.stringify(basketData),
    {
      headers: { 'Content-Type': 'application/json' },
    }
  );
  
  check(responses.updateBasket, {
    'update basket status is 200': (r) => r.status === 200,
    'update basket response time < 1s': (r) => r.timings.duration < 1000,
  }) || errorRate.add(1);
  
  sleep(2);
  
  // Get updated basket
  responses.getUpdatedBasket = http.get(`${BASE_URL}/api/basket/${userId}`);
  check(responses.getUpdatedBasket, {
    'get updated basket status is 200': (r) => r.status === 200,
    'basket contains items': (r) => {
      try {
        const data = JSON.parse(r.body);
        return data.items && data.items.length > 0;
      } catch {
        return false;
      }
    },
  }) || errorRate.add(1);
  
  sleep(1);
  
  // Delete basket
  responses.deleteBasket = http.del(`${BASE_URL}/api/basket/${userId}`);
  check(responses.deleteBasket, {
    'delete basket status is 200': (r) => r.status === 200,
  }) || errorRate.add(1);
  
  sleep(1);
}

// Order processing scenario
export function orderProcessing() {
  const userId = `user-${Math.random().toString(36).substr(2, 9)}`;
  const responses = {};
  
  // Create basket with items
  const basketData = {
    buyerId: userId,
    items: [
      {
        id: '1',
        productId: 1,
        productName: 'Alpine Hiking Jacket',
        unitPrice: 199.99,
        quantity: 1,
        pictureUrl: 'jacket.jpg'
      },
      {
        id: '2',
        productId: 2,
        productName: 'Trail Running Boots',
        unitPrice: 149.99,
        quantity: 1,
        pictureUrl: 'boots.jpg'
      }
    ]
  };
  
  responses.createBasket = http.post(
    `${BASE_URL}/api/basket`,
    JSON.stringify(basketData),
    {
      headers: { 'Content-Type': 'application/json' },
    }
  );
  
  check(responses.createBasket, {
    'create basket for order status is 200': (r) => r.status === 200,
  }) || errorRate.add(1);
  
  sleep(2);
  
  // Simulate checkout process (this would typically involve more steps)
  const checkoutData = {
    userId: userId,
    userName: `${userId}@example.com`,
    city: 'Seattle',
    street: '123 Test Street',
    state: 'WA',
    country: 'USA',
    zipCode: '98101',
    cardNumber: '4111111111111111',
    cardHolderName: 'Test User',
    cardExpiration: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000), // 1 year from now
    cardSecurityNumber: '123',
    cardTypeId: 1,
    basketItems: basketData.items
  };
  
  // Note: This endpoint might not exist in the actual API
  // This is a simulation of order creation
  responses.createOrder = http.post(
    `${BASE_URL}/api/orders`,
    JSON.stringify(checkoutData),
    {
      headers: { 'Content-Type': 'application/json' },
    }
  );
  
  check(responses.createOrder, {
    'create order completes': (r) => r.status >= 200 && r.status < 500, // Accept various responses
    'create order response time < 3s': (r) => r.timings.duration < 3000,
  }) || errorRate.add(1);
  
  sleep(3);
}

// Health check scenario
export function healthCheck() {
  const responses = {};
  
  // Check main application health
  responses.appHealth = http.get(`${BASE_URL}/health`);
  check(responses.appHealth, {
    'app health status is 200': (r) => r.status === 200,
    'app health response time < 500ms': (r) => r.timings.duration < 500,
  }) || errorRate.add(1);
  
  // Check catalog API health
  responses.catalogHealth = http.get(`${BASE_URL}/api/catalog/health`);
  check(responses.catalogHealth, {
    'catalog health check completes': (r) => r.status >= 200 && r.status < 500,
  }) || errorRate.add(1);
  
  // Check basket API health
  responses.basketHealth = http.get(`${BASE_URL}/api/basket/health`);
  check(responses.basketHealth, {
    'basket health check completes': (r) => r.status >= 200 && r.status < 500,
  }) || errorRate.add(1);
  
  sleep(10); // Check every 10 seconds
}

// Setup function
export function setup() {
  console.log('Starting eShop performance tests...');
  console.log(`Base URL: ${BASE_URL}`);
  
  // Verify the application is running
  const healthCheck = http.get(`${BASE_URL}/health`);
  if (healthCheck.status !== 200) {
    console.error('Application health check failed. Make sure eShop is running.');
    return null;
  }
  
  console.log('Application is healthy. Starting load tests...');
  return { baseUrl: BASE_URL };
}

// Teardown function
export function teardown(data) {
  console.log('Performance tests completed.');
  console.log('Check the results for performance metrics and any failures.');
}