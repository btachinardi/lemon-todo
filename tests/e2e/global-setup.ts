import fs from 'node:fs';
import path from 'node:path';

const API_DIR = path.resolve(import.meta.dirname, '../../src/LemonDo.Api');
const DB_NAME = 'lemondo-e2e.db';

export default function globalSetup() {
  // Delete the E2E SQLite database so the API recreates it fresh via MigrateAsync()
  for (const suffix of ['', '-shm', '-wal']) {
    const filePath = path.join(API_DIR, `${DB_NAME}${suffix}`);
    if (fs.existsSync(filePath)) {
      try {
        fs.unlinkSync(filePath);
        console.log(`Deleted ${filePath}`);
      } catch {
        // File may be locked by the webServer process that started before globalSetup.
        // The API will recreate it via MigrateAsync() on startup anyway.
        console.log(`Skipped ${filePath} (locked by running process)`);
      }
    }
  }
}
