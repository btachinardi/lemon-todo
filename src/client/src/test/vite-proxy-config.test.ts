import { readFileSync } from 'fs';
import path from 'path';
import { describe, it, expect } from 'vitest';

describe('Vite proxy configuration', () => {
  it('should use a fallback URL that matches the API http launch profile', () => {
    // Read the API launch settings to get the actual URL
    const launchSettingsPath = path.resolve(
      __dirname,
      '../../../../src/LemonDo.Api/Properties/launchSettings.json',
    );
    const raw = readFileSync(launchSettingsPath, 'utf-8').replace(/^\uFEFF/, '');
    const launchSettings = JSON.parse(raw);
    const httpProfileUrl: string = launchSettings.profiles.http.applicationUrl;

    // Read vite.config.ts and extract the proxy fallback URL
    const viteConfigPath = path.resolve(__dirname, '../../vite.config.ts');
    const viteConfigSource = readFileSync(viteConfigPath, 'utf-8');

    // The proxy target line looks like:
    //   target: process.env.services__api__https__0 || 'https://localhost:5001',
    const fallbackMatch = viteConfigSource.match(
      /services__api__https__0\s*\|\|\s*['"]([^'"]+)['"]/,
    );
    expect(fallbackMatch).not.toBeNull();

    const fallbackUrl = fallbackMatch![1];
    expect(fallbackUrl).toBe(httpProfileUrl);
  });
});
