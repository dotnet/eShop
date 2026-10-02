import { defineConfig, devices } from '@playwright/test';
require('dotenv').config({ path: './.env', quiet: true });
import path from 'path';

export const STORAGE_STATE = path.join(__dirname, 'playwright/.auth/user.json');

// Support environment variable overrides for base URL and web server
const BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5045';
const PLAYWRIGHT_IGNORE_HTTPS = process.env.PLAYWRIGHT_BASE_URL?.startsWith('https') ? true : false;


/**
 * See https://playwright.dev/docs/test-configuration.
 */
const config = defineConfig({
  testDir: './e2e',
  /* Seeded users share server-side basket state, so UI journeys run serially. */
  fullyParallel: false,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [['html', { open: 'never' }]],
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: BASE_URL,

    /* Ignore HTTPS errors for self-signed certificates */
    ignoreHTTPSErrors: PLAYWRIGHT_IGNORE_HTTPS,

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
      testMatch: ['**/CartTest.spec.ts', '**/CheckoutTest.spec.ts'],
      dependencies: ['setup'],
      use: {
        storageState: STORAGE_STATE,
      },
    },
    {
      name: 'e2e tests without logged in',
      testMatch: ['**/BrowseItemTest.spec.ts'],
    }
  ],

  /* Run your local dev server before starting the tests - only for default localhost URL */
  ...(BASE_URL === 'http://localhost:5045' ? {
    webServer: {
      command: 'dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj',
      url: 'http://localhost:5045',
      reuseExistingServer: !process.env.CI,
      stderr: 'pipe',
      stdout: 'pipe',
      timeout: process.env.CI ? (5 * 60_000) : 60_000,
    },
  } : {}),
});

export default config;
