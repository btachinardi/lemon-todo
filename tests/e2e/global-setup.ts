import fs from 'node:fs';
import path from 'node:path';

const API_DIR = path.resolve(import.meta.dirname, '../../src/LemonDo.Api');
const DB_NAME = 'lemondo-e2e.db';

export default function globalSetup() {
  // Delete the E2E SQLite database so the API recreates it fresh via MigrateAsync()
  for (const suffix of ['', '-shm', '-wal']) {
    const filePath = path.join(API_DIR, `${DB_NAME}${suffix}`);
    if (fs.existsSync(filePath)) {
      fs.unlinkSync(filePath);
      console.log(`Deleted ${filePath}`);
    }
  }
}
