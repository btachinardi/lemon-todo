# Release Process

> **Source**: Extracted from docs/RELEASING.md
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> How to cut a release for LemonDo using gitflow and semantic versioning.

---

## Prerequisites

- All work for the release is merged into `develop`
- All tests pass on `develop`
- You know the target version number (follow [SemVer 2.0](https://semver.org/))

## Version Numbering

| Stage | Format | Example |
|-------|--------|---------|
| Pre-1.0 development | `0.MINOR.PATCH` | `0.1.0`, `0.2.0` |
| Stable release | `MAJOR.MINOR.PATCH` | `1.0.0`, `1.1.0` |
| Pre-release | `VERSION-LABEL.N` | `1.0.0-beta.1` |

**When to bump what:**
- **MAJOR**: Breaking API changes (response shape, removed endpoints)
- **MINOR**: New features, backward-compatible (new endpoints, new UI pages)
- **PATCH**: Bug fixes, backward-compatible

## Step-by-Step Process

### 1. Create release branch from develop

```bash
git checkout develop
git pull origin develop
git checkout -b release/X.Y.Z develop
```

### 2. Bump versions

**Backend** — Edit `src/Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Version>X.Y.Z</Version>
    <AssemblyVersion>X.Y.Z.0</AssemblyVersion>
    <FileVersion>X.Y.Z.0</FileVersion>
    <InformationalVersion>X.Y.Z</InformationalVersion>
  </PropertyGroup>
</Project>
```

**Frontend** — Edit `src/client/package.json`:

```json
"version": "X.Y.Z"
```

### 3. Update CHANGELOG.md

Move items from `[Unreleased]` to a new `[X.Y.Z] - YYYY-MM-DD` section. Group entries into:

- **Added** — New features
- **Changed** — Changes in existing functionality
- **Deprecated** — Soon-to-be removed features
- **Removed** — Removed features
- **Fixed** — Bug fixes
- **Security** — Vulnerability fixes

Update the comparison links at the bottom of the file.

### 4. Update documentation

- `TASKS.md`: Add Decision Log entry, update Progress Summary
- `docs/journal/v1.md`: Add release section with version strategy notes

### 5. Commit release preparation

```bash
git add CHANGELOG.md src/Directory.Build.props src/client/package.json TASKS.md docs/journal/v1.md
git commit -m "chore(release): prepare vX.Y.Z"
```

### 6. Run verification gate

All checks must pass before merging:

```bash
# Backend (clean build to surface all warnings)
dotnet clean src/LemonDo.slnx -v quiet && dotnet build src/LemonDo.slnx

# Frontend
cd src/client && pnpm build

# Backend tests
dotnet test --solution src/LemonDo.slnx

# Frontend tests
cd src/client && pnpm test

# Frontend lint
cd src/client && pnpm lint
```

Expected results:
- 0 build warnings, 0 errors (backend)
- Frontend build succeeds
- All backend tests pass
- All frontend tests pass
- Lint clean

### 7. Merge to main and tag

```bash
git checkout main
git pull origin main
git merge --no-ff release/X.Y.Z -m "release: vX.Y.Z — <Release Title>"
git tag -a vX.Y.Z -m "vX.Y.Z — <Release Title>"
```

### 8. Back-merge to develop

```bash
git checkout develop
git merge --no-ff release/X.Y.Z -m "merge: back-merge release/X.Y.Z into develop"
```

### 9. Push and clean up

```bash
git push origin main develop --tags
git branch -d release/X.Y.Z
```

### 10. Create GitHub release (optional)

```bash
gh release create vX.Y.Z --title "vX.Y.Z — <Release Title>" --notes-file CHANGELOG.md
```

## Version Display

- **Frontend**: Version is displayed in the bottom-right corner of the dashboard layout, read from `package.json` via Vite's `define` config.
- **Backend**: API logs the assembly `InformationalVersion` on startup.
- **.NET assemblies**: All 6 source projects inherit version from `src/Directory.Build.props`. Test projects are intentionally excluded.

## Verification Checklist

- [ ] Version bumped in `src/Directory.Build.props`
- [ ] Version bumped in `src/client/package.json`
- [ ] CHANGELOG.md updated with release entries
- [ ] Backend build: 0 warnings, 0 errors
- [ ] Frontend build: succeeds
- [ ] All backend tests pass
- [ ] All frontend tests pass
- [ ] Frontend lint: clean
- [ ] Merged to `main` with `--no-ff`
- [ ] Annotated tag created
- [ ] Back-merged to `develop`
- [ ] Pushed to origin (main + develop + tag)
- [ ] Release branch deleted
