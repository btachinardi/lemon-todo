/**
 * IndexedDB-backed offline mutation queue.
 *
 * Stores HTTP mutations (POST/PUT/DELETE) that failed due to network errors.
 * Mutations are replayed in FIFO order when connectivity is restored.
 *
 * Uses raw IndexedDB API to avoid adding a dependency.
 */

const DB_NAME = 'lemondo-offline';
const DB_VERSION = 1;
const STORE_NAME = 'mutations';

/** A single queued mutation waiting to be replayed. */
export interface QueuedMutation {
  /** Auto-generated UUID. */
  id: string;
  /** Unix timestamp (ms) when the mutation was enqueued. */
  timestamp: number;
  /** HTTP method. */
  method: 'POST' | 'PUT' | 'DELETE';
  /** Relative URL (e.g. `/api/tasks/123`). */
  url: string;
  /** JSON-serialized request body (undefined for DELETE). */
  body?: string;
}

/** Opens (or creates) the IndexedDB database. */
function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME, { keyPath: 'id' });
      }
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

/** Wraps an IDBRequest in a Promise. */
function promisify<T>(request: IDBRequest<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

/** Adds a mutation to the queue. Returns the generated ID. */
export async function enqueue(
  method: QueuedMutation['method'],
  url: string,
  body?: unknown,
): Promise<string> {
  const db = await openDb();
  const id = crypto.randomUUID();
  const mutation: QueuedMutation = {
    id,
    timestamp: Date.now(),
    method,
    url,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  };
  const tx = db.transaction(STORE_NAME, 'readwrite');
  tx.objectStore(STORE_NAME).add(mutation);
  await new Promise<void>((resolve, reject) => {
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
  db.close();
  return id;
}

/** Returns all queued mutations sorted by timestamp (FIFO). */
export async function dequeueAll(): Promise<QueuedMutation[]> {
  const db = await openDb();
  const tx = db.transaction(STORE_NAME, 'readonly');
  const all = await promisify(tx.objectStore(STORE_NAME).getAll());
  db.close();
  return all.sort((a, b) => a.timestamp - b.timestamp);
}

/** Removes a single mutation by ID. */
export async function remove(id: string): Promise<void> {
  const db = await openDb();
  const tx = db.transaction(STORE_NAME, 'readwrite');
  tx.objectStore(STORE_NAME).delete(id);
  await new Promise<void>((resolve, reject) => {
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
  db.close();
}

/** Clears all queued mutations. */
export async function clear(): Promise<void> {
  const db = await openDb();
  const tx = db.transaction(STORE_NAME, 'readwrite');
  tx.objectStore(STORE_NAME).clear();
  await new Promise<void>((resolve, reject) => {
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
  db.close();
}

/** Returns the number of pending mutations. */
export async function count(): Promise<number> {
  const db = await openDb();
  const tx = db.transaction(STORE_NAME, 'readonly');
  const result = await promisify(tx.objectStore(STORE_NAME).count());
  db.close();
  return result;
}
