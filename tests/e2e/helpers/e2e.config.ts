/**
 * E2E test configuration constants sourced from `e2e.config.json`.
 *
 * Values are set by the test runner scripts and match the ports used by the
 * `webServer` entries in `playwright.config.ts`. Do not hardcode ports in
 * test files — always import from here.
 *
 * @module
 */

import fs from 'node:fs';
import path from 'node:path';

const config = JSON.parse(
  fs.readFileSync(path.resolve(import.meta.dirname, '../e2e.config.json'), 'utf-8'),
) as { apiPort: number; clientPort: number; dbName: string };

/** Port the API server listens on during E2E runs. */
export const API_PORT = config.apiPort;

/** Port the Vite dev server listens on during E2E runs. */
export const CLIENT_PORT = config.clientPort;

/** SQLite database filename used by the API during E2E runs (e.g. `lemondo-e2e.db`). */
export const DB_NAME = config.dbName;

/** Base URL for direct API calls from helpers (e.g. `http://localhost:5155/api`). */
export const API_BASE = `http://localhost:${API_PORT}/api`;
