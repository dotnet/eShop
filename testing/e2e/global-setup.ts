import { chromium, FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting eShop E2E Test Suite Global Setup');
  
  const { baseURL } = config.projects[0].use;
  
  if (!baseURL) {
    throw new Error('Base URL is not configured');
  }
  
  console.log(`📍 Base URL: ${baseURL}`);
  
  // Launch browser for setup tasks
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();
  
  try {
    // Wait for application to be ready
    console.log('⏳ Waiting for application to be ready...');
    
    let retries = 30;
    let isReady = false;
    
    while (retries > 0 && !isReady) {
      try {
        const response = await page.goto(`${baseURL}/health`, { 
          waitUntil: 'networkidle',
          timeout: 5000 
        });
        
        if (response && response.ok()) {
          isReady = true;
          console.log('✅ Application is ready');
        } else {
          throw new Error(`Health check failed with status: ${response?.status()}`);
        }
      } catch (error) {
        retries--;
        console.log(`⏳ Application not ready yet, retrying... (${retries} attempts left)`);
        await page.waitForTimeout(2000);
      }
    }
    
    if (!isReady) {
      throw new Error('Application failed to become ready within the timeout period');
    }
    
    // Verify essential pages are accessible
    console.log('🔍 Verifying essential pages...');
    
    const essentialPages = [
      { path: '/', name: 'Homepage' },
      { path: '/health', name: 'Health Check' }
    ];
    
    for (const { path, name } of essentialPages) {
      try {
        const response = await page.goto(`${baseURL}${path}`, { 
          waitUntil: 'networkidle',
          timeout: 10000 
        });
        
        if (response && response.ok()) {
          console.log(`✅ ${name} is accessible`);
        } else {
          console.warn(`⚠️  ${name} returned status: ${response?.status()}`);
        }
      } catch (error) {
        console.warn(`⚠️  Failed to access ${name}: ${error}`);
      }
    }
    
    // Setup test data if needed
    console.log('📊 Setting up test data...');
    
    // Check if catalog has products
    try {
      const catalogResponse = await page.goto(`${baseURL}/api/catalog/items?pageSize=1`, {
        waitUntil: 'networkidle',
        timeout: 10000
      });
      
      if (catalogResponse && catalogResponse.ok()) {
        const catalogData = await catalogResponse.json();
        if (catalogData && catalogData.data && catalogData.data.length > 0) {
          console.log('✅ Catalog has products available for testing');
        } else {
          console.warn('⚠️  Catalog appears to be empty - some tests may fail');
        }
      }
    } catch (error) {
      console.warn(`⚠️  Could not verify catalog data: ${error}`);
    }
    
    // Create test user session if authentication is available
    try {
      await setupTestUser(page, baseURL);
    } catch (error) {
      console.warn(`⚠️  Could not setup test user: ${error}`);
    }
    
    console.log('✅ Global setup completed successfully');
    
  } catch (error) {
    console.error('❌ Global setup failed:', error);
    throw error;
  } finally {
    await context.close();
    await browser.close();
  }
}

async function setupTestUser(page: any, baseURL: string) {
  console.log('👤 Setting up test user...');
  
  // Try to access login page
  const loginResponse = await page.goto(`${baseURL}/login`, {
    waitUntil: 'networkidle',
    timeout: 5000
  });
  
  if (loginResponse && loginResponse.ok()) {
    console.log('✅ Authentication system is available');
    
    // Check if we can create a test account or if one already exists
    // This is a simplified example - actual implementation would depend on the auth system
    const loginForm = page.locator('[data-testid="login-form"]');
    
    if (await loginForm.isVisible({ timeout: 2000 }).catch(() => false)) {
      console.log('📝 Login form detected - authentication is configured');
    }
  } else {
    console.log('ℹ️  Authentication system not available or not configured');
  }
}

export default globalSetup;