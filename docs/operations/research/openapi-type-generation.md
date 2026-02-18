# OpenAPI Type Generation

> **Source**: Extracted from docs/operations/research.md §7
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date**: 2026-02-17
> **Context**: Backend C# enum/DTO changes don't propagate to the frontend automatically. 9 hand-written TypeScript type files manually mirror backend DTOs and can drift silently. Goal: generate TypeScript types from the backend's OpenAPI spec.

---

## 7. OpenAPI Type Generation Research

### 7.1 Microsoft.Extensions.ApiDescription.Server (Build-Time OpenAPI)

- **Version**: 10.0.3 (matches ASP.NET Core 10.0.3)
- **Purpose**: Generates OpenAPI JSON document during `dotnet build` without running the app at runtime
- **How It Works**: Launches app entry point with a mock server (`GetDocument.Insider` assembly) during build. All startup code executes, so DB migration/seeding must be guarded with `Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider"` check.
- **MSBuild Properties**:
  - `OpenApiGenerateDocuments` = `true` — enables build-time generation
  - `OpenApiDocumentsDirectory` — output dir, resolved relative to project file
  - `OpenApiGenerateDocumentsOptions` — CLI args like `--file-name openapi`
- **Default Output**: `{ProjectName}.json` in the `obj/` directory
- **Custom Output**: Set `OpenApiDocumentsDirectory=../client` + `--file-name openapi` to output `src/client/openapi.json`
- **Source**: [Microsoft Learn — Generate OpenAPI documents](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0), [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.ApiDescription.Server)

### 7.2 Schema Transformers (ASP.NET Core 10 OpenAPI)

- **Purpose**: Modify generated OpenAPI schemas before document emission
- **Three Transformer Types**: Document, Operation, Schema
- **Schema Transformer API**: `options.AddSchemaTransformer((schema, context, ct) => { ... })` — called for each schema type
- **Context Properties**: `context.JsonTypeInfo.Type` gives the CLR type, `context.JsonTypeInfo` gives full JSON serialization metadata
- **Execution Order**: Schema transformers → Operation transformers → Document transformers
- **Use Case**: Enrich `string`-typed DTO properties (Priority, TaskStatus, NotificationType) with enum constraints. These enums are converted to strings via `.ToString()` in mappers, so the OpenAPI generator sees `string` not the enum type. A document transformer walks `components.schemas` and adds `enum` arrays to the relevant properties.
- **AuditAction**: Already has `[JsonConverter(typeof(JsonStringEnumConverter))]` so it appears as a proper enum schema automatically.
- **Source**: [Microsoft Learn — Customize OpenAPI documents](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0)

### 7.3 openapi-typescript (NPM)

- **Version**: 7.13.0 (latest stable, Feb 2026)
- **Purpose**: Generate TypeScript type definitions from OpenAPI 3.0/3.1 specs
- **CLI**: `npx openapi-typescript <input> -o <output>` — reads JSON/YAML, outputs `.d.ts` file
- **Output Format**: Generates `paths`, `components`, `operations` namespaces. Types are accessed via `components["schemas"]["TaskDto"]`.
- **Key Flags**:
  - `--output` (`-o`): Output file path
  - `--enum`: Generate true TS enums instead of string unions (default: false — string unions preferred)
  - `--export-type` (`-t`): Use `type` instead of `interface` (default: false)
  - `--root-types`: Export component schemas as root-level aliases
  - `--root-types-no-schema-prefix`: Omit "Schema" prefix from root type names
  - `--immutable`: Generate readonly properties and arrays
  - `--alphabetize`: Sort types alphabetically
  - `--check`: Verify generated types are current (CI validation)
- **TypeScript Requirements**: `"module": "ESNext"` or `"NodeNext"`, `"moduleResolution": "Bundler"` or `"NodeNext"` (already satisfied by our tsconfig)
- **Generated Type Access Pattern**: `import type { components } from './schema'; type TaskDto = components["schemas"]["TaskDto"];`
- **Source**: [openapi-ts.dev](https://openapi-ts.dev/introduction), [npm](https://www.npmjs.com/package/openapi-typescript), [GitHub](https://github.com/openapi-ts/openapi-typescript)

### 7.4 Enum Enrichment Strategy

The LemonDo API serializes enums differently across DTOs:

| Enum | Serialization | OpenAPI Generation |
|------|--------------|-------------------|
| `AuditAction` | `[JsonStringEnumConverter]` attribute on enum → DTO property is `AuditAction` type | Automatic — appears as `{ type: "string", enum: [...] }` |
| `Priority` | `.ToString()` in mapper → DTO property is `string` type | Manual — needs document transformer to add `enum` constraint |
| `TaskStatus` | `.ToString()` in mapper → DTO property is `string` type | Manual — needs document transformer to add `enum` constraint |
| `NotificationType` | `.ToString()` in endpoint → response property is `string` type | Manual — needs document transformer to add `enum` constraint |

**Approach**: Use a document transformer that walks `components.schemas` and enriches specific properties with enum values from the backend enum types. This keeps the backend DTO types unchanged (no breaking API changes).

### 7.5 Translation Guard Test Strategy

- **Mechanism**: Import the committed `openapi.json` directly in a Vitest test
- **Extract**: Enum arrays from schema properties (e.g., `AuditAction`, `Priority`, `TaskStatus`)
- **Assert**: Every enum value has a corresponding key in `en.json`, `es.json`, `pt-BR.json`
- **Benefit**: When a new enum value is added to the backend, `./dev generate` updates `openapi.json`, and the translation guard test fails until the i18n files are updated
- **Covers**: `AuditAction` → `admin.audit.actions.*`, `Priority` → `tasks.priority.*`, `TaskStatus` → `tasks.status.*`
