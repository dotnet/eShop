import { defineConfig, devices, PlaywrightTestConfig  } from '@playwright/test';
require("dotenv").config({ path: "./.env" });
import path from 'path';

export const STORAGE_STATE = path.join(__dirname, 'playwright/.auth/user.json');

/**
 * ReportPortal Configuration
 * Uses environment variables from .env file
 */
const rpConfig = {
  // API Key Authentication
  apiKey: process.env.RP_API_KEY || process.env.RP_TOKEN,
  
  // ReportPortal Server Configuration
  endpoint: process.env.RP_ENDPOINT || 'https://reportportal.epam.com/api/v1',
  project: process.env.RP_PROJECT || 'roberto_meza_personal',
  
  // Launch Configuration
  launch: process.env.RP_LAUNCH || `eShop Playwright E2E - ${process.env.TEST_ENV || 'local'} - ${new Date().toISOString()}`,
  description: process.env.RP_LAUNCH_DESCRIPTION || 'End-to-end Playwright tests for eShop microservices application',
  
  // Launch Attributes (tags for filtering)
  attributes: [
    { key: 'test-type', value: 'e2e' },
    { key: 'framework', value: 'playwright' },
    { key: 'browser', value: 'chromium' },
    { key: 'environment', value: process.env.TEST_ENV || 'local' },
    { key: 'application', value: 'eShop' }
  ],
  
  // Execution Mode
  mode: process.env.RP_MODE || (process.env.TEST_ENV === 'local' ? 'DEBUG' : 'DEFAULT'),
  
  // Additional Options
  debug: process.env.RP_DEBUG === 'true',
  includeTestSteps: true,
  uploadVideo: true,
  uploadTrace: true,
};


/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  testDir: './e2e',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [
    ['html'],
    ['@reportportal/agent-js-playwright', rpConfig]
  ],
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: 'http://localhost:5045',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    ...devices['Desktop Chrome'],
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'setup',
      testMatch: '**/*.setup.ts',
    },
    {
      name: 'e2e tests logged in',
      testMatch: ['**/AddItemTest.spec.ts', '**/RemoveItemTest.spec.ts'],
      dependencies: ['setup'],
      use: {
        storageState: STORAGE_STATE,
      },
    },
    {
      name: 'e2e tests without logged in',
      testMatch: ['**/BrowseItemTest.spec.ts'],
    }
    // {
    //   name: 'chromium',
    //   use: { ...devices['Desktop Chrome'] },
    // },

    // {
    //   name: 'firefox',
    //   use: { ...devices['Desktop Firefox'] },
    // },

    // {
    //   name: 'webkit',
    //   use: { ...devices['Desktop Safari'] },
    // },

    /* Test against mobile viewports. */
    // {
    //   name: 'Mobile Chrome',
    //   use: { ...devices['Pixel 5'] },
    // },
    // {
    //   name: 'Mobile Safari',
    //   use: { ...devices['iPhone 12'] },
    // },

    /* Test against branded browsers. */
    // {
    //   name: 'Microsoft Edge',
    //   use: { ...devices['Desktop Edge'], channel: 'msedge' },
    // },
    // {
    //   name: 'Google Chrome',
    //   use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    // },
  ],

  /* Run your local dev server before starting the tests */
  webServer: {
    command: 'dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj',
    url: 'http://localhost:5045',
    reuseExistingServer: !process.env.CI,
    stderr: 'pipe',
    stdout: 'pipe',
    timeout: process.env.CI ? (5 * 60_000) : 60_000,
    ignoreHTTPSErrors: true,
  },
});
