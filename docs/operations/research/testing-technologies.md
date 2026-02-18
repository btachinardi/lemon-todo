# Testing Technologies

> **Source**: Extracted from docs/operations/research.md §3
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date**: 2026-02-13
> **Purpose**: Document the testing tools, cross-browser strategy, and visual regression approach for the LemonDo stack.

---

## 3. Testing Technologies

### 3.1 Backend Testing

| Tool | Version | Purpose |
|------|---------|---------|
| MSTest 4 + MTP | 4.0.1 | Unit + integration testing (first-party) |
| FsCheck | 3.3.2 | Property-based testing (core API) |
| Microsoft.AspNetCore.Mvc.Testing | .NET 10 | Integration test host |
| Microsoft.EntityFrameworkCore.InMemory | .NET 10 | In-memory DB for tests |

### 3.2 Frontend Testing

| Tool | Version | Purpose |
|------|---------|---------|
| Vitest | 4.x (Vite 7 compatible) | Unit + component testing |
| @testing-library/react | 16.x | React component testing |
| fast-check | 4.x | Property-based testing (JS) |
| MSW (Mock Service Worker) | 2.x | API mocking in tests |

### 3.3 E2E & Cross-Browser Testing

| Tool | Version | Purpose |
|------|---------|---------|
| Playwright | 1.58.0 (.NET) | Cross-browser E2E testing |
| @playwright/test | 1.x | Frontend E2E via Node |
| BrowserStack | Cloud service | Real device/browser testing |

**Cross-Browser Testing Strategy**:

Playwright natively supports three rendering engines with a single API:
- **Chromium** - Chrome, Edge, Opera, Brave, and all Chromium-based browsers
- **Firefox** - Gecko engine
- **WebKit** - Safari engine (derived from latest WebKit trunk, often ahead of shipping Safari)

Playwright projects configuration runs the same test suite against all three engines in CI. This covers the vast majority of desktop browser rendering differences.

**Device Emulation**:

Playwright includes predefined device descriptors (iPhone 14, Pixel 7, iPad, etc.) that configure viewport, user agent, touch events, and device scale factor. This validates:
- Responsive breakpoints (mobile, tablet, desktop)
- Touch interaction behavior
- Viewport-dependent layout and overflow

**Limitation**: Emulation is not real-device testing. It simulates viewport and user agent but runs in the same desktop engine. Real Safari on iOS has rendering quirks that WebKit emulation may not catch.

**Real Device Testing (Production)**:

BrowserStack provides 3500+ real browsers and devices in the cloud. Playwright tests can run directly on BrowserStack via their integration - same test code, real hardware:
- Real iOS Safari on physical iPhones/iPads
- Real Android Chrome on physical devices
- Older browser versions (Safari 15, Chrome 100, Firefox ESR)
- Real OS-level rendering (font smoothing, scrollbar behavior, safe areas)

**Phasing**:
- **CP5**: Playwright E2E with Chromium + Firefox + WebKit projects + device emulation (iPhone, iPad, Pixel). Covers ~95% of rendering scenarios.
- **Production**: BrowserStack for real device matrix. Run on every release candidate. Focus on iOS Safari (historically most quirky) and older Android versions.

### 3.4 Visual Regression Testing

| Tool | Version | Purpose |
|------|---------|---------|
| Playwright screenshots | Built-in | Baseline visual comparison |
| Percy (BrowserStack) | Cloud service | AI-powered cross-browser visual diffs |

**Strategy**:

- **CP5**: Playwright's built-in `toHaveScreenshot()` for baseline visual comparison. Captures screenshots during E2E runs, compares against committed baselines, fails on pixel-level drift. Free, no external service, works in CI.
- **Production**: Percy (by BrowserStack) or Chromatic for AI-powered visual regression. These render snapshots across real browser engines (not emulated), detect meaningful visual changes vs noise (anti-aliasing, font rendering), and provide team review workflows with approval flows.

**What Visual Regression Catches**:
- CSS regressions (margin collapse, flexbox/grid issues across browsers)
- Font rendering differences across OS (Windows ClearType vs macOS subpixel)
- Theme inconsistencies (dark mode colors, contrast ratios)
- Responsive breakpoint edge cases (content overflow, truncation)
- i18n layout issues (longer text in pt-BR/es breaking layouts)
