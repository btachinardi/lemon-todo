# ADR-004: Authentication & Security

> **Source**: Extracted from docs/architecture/decisions/trade-offs.md §Authentication & Security
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Authentication & Security Trade-offs

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Identity vs Domain User separation** | Two tables (AspNetUsers for credentials, Users for profile) | Single ApplicationUser with all data | ASP.NET Identity designed for auth only; overloading with profile data violates SRP; separation enables domain User to evolve independently and simplifies protected data handling |
| **User entity shape** | Stores redacted strings (`RedactedEmail: string`) | Store Email/DisplayName VOs on entity | Storing `"j***@example.com"` in an `Email` VO creates semantic confusion; VOs used for validation during `Create()`, then `.Redacted` values extracted and stored |
| **Protected data storage strategy** | Redacted (display), hashed (lookup), encrypted (truth) via shadow properties | Single plaintext column or fully encrypted | Three-form strategy: redacted for UI/logs (no decryption overhead), hash for exact-match searches (O(1) lookup), encrypted as source of truth (audited decryption only when needed) |
| **Email hash location** | Identity.UserName (repurposed existing field) | New EmailHash column on AspNetUsers | Identity already has UserName indexed; FindByNameAsync(emailHash) works out-of-box; avoids schema change to Identity table |
| **Admin search paradigm** | Exact email (hash match) or partial redacted name | Partial plaintext email search | Hash-based lookup prevents plaintext protected data in queries; exact email still works for support scenarios; redacted name search covers "I remember the name started with J" cases |
| **UserRepository encryption transparency** | `AddAsync(user, email, displayName)` receives VOs, encrypts internally | Domain handles encryption explicitly | Repository isolates encryption details from domain; domain User entity has no knowledge of hash/encrypt operations; preserves domain purity |
| **IProtectedData as domain concern** | Marker interface on VOs with Redacted property | Infrastructure/Application layer "knows" which fields are sensitive | Protected data awareness centralized at domain VO level; impossible to forget redaction when adding new protected data fields; domain defines the policy, infrastructure implements it |
| **Token storage** | HttpOnly cookie (refresh) + JS memory (access) | localStorage for both tokens | XSS can read localStorage but not HttpOnly cookies; memory-only access token is invisible to injected scripts |
| **Cookie scope** | `SameSite=Strict`, `Path=/api/auth` | `SameSite=Lax` or broader path | Strict + narrow path means cookie is only sent on same-site requests to auth endpoints; eliminates CSRF without CSRF tokens |
| **CSRF protection** | None (SameSite=Strict is sufficient) | Explicit CSRF tokens | SameSite=Strict prevents cross-origin cookie transmission; path-scoping prevents same-origin leakage to non-auth endpoints; adding CSRF tokens would be redundant complexity |
| **Session restoration** | Silent refresh on page load via cookie | Persisted access token in sessionStorage | sessionStorage is also XSS-readable; silent refresh adds ~100ms on page load but eliminates all client-side token storage |
| **Zustand persistence** | Removed entirely (memory-only store) | localStorage persistence with `skipHydration` workaround | Eliminating persist also eliminated the Zustand 5 + React 19 hydration race condition; simpler code, better security |
| **Protected data in logs** | Masked emails (`u***@example.com`) | Full emails for easier debugging | Protected data in logs violates GDPR/HIPAA; masked format preserves enough info for debugging (first char + domain) |
| **Token family detection** | Deferred | Detect stolen refresh token reuse | Requires DB migration (FamilyId column) and complex revocation logic; current single-device model limits attack surface |
| **HaveIBeenPwned check** | Deferred | Reject breached passwords on registration | External API dependency needs graceful degradation; can be added independently later |
| **Refresh token cleanup** | Background service (every 6 hours) | Manual cleanup or no cleanup | Prevents unbounded table growth; 6h interval balances DB load vs staleness |
| **Protected data reveal: justification** | Required reason enum + optional comments | Free-text-only justification | Structured enum enables compliance reporting and analytics; "Other" with required details covers edge cases; optional comments field adds context without blocking |
| **Protected data reveal: re-auth** | Password re-entry | MFA step-up (TOTP/WebAuthn) | MFA not yet implemented; password re-auth provides "something you know" as second factor beyond session cookie; MFA step-up planned for future enhancement |
| **Protected data reveal: timer** | 30s hardcoded, client-side only | Configurable timer / server-enforced TTL | Security policy should not be user-adjustable; backend is stateless (returns data once); server-enforced TTL would require time-limited encrypted tokens — deferred |
| **Task titles as PHI** | Strip from audit logs entirely | Hash or redact task titles in audit | Hashing is not reversible for audit review; redaction patterns are fragile and still leak partial info; task ID in audit entry allows authorized lookup if needed |
| **Tags as PHI** | Not treated as PHI | Encrypt or redact tags | Tags are categorical labels, not personally identifying; a tag like "medical" doesn't identify a person — it's the association with a user's task that creates PHI, already protected behind auth |
| **Protected data decryption paths** | Single IProtectedDataAccessService with two methods (system + admin) | Multiple services or direct encryption service calls | Centralized service ensures ALL decryption is audited; two methods separate system operations (transactional emails) from admin operations (break-the-glass) with different audit detail requirements |
