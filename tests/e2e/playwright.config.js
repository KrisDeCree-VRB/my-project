import { defineConfig, devices } from '@playwright/test';
import { dbPath, PORT } from './support/server.js';

const baseURL = `http://127.0.0.1:${PORT}`;

export default defineConfig({
  testDir: './specs',
  // Every spec creates its own trip and its own browser contexts, so nothing is shared
  // beyond the server and its database — specs can run side by side.
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 2 : undefined,
  reporter: process.env.CI
    ? [['github'], ['html', { open: 'never' }], ['junit', { outputFile: 'test-results/e2e-results.xml' }]]
    : [['list'], ['html', { open: 'never' }]],

  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],

  // A dedicated port and a throwaway database, so the suite never talks to — or wipes —
  // the app a developer already has running on 5202. --artifacts-path keeps the build out
  // of the project's own bin/, which that running app holds a lock on.
  webServer: {
    command: 'dotnet run --project ../.. --no-launch-profile --artifacts-path .playwright/artifacts',
    url: baseURL,
    reuseExistingServer: false,
    timeout: 180_000,
    stdout: 'ignore', // EF Core logs every statement at Information; only failures matter here.
    stderr: 'pipe',
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      Logging__LogLevel__Default: 'Warning',
      ASPNETCORE_URLS: baseURL,
      ConnectionStrings__Default: `Data Source=${dbPath}`,
      // The join page allows 20 lookups per minute per IP in production (NFR-004). Every
      // request here comes from one IP, so the suite would throttle itself.
      RateLimiting__InvitePermitLimit: '10000',
    },
  },

  globalTeardown: './support/global-teardown.js',
});
