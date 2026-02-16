import { describe, it, expect } from 'vitest';
import { generateTraceparent } from '../traceparent';

/**
 * W3C Trace Context `traceparent` header format:
 * `{version(2)}-{trace-id(32)}-{parent-id(16)}-{trace-flags(2)}`
 * Total: 55 characters with dashes.
 *
 * @see https://www.w3.org/TR/trace-context/#traceparent-header
 */
const TRACEPARENT_REGEX = /^00-[0-9a-f]{32}-[0-9a-f]{16}-01$/;

describe('generateTraceparent', () => {
  it('should produce a valid W3C traceparent string', () => {
    const tp = generateTraceparent();
    expect(tp).toMatch(TRACEPARENT_REGEX);
  });

  it('should be exactly 55 characters long', () => {
    const tp = generateTraceparent();
    expect(tp).toHaveLength(55);
  });

  it('should generate unique trace IDs on each call', () => {
    const values = new Set(Array.from({ length: 20 }, () => generateTraceparent()));
    expect(values.size).toBe(20);
  });
});
