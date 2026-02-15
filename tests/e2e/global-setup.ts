import fs from 'node:fs';
import path from 'node:path';

const API_DIR = path.resolve(import.meta.dirname, '../../src/LemonDo.Api');
const DB_NAME = 'lemondo-e2e.db';

/**
 * Playwright globalSetup — runs after webServer starts but before tests.
 *
 * Attempts to delete E2E database files as a safety net.
 * Primary cleanup (process killing + DB deletion) is handled by
 * scripts/cleanup.mjs which runs BEFORE Playwright via the npm script.
 */
export default function globalSetup() {
  for (const suffix of ['', '-shm', '-wal']) {
    const filePath = path.join(API_DIR, `${DB_NAME}${suffix}`);
    if (fs.existsSync(filePath)) {
      try {
        fs.unlinkSync(filePath);
        console.log(`[global-setup] Deleted ${path.basename(filePath)}`);
      } catch {
        // File is locked by the freshly-started webServer — expected
      }
    }
  }
}
