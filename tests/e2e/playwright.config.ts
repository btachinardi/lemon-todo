import fs from 'node:fs';
import path from 'node:path';
import { defineConfig } from '@playwright/test';

const config = JSON.parse(
  fs.readFileSync(path.resolve(import.meta.dirname, 'e2e.config.json'), 'utf-8'),
) as { apiPort: number; clientPort: number; dbName: string };

const API_PORT = config.apiPort;
const CLIENT_PORT = config.clientPort;

export default defineConfig({
  testDir: './specs',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: 'html',
  timeout: 30_000,

  use: {
    baseURL: `http://localhost:${CLIENT_PORT}`,
    trace: 'on-first-retry',
  },

  globalSetup: './global-setup.ts',

  projects: [
    {
      name: 'chromium',
      use: { browserName: 'chromium' },
    },
  ],

  webServer: [
    {
      command: `dotnet run --project ../../src/LemonDo.Api --no-launch-profile`,
      url: `http://localhost:${API_PORT}/health`,
      reuseExistingServer: !process.env.CI,
      timeout: 30_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: `http://localhost:${API_PORT}`,
        ConnectionStrings__DefaultConnection: `Data Source=${config.dbName}`,
        RateLimiting__Auth__PermitLimit: '10000',
      },
    },
    {
      command: `pnpm dev --port ${CLIENT_PORT}`,
      cwd: '../../src/client',
      url: `http://localhost:${CLIENT_PORT}`,
      reuseExistingServer: !process.env.CI,
      timeout: 15_000,
      env: {
        services__api__https__0: `http://localhost:${API_PORT}`,
      },
    },
  ],
});
