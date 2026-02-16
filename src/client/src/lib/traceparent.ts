/** Generates a random hex string of the given byte length. */
function randomHex(byteLength: number): string {
  const bytes = new Uint8Array(byteLength);
  crypto.getRandomValues(bytes);
  return Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('');
}

/**
 * Generates a W3C Trace Context `traceparent` header.
 * Format: `{version}-{trace-id}-{parent-id}-{trace-flags}`
 * @see https://www.w3.org/TR/trace-context/#traceparent-header
 */
export function generateTraceparent(): string {
  const version = '00';
  const traceId = randomHex(16); // 16 bytes = 32 hex chars
  const parentId = randomHex(8); // 8 bytes = 16 hex chars
  const traceFlags = '01'; // sampled
  return `${version}-${traceId}-${parentId}-${traceFlags}`;
}
