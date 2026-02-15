/**
 * E2E pre-test cleanup script.
 *
 * Runs BEFORE Playwright starts to ensure a clean environment:
 * 1. Kills any stale processes on E2E ports (API + Vite)
 * 2. Deletes the E2E SQLite database files
 *
 * This prevents Playwright's `reuseExistingServer` from picking up
 * stale servers with wrong config or stale data.
 */
import fs from 'node:fs';
import path from 'node:path';
import { execSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const API_DIR = path.resolve(__dirname, '../../../src/LemonDo.Api');
const DB_NAME = 'lemondo-e2e.db';

// Must match helpers/e2e.config.ts — duplicated here because .mjs can't import .ts
const API_PORT = 54155;
const CLIENT_PORT = 54173;

/**
 * Kills any process listening on the given port.
 * Returns true if a process was found and killed.
 */
function killProcessOnPort(port) {
  try {
    if (process.platform === 'win32') {
      const result = execSync(
        `netstat -ano | findstr ":${port} " | findstr LISTENING`,
        { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] },
      );
      const lines = result.trim().split('\n').filter(Boolean);
      const pids = [
        ...new Set(
          lines.map((line) => line.trim().split(/\s+/).pop()).filter(Boolean),
        ),
      ];
      for (const pid of pids) {
        try {
          execSync(`taskkill /F /PID ${pid}`, { stdio: 'pipe' });
          console.log(
            `[e2e-cleanup] Killed stale process PID ${pid} on port ${port}`,
          );
        } catch {
          /* already dead */
        }
      }
      return pids.length > 0;
    } else {
      const result = execSync(`lsof -ti :${port}`, {
        encoding: 'utf-8',
        stdio: ['pipe', 'pipe', 'pipe'],
      });
      const pids = result.trim().split('\n').filter(Boolean);
      for (const pid of pids) {
        try {
          execSync(`kill -9 ${pid}`, { stdio: 'pipe' });
          console.log(
            `[e2e-cleanup] Killed stale process PID ${pid} on port ${port}`,
          );
        } catch {
          /* already dead */
        }
      }
      return pids.length > 0;
    }
  } catch {
    // No process listening on port — nothing to kill
    return false;
  }
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function main() {
  console.log('[e2e-cleanup] Cleaning E2E environment...');

  // 1. Kill any stale processes on E2E ports
  const killedApi = killProcessOnPort(API_PORT);
  const killedClient = killProcessOnPort(CLIENT_PORT);

  if (killedApi || killedClient) {
    // Wait for processes to fully terminate and release file locks
    await sleep(2000);
  }

  // 2. Delete E2E SQLite database files
  let deletedAny = false;
  for (const suffix of ['', '-shm', '-wal']) {
    const filePath = path.join(API_DIR, `${DB_NAME}${suffix}`);
    if (fs.existsSync(filePath)) {
      try {
        fs.unlinkSync(filePath);
        console.log(`[e2e-cleanup] Deleted ${path.basename(filePath)}`);
        deletedAny = true;
      } catch (err) {
        console.warn(
          `[e2e-cleanup] WARNING: Could not delete ${path.basename(filePath)}: ${err.message}`,
        );
      }
    }
  }

  if (!killedApi && !killedClient && !deletedAny) {
    console.log('[e2e-cleanup] Environment already clean.');
  } else {
    console.log('[e2e-cleanup] Done.');
  }
}

main();
