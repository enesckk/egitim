import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e/fullstack',
  fullyParallel: false,
  workers: 1, // Full-stack stateful tests run serially
  retries: 0,
  reporter: 'list',
  timeout: 60 * 1000,
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: '/opt/homebrew/bin/npx vite --port 3000 --no-open',
    url: 'http://localhost:3000',
    reuseExistingServer: true,
    timeout: 30 * 1000,
    env: {
      PATH: `/opt/homebrew/bin:${process.env.PATH || ''}`,
      VITE_API_BASE_URL: 'http://localhost:5005',
    },
  },
});
