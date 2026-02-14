import { defineConfig } from '@playwright/test';

const API_PORT = 5155;
const CLIENT_PORT = 5173;

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
      command: `dotnet run --project ../../src/LemonDo.Api --launch-profile http`,
      url: `http://localhost:${API_PORT}/health`,
      reuseExistingServer: !process.env.CI,
      timeout: 30_000,
      env: {
        ConnectionStrings__DefaultConnection: 'Data Source=lemondo-e2e.db',
      },
    },
    {
      command: `pnpm dev`,
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
