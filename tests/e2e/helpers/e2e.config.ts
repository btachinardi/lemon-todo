import fs from 'node:fs';
import path from 'node:path';

const config = JSON.parse(
  fs.readFileSync(path.resolve(import.meta.dirname, '../e2e.config.json'), 'utf-8'),
) as { apiPort: number; clientPort: number; dbName: string };

/** E2E test configuration constants — sourced from e2e.config.json. */
export const API_PORT = config.apiPort;
export const CLIENT_PORT = config.clientPort;
export const DB_NAME = config.dbName;
export const API_BASE = `http://localhost:${API_PORT}/api`;
