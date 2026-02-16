import { describe, expect, it } from 'vitest';
import en from './en.json';
import ptBR from './pt-BR.json';

const locales: Record<string, Record<string, unknown>> = {
  en,
  'pt-BR': ptBR,
};

function flattenKeys(obj: Record<string, unknown>, prefix = ''): string[] {
  return Object.entries(obj).flatMap(([key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key;
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      return flattenKeys(value as Record<string, unknown>, path);
    }
    return [path];
  });
}

describe('locale completeness', () => {
  const keysByLocale = Object.fromEntries(
    Object.entries(locales).map(([name, data]) => [name, new Set(flattenKeys(data))]),
  );

  const localeNames = Object.keys(locales);

  for (const locale of localeNames) {
    for (const other of localeNames) {
      if (locale === other) continue;

      it(`should have all keys from ${other} in ${locale}`, () => {
        const missing = [...keysByLocale[other]].filter((key) => !keysByLocale[locale].has(key));

        expect(missing, `${locale} is missing keys that exist in ${other}`).toEqual([]);
      });
    }
  }
});
