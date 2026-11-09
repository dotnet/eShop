import { defineConfig } from '@playwright/test';
import baseConfig from './playwright.config';

/**
 * Runs the e2e suite against the Docker Compose test stack (testing/teststack up) instead of
 * starting the AppHost. Seeded users come from testing/scenarios/default.
 */
process.env.USERNAME1 ??= 'alice';
process.env.PASSWORD ??= 'Pass123$';

export default defineConfig({
  ...baseConfig,
  use: {
    ...baseConfig.use,
    baseURL: process.env.TESTSTACK_URL ?? 'http://localhost:18080',
    launchOptions: {
      // Identity redirects the browser to host.docker.internal:18081 (the same URL the containers use).
      args: ['--host-resolver-rules=MAP host.docker.internal 127.0.0.1'],
    },
  },
  projects: [
    ...baseConfig.projects!,
    {
      // Each spec loads its own scenario (e2e/teststack/scenario.ts), so these only run here.
      name: 'scenarios',
      testMatch: ['**/teststack/*.spec.ts'],
      dependencies: ['e2e tests logged in', 'e2e tests without logged in'],
    },
  ],
  webServer: undefined,
});
