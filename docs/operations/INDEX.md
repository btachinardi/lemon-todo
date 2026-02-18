# Operations

> Guides for developing, deploying, releasing, and maintaining LemonDo.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [development.md](./development.md) | Prerequisites, quick start, developer CLI reference, test accounts, database migrations, visual regression testing, and troubleshooting | Active |
| [deployment.md](./deployment.md) | CI/CD pipeline, Azure infrastructure (Terraform stages), Docker, observability, security posture, and production URLs | Active |
| [releasing.md](./releasing.md) | Gitflow release process: branch creation, version bumping, CHANGELOG maintenance, verification gate, tagging, and back-merge | Active |
| [research.md](./research.md) | Technology research: version lock summary, compatibility matrix, and in-depth notes on every library in the stack including backend, frontend, testing, and infrastructure | Active |

---

## Summary

**Development** covers the daily workflow. Clone the repo, run `./dev install`, then `./dev start` to bring up the full stack via Aspire. All common operations — building, testing, linting, migrating, and managing Docker containers — are available through the `./dev` CLI script. Three dev accounts are seeded automatically in Development mode (User, Admin, SystemAdmin). The default database is SQLite; SQL Server is available for production-parity testing via `./dev docker up`. Visual regression snapshots are committed to git and must be regenerated when UI changes are intentional.

**Deployment** documents the CI/CD pipeline and Azure infrastructure. GitHub Actions runs backend tests (SQLite + SQL Server), frontend tests, Docker build, and deploys on merge to `main`. The infrastructure uses three progressive Terraform stages (MVP at ~$18/mo, Resilience at ~$180/mo, Scale at ~$1.7K/mo). The production deployment uses Azure Container Apps for the API and Azure Static Web Apps for the frontend, both behind custom domains with managed TLS certificates. Observability is provided by Application Insights, Log Analytics, and Serilog with correlation IDs.

**Releasing** describes the gitflow-based release process. Releases branch from `develop`, bump versions in `src/Directory.Build.props` and `src/client/package.json`, update CHANGELOG.md, run the verification gate (build + generate + tests + lint), merge to `main` with an annotated tag, then back-merge to `develop`. Semantic versioning (SemVer 2.0) governs version numbers: MAJOR for breaking API changes, MINOR for backward-compatible features, PATCH for bug fixes.

**Research** captures the technology decisions made before and during development. It documents the current version lock for every dependency in the stack (backend, frontend, testing, infrastructure, CI/CD) and includes a compatibility matrix. A dedicated section covers the OpenAPI-based TypeScript type generation pipeline (`Microsoft.Extensions.ApiDescription.Server` + `openapi-typescript`) including the schema transformer strategy for enum enrichment and the translation guard test approach.
