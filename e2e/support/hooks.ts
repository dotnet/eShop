import { Before, After, BeforeAll, AfterAll, Status } from '@cucumber/cucumber';
import { chromium } from '@playwright/test';
import { EShopWorld } from './world';

BeforeAll(async function() {
  if (process.env.VERBOSE === 'true') {
    console.log('=== eShop E2E Test Suite Starting ===');
    console.log(`Environment: ${process.env.TEST_ENV || 'local'}`);
    console.log(`Base URL: ${process.env.BASE_URL || 'http://localhost:5045'}`);
    console.log(`Timestamp: ${new Date().toISOString()}`);
  }
});

Before(async function(this: EShopWorld) {
  // Launch browser and create context
  this.browser = await chromium.launch({
    headless: process.env.HEADLESS !== 'false',
    slowMo: process.env.SLOW_MO ? parseInt(process.env.SLOW_MO) : 0
  });
  
  this.context = await this.browser.newContext({
    baseURL: this.baseUrl,  // Set base URL for navigation
    viewport: { width: 1280, height: 720 },
    ignoreHTTPSErrors: true,  // Ignore SSL certificate errors
    recordVideo: process.env.RECORD_VIDEO === 'true' ? {
      dir: 'e2e/reports/videos/'
    } : undefined
  });
  
  this.page = await this.context.newPage();
  this.pageLoadStart = Date.now();
  this.correlationId = this.generateCorrelationId();
  
  this.logMessage('=== Starting Scenario ===');
  this.logMessage(`Correlation ID: ${this.correlationId}`);
  this.logMessage(`Base URL: ${this.baseUrl}`);
});

After(async function(this: EShopWorld, scenario) {
  const duration = Date.now() - this.pageLoadStart;
  
  if (scenario.result?.status === Status.FAILED) {
    this.logMessage(`FAILED: ${scenario.pickle.name}`);
    this.logMessage(`Error: ${scenario.result.message}`);
    
    // Take screenshot on failure
    const screenshot = await this.page.screenshot({ fullPage: true });
    this.attach(screenshot, 'image/png');
    
    // Attach page HTML
    const html = await this.page.content();
    this.attach(html, 'text/html');
  } else {
    this.logMessage(`✓ PASSED: ${scenario.pickle.name}`);
  }
  
  this.logMessage(`=== Scenario completed in ${duration}ms ===`);
  
  // Cleanup
  await this.page.close();
  await this.context.close();
  await this.browser.close();
});

AfterAll(async function() {
  if (process.env.VERBOSE === 'true') {
    console.log('=== eShop E2E Test Suite Completed ===');
    console.log(`Timestamp: ${new Date().toISOString()}`);
  }
});

