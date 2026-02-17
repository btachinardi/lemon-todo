import { describe, expect, it } from 'vitest';
import spec from '../../openapi.json';
import en from './en.json';
import es from './es.json';
import ptBR from './pt-BR.json';

/**
 * Enum translation coverage guard.
 *
 * Ensures every backend enum value present in the committed OpenAPI spec
 * has a corresponding i18n key in all supported locales. If a new enum
 * value is added to the backend and `./dev generate` updates the spec,
 * this test fails until translations are provided.
 */

// Extract enum arrays from the OpenAPI spec
const auditActions: string[] =
  spec.components.schemas.AuditAction.enum;
const priorities: string[] =
  spec.components.schemas.TaskDto.properties.priority.enum;
const taskStatuses: string[] =
  spec.components.schemas.TaskDto.properties.status.enum;

const locales = {
  en,
  es,
  'pt-BR': ptBR,
} as const;

/** Lowercase the first character: `InProgress` → `inProgress`, `None` → `none`. */
function lcFirst(s: string): string {
  return s.charAt(0).toLowerCase() + s.slice(1);
}

/** Resolve a dot-separated path in a nested object. */
function resolve(obj: Record<string, unknown>, path: string): unknown {
  return path.split('.').reduce<unknown>((acc, key) => {
    if (acc && typeof acc === 'object' && !Array.isArray(acc)) {
      return (acc as Record<string, unknown>)[key];
    }
    return undefined;
  }, obj);
}

describe('enum translation coverage', () => {
  for (const [locale, translations] of Object.entries(locales)) {
    describe(locale, () => {
      it('should have translations for all AuditAction values', () => {
        const missing = auditActions.filter(
          (action) => !resolve(translations, `admin.audit.actions.${action}`),
        );
        expect(
          missing,
          `${locale} is missing audit action translations`,
        ).toEqual([]);
      });

      it('should have translations for all Priority values', () => {
        const missing = priorities.filter(
          (p) => !resolve(translations, `tasks.priority.${lcFirst(p)}`),
        );
        expect(
          missing,
          `${locale} is missing priority translations`,
        ).toEqual([]);
      });

      it('should have translations for all TaskStatus values', () => {
        const missing = taskStatuses.filter(
          (s) => !resolve(translations, `tasks.status.${lcFirst(s)}`),
        );
        expect(
          missing,
          `${locale} is missing task status translations`,
        ).toEqual([]);
      });
    });
  }
});
