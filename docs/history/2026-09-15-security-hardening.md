# Security Hardening — 2026-09-15

## Summary

The application was hardened across authentication, authorization, guest order capabilities, payment verification, order integrity, input validation, rate limiting, browser policy, file handling, data migration, and account deletion.

## Motivation

The previous audit identified multiple paths to unauthorized administration, payment manipulation, order enumeration, token theft, data loss, and sensitive-data exposure.

## Implementation

### Authentication and accounts

- Moved JWT storage to the one-hour `__Host-waffl_session` HttpOnly/Secure cookie.
- Removed bearer-token and JavaScript JWT handling from active Angular flows.
- Added database-backed token-version and current-role checks on every authenticated request.
- Added `GET /public/session` and server-side logout.
- Normalized emails and added a unique sparse MongoDB index.
- Replaced new password hashes with ASP.NET PasswordHasher while retaining legacy verification compatibility.
- Added random, hashed, expiring, single-use password-reset tokens and expiring verification codes.
- Made account recovery responses resistant to email enumeration.
- Changed account removal to anonymization so order references remain valid while sessions are revoked.

### Authorization and data exposure

- Protected the entire admin controller with the `admin` role.
- Kept destructive seed logic outside HTTP routing.
- Restricted file mutations to administrators and stopped returning internal exception details.
- Replaced profile and admin order mass assignment with explicit update fields.
- Added ownership checks to user order reads and writes.
- Removed order capability hashes from administrator UI output.

### Orders, payments, and inventory

- Centralized registered and guest order creation in `OrderService`.
- Validated customization ownership, uniqueness, reuse, product variants, payment methods, delivery methods, and Packeta fields.
- Added `Customization.IsOrdered`, atomic conditional item claims, conditional stock decrement, and compensating rollback.
- Generated 256-bit cancellation/follow tokens, stored only hashes, and added expiration times.
- Moved capability API calls to POST bodies and browser navigation tokens to URL fragments.
- Built Stripe sessions from the database order total and verified status, currency, order reference, session ID, and amount.
- Added signed Stripe webhook processing and prevented manual administrator confirmation of Stripe payments.

### Platform controls

- Restricted CORS to the configured web origin with credentials.
- Added explicit trusted-proxy configuration without clearing framework trust boundaries.
- Added global and endpoint-specific fixed-window rate limits.
- Added request-size limits, origin checks, HSTS, and API/browser security headers.
- Added Firebase CSP and changed Packeta to on-demand script loading.
- Added DTO validation and bounded collection/string inputs.
- Added startup migration for legacy emails, tokens, expirations, ordered customizations, and indexes.

## Affected contracts

- Login no longer returns a JWT; it sets an HttpOnly cookie and returns UI metadata.
- Authenticated API calls require browser credentials rather than a JavaScript bearer header.
- Guest follow/cancel operations use POST JSON bodies rather than query-string tokens.
- Order creation returns raw cancellation/follow tokens once; stored order records contain hashes.
- Admin order updates accept an explicit update request instead of replacing a full database model.

## Data and deployment impact

- Existing sessions without `TokenVersion` are invalidated after deployment.
- Startup migration hashes legacy order tokens and creates unique indexes.
- Duplicate normalized emails stop startup and require deliberate staging cleanup.
- Historical MongoDB, Stripe, SMTP, and JWT credentials still require external rotation.
- Production must configure HTTPS origins, trusted proxy IPs, Stripe webhook secret, and the real frontend/API URLs.

## Verification performed

- Backend solution build passed with 0 warnings and 0 errors.
- Angular production build passed; only pre-existing unused-import diagnostics remained.
- Fourteen isolated unit/security tests passed.
- npm and NuGet vulnerability audits reported no known vulnerable packages.
- A static search found no active local-storage JWT, bearer header, query capability token, or client-readable auth-cookie flow.

## Remaining risks

- Full MongoDB integration tests require an isolated local database and contract updates.
- Order creation uses compensating writes, not a MongoDB multi-document transaction.
- The production Angular API URL remains unknown and must not be guessed.
- CSP, cross-site cookie behavior, Stripe, Packeta, and startup migration require staging validation.

