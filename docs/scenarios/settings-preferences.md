# Settings & Preferences Scenarios

> **Source**: Extracted from docs/SCENARIOS.md §5
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Scenario S07: Theme and Language Switching

**Context**: A user in Brazil wants the app in Portuguese with dark mode.

```
Step 1: User opens Settings
  -> Settings page with sections: Profile, Appearance, Language, Notifications
  [analytics: settings_opened]

Step 2: User switches to Dark mode
  -> Toggle switch: Light | Dark | System
  -> Instant theme change with smooth transition
  -> All components properly themed (no white flashes)
  [analytics: theme_toggled, to: dark]

Step 3: User changes language to Portuguese
  -> Language dropdown with: English, Portugues, Espanol
  -> On selection, ALL UI text updates immediately (no page reload)
  -> Dates, numbers format to pt-BR locale
  [analytics: language_changed, from: en, to: pt-BR]
```

---

## Scenario S09: Multi-Factor Authentication Setup

**Context**: Marcus's company requires MFA for all accounts.

```
Step 1: Marcus goes to Settings > Security
  -> "Two-Factor Authentication" section with "Enable" button
  [analytics: settings_security_opened]

Step 2: Marcus enables MFA
  -> QR code displayed for authenticator app
  -> Manual key also shown for copy-paste
  -> Input field to verify TOTP code
  [analytics: mfa_setup_started]

Step 3: Marcus scans QR code and enters verification code
  -> Code verified -> MFA enabled
  -> Backup codes generated and shown ONCE
  -> Prompt to save/print backup codes
  [analytics: mfa_enabled]

Step 4: Next login requires MFA
  -> Email + password entered
  -> Second screen: "Enter your 2FA code"
  -> Code from authenticator app accepted
  -> Login complete
  [analytics: login_completed, mfa: true]
```
