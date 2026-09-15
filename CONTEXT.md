# KorSoft ESHOP Application Context

Last updated: 2026-09-15

This file is the current technical handoff for developers and AI assistants. Read it before changing authentication, orders, payments, database models, checkout, deployment, or security controls.

Documentation roles:

- `README.md` files explain how to understand, run, and verify each part of the repository.
- `CONTEXT.md` describes how the application works now and the invariants future changes must preserve.
- `docs/history/YYYY-MM-DD-*.md` records what changed on a given day and must not be used as the current source of truth.
- `docs/audit-2026-09-14.md` is the original security assessment.
- `docs/secure-setup.md` contains deployment and secret-management requirements.
- `docs/planning/README.md` indexes future architecture and product proposals; none of those proposals is implemented merely by being documented.

## 1. Product and architecture

KorSoft ESHOP is now the standalone commercial product identity established by the owner. The existing runtime is still the WAFFL custom-clothing implementation: one storefront/admin Angular application, one ASP.NET API, and one store database. A registered user or guest selects a product, variant, and design, creates customizations, and turns them into an order. Payment methods are Stripe, bank transfer (`IBAN`), and cash on delivery (`Dobierka`). Delivery is either home delivery or Packeta.

Multiple customer installations, optional reusable modules, commercial editions, backend layer separation, and the requested `admin.<customer-domain>.sk` deployment are planned in [docs/planning](docs/planning/README.md). Do not describe them as existing capabilities. Names such as `nia_api`, `waffl.sln`, and the current cookie remain compatibility-sensitive runtime identifiers.

Repository layout:

- `api/nia_api` — ASP.NET Core 8 Web API using MongoDB, Stripe, and SMTP.
- `api/nia_api.Tests` — xUnit unit, security, and MongoDB integration tests.
- `web` — Angular 21 standalone application using PrimeNG and Firebase Hosting.
- `docs` — security, deployment, and historical documentation.

The API disables the default JSON camel-case conversion. Public contracts therefore frequently use C# property casing such as `OrderId` and `CancellationToken`. UI-only session metadata uses lower camel case. When changing a contract, inspect both the API and Angular caller.

## 2. Important files

- `api/nia_api/Program.cs` — dependency injection, authentication, CORS, trusted proxies, rate limiting, security headers, and middleware order.
- `api/nia_api/Data/NiaDbContext.cs` — MongoDB collection mapping.
- `api/nia_api/Controllers/PublicController.cs` — registration, login, logout, session refresh, verification, and password reset.
- `api/nia_api/Controllers/UserController.cs` — profile, registered-user customizations, and orders.
- `api/nia_api/Controllers/GuestUserController.cs` — guest checkout and capability-token access.
- `api/nia_api/Controllers/AdminController.cs` — administrator operations; the whole controller requires the `admin` role.
- `api/nia_api/Controllers/PaymentController.cs` — Stripe checkout, return verification, and webhook.
- `api/nia_api/Controllers/FileController.cs` — image access and admin-only mutations.
- `api/nia_api/Services/OrderService.cs` — authoritative order creation, cancellation, and inventory handling.
- `api/nia_api/Services/SecurityDataInitializer.cs` — startup security migration and unique indexes.
- `api/nia_api/Security/*` — session cookie, capability token, and email normalization helpers.
- `web/src/app/Services/auth.service.ts` — client-side session metadata and authentication calls.
- `web/src/app/Services/auth.interceptor.ts` — credentials handling for API requests.
- `web/src/app/Components/checkouts/first-page-checkout/*` — shared registered/guest checkout flow.
- `web/firebase.json` — Firebase rewrites and browser security headers, including CSP.

## 3. Data model

`NiaDbContext` exposes these MongoDB collections:

- `Users`, `GuestUsers`
- `Products`, `Designs`, `Tags`, `PairedDesigns`
- `Customizations`, `Orders`
- `NewsReceiver`, `Files`, `Gallery`, `Questions`, `StoreSettings`

Users, guests, products, designs, and customizations use `Guid` identifiers. `Order.Id` is a sequential integer generated from the current highest ID. Duplicate-key conflicts are retried up to ten times.

Order states, in enum order, are:

1. `PRIJATA`
2. `ZAPLATENA`
3. `VO_VYROBE`
4. `PRIPRAVENA`
5. `POSLANA`
6. `ZRUSENA`
7. `REKLAMACIA`

Do not confuse `Order.StatusOrder` with `Order.PaymentStatus`. A Stripe order becomes paid only after server-side verification with Stripe. The admin `mark-paid` endpoint is only for non-Stripe manual payments.

## 4. Security invariants

### Authentication and authorization

- JWT is stored only in the `__Host-waffl_session` cookie.
- The cookie must remain `HttpOnly`, `Secure`, `SameSite=None`, `Path=/`, have no `Domain`, and expire after one hour.
- Never return JWT in JSON, store it in `localStorage`, expose it to JavaScript, or rebuild bearer-token handling in Angular.
- The Angular interceptor sends `withCredentials: true` only to URLs under `environment.apiUrl`. It must not add `Authorization: Bearer`.
- `waffl_ui_session` in `localStorage` contains only untrusted UI hints: role, first name, and email-confirmation state. Users can modify it. It must never authorize an API action.
- Angular route guards are UX controls only. Every protected API action needs `[Authorize]` or `[Authorize(Roles = "admin")]`.
- JWT validation loads the user from MongoDB and compares `TokenVersion` and the current admin flag on every authenticated request.
- Password reset and account anonymization increment `TokenVersion`, revoking old sessions.
- `POST /public/login` returns only UI metadata. `GET /public/session` refreshes that metadata from an authenticated server session.
- `api/nia_api/Middleware/AdminMiddleware.cs` is obsolete and unregistered. It checks an old claim and must not be re-enabled.
- `web/src/app/Services/decoding-token.service.ts` is obsolete and must not be used to restore client-side JWT parsing.

### Order capability tokens

- Cancellation and follow tokens are 32 cryptographically random bytes encoded as 64 hexadecimal characters.
- The raw token is returned only when an order is created. MongoDB stores only its SHA-256 hash.
- Cancellation tokens expire after 24 hours. Follow tokens expire after 180 days.
- Never place a raw token in logs, analytics, admin responses, or query parameters.
- The frontend may carry a token between its own pages in the URL fragment (`#token`), because fragments are not sent in HTTP requests.
- API calls send the raw token in a POST JSON body such as `{ "Token": "..." }`.
- Backend lookup always hashes the submitted token and checks its expiration.
- Guest tracking is capability-based access. Treat every field returned by a follow-token endpoint as visible to anyone holding that link.

### Email and passwords

- Normalize email with `Trim().ToUpperInvariant()` and store it in `User.NormalizedEmail`.
- MongoDB has the unique sparse index `ux_users_normalized_email`. Registration must still catch duplicate-key races.
- New passwords use ASP.NET `PasswordHasher<User>`. Legacy SHA-256 hashes remain readable only for compatibility with old accounts.
- Password-reset tokens are cryptographically random, stored as hashes, expire after 15 minutes, and are deleted after successful use.
- Registration verification codes are cryptographically generated six-digit numbers and expire after 10 minutes.
- Forgot-password and resend-verification responses must not reveal whether an email address exists.

### CORS, request origin, proxies, and rate limits

- CORS allows only `Hosting:Web-Url`; Development additionally allows `http://localhost:4200`.
- `AllowCredentials` is required because the Firebase frontend and API use a cross-site HttpOnly cookie.
- Authenticated POST/PUT/PATCH/DELETE requests that contain an `Origin` header must match the configured web origin.
- Never clear `KnownNetworks` or `KnownProxies`. Add reverse-proxy IP addresses explicitly under `Hosting:KnownProxies`.
- Rate limits per minute: global 120, `sensitive` 10, `public-write` 5, and `public-read` 30. The partition key is authenticated `UserId`, otherwise remote IP.
- Kestrel and multipart request bodies are limited to 6 MiB. A single image upload is limited to 5 MiB.

### Files and third-party scripts

- Upload accepts JPG/JPEG, PNG, and WEBP only, checks extension and magic bytes, and generates a random server-side filename.
- Upload and deletion must remain admin-only. Filesystem paths must be derived from trusted stored names, never raw client filenames.
- The Packeta script is not globally loaded from `index.html`. It is loaded only when the user opens the Packeta widget and uses `referrerPolicy = no-referrer`.
- Keep the CSP in `web/firebase.json` as narrow as possible. Do not introduce `script-src *` or `'unsafe-eval'`.

## 5. Authentication flow

1. `POST /public/login` looks up a normalized email and verifies the password.
2. The API creates a one-hour JWT containing `UserId`, user identity claims, `TokenVersion`, `jti`, and optional `Role=admin`.
3. The API writes the HttpOnly cookie and returns `{ role, firstName, email_confirmation }`.
4. Angular stores only that UI metadata; the browser manages the cookie.
5. Every protected request verifies signature, issuer, audience, lifetime, database user, token version, and current role.
6. `POST /public/logout` removes the cookie. Angular removes UI metadata even if the request fails.

If cookie settings or origins change, update the API cookie, CORS, frontend credentials behavior, and deployment configuration together. Production requires HTTPS.

## 6. Customization and order flow

### Registered user

1. `POST /user/make-customization` accepts 1–25 items and assigns ownership from the JWT, not from the request.
2. The server loads the real product and design, validates color/size, and calculates price from database values.
3. `POST /user/make-order` receives unique customization IDs plus payment and delivery parameters.
4. `OrderService.CreateAsync` verifies ownership, `IsOrdered == false`, stock, payment/delivery allowlists, and Packeta fields.
5. Customizations are claimed with `IsOrdered = true`, then inventory is atomically decremented per product variant.
6. After successful insertion, raw cancellation/follow tokens are returned once; only hashes remain in MongoDB.

### Guest user

1. `POST /guest/make-customization-without-register` creates a `GuestUser` and its customizations.
2. `POST /guest/make-order-without-register` validates that guest and calls the same `OrderService`.
3. `POST /guest/follow-order` receives the follow token in its body.
4. `POST /guest/order/{OrderId}` receives the same token in its body for a numbered detail request.
5. `POST /guest/cancel` and the compatibility route `cancel-order-by-token` receive the cancellation token in their body.

`OrderService` currently uses compensating writes. If stock reduction fails, it restores already-decremented items and releases claimed customizations. Cancellation restores inventory and sets `IsOrdered = false`. This is not a MongoDB multi-document transaction. A process crash between writes cannot be fully eliminated by compensating code; migrating to transactions on a replica set is future work.

## 7. Payments

- Allowed payment values are `Stripe`, `IBAN`, and `Dobierka`; delivery values are `HomeDelivery` and `Packeta`.
- A Stripe checkout session can be created only for an existing unpaid, uncancelled Stripe order with a valid cancellation token.
- The Stripe session uses `ClientReferenceId = Order.Id`, EUR, and the authoritative `Order.TotalPrice` from MongoDB.
- Verification checks Stripe payment status, currency, client reference, stored session ID, and exact amount.
- The canonical fulfillment path is signed `POST /payment/stripe-webhook` for `checkout.session.completed` and `checkout.session.async_payment_succeeded`.
- The browser return endpoint may verify a session, but must keep the same server-side checks. Never trust a frontend payment flag or query value by itself.
- Administrators must not manually mark Stripe orders as paid.

## 8. Startup migration

`SecurityDataInitializer` runs during API startup and:

- populates normalized email values;
- refuses to start when normalized emails are duplicated;
- creates the unique email index;
- hashes legacy plaintext order tokens;
- assigns expiration timestamps to legacy tokens;
- marks customizations belonging to active orders as ordered;
- creates unique sparse indexes for cancellation and follow-token hashes.

Test migration changes against a staging copy first. Do not replace duplicate-email fail-fast behavior with silent account merging.

## 9. Account anonymization

`DELETE /user/remove` preserves order references and anonymizes the account instead of deleting the document:

- changes email to `deleted-<guid>@invalid.local`;
- clears identity, address, phone, password, and reset data;
- removes admin access and increments `TokenVersion`;
- clears personal descriptions from the user's customizations;
- removes the session cookie.

For a complete retention or GDPR workflow, separately assess `GuestUsers`, order records, logs, backups, and statutory accounting retention.

## 10. Configuration and secrets

Real secrets must never be committed to `appsettings*.json`, documentation, tests, or history entries. Required settings are:

- `NiaDbSettings:ConnectionString`, `NiaDbSettings:DatabaseName`
- `JwtConfig:Key`, `JwtConfig:Issuer`, `JwtConfig:Audience`
- `Stripe:SecretKey`, `Stripe:PublishableKey`, `Stripe:WebhookSecret`
- `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`
- `Hosting:Web-Url`, `Hosting:KnownProxies`

Use .NET User Secrets locally and environment variables with `__` instead of `:` in hosting. Historical MongoDB, Stripe, SMTP, and JWT credentials must be rotated; deleting them from the current tree does not revoke them.

`web/src/Environments/environment.ts` currently points to `https://localhost:7115`. Before publishing, add or configure a production Angular environment with the real HTTPS API URL. Do not guess that URL.

## 11. Build and verification

From the repository root:

```powershell
dotnet build api\nia_api\waffl.sln --no-restore --verbosity quiet
dotnet test api\nia_api\waffl.sln --no-build --filter "FullyQualifiedName!~PublicControllerIntegrationTests" --logger "console;verbosity=minimal"
dotnet list api\nia_api\waffl.sln package --vulnerable --include-transitive
```

Frontend:

```powershell
cd web
npm run build -- --progress=false
npm audit
```

Verified on 2026-09-15:

- backend build passed with 0 warnings and 0 errors;
- Angular production build passed with existing unused-import warnings;
- isolated unit/security suite passed 14/14 tests;
- npm and NuGet audits reported no known vulnerable dependencies.

`PublicControllerIntegrationTests` require an isolated MongoDB instance at `127.0.0.1`. Some old expectations may still describe the pre-HttpOnly-cookie response contract. Update them before CI use and never run them against production data.

## 12. Working and writing rules

These rules apply to developers and AI assistants working in this repository.

### Before editing

- Read the nearest `README.md`, this file, and the latest relevant file under `docs/history`.
- Inspect the actual implementation before relying on historical documentation.
- Preserve unrelated user changes and do not overwrite a dirty working tree.
- Make the smallest coherent change that solves the requested problem.

### Code changes

- Keep authorization and business validation on the server. Frontend checks are never a security boundary.
- Use request/response DTOs instead of binding database models for mutable endpoints.
- Derive user identity from authenticated claims and re-check resource ownership in the database query.
- Calculate prices, fees, stock changes, roles, and payment state from trusted server-side data.
- Prefer atomic conditional MongoDB updates. Document any multi-document consistency window and rollback behavior.
- Preserve backward compatibility only when it does not weaken security. Clearly document breaking API or data changes.
- Never expose exception details, hashes, secrets, personal data, or raw capability tokens in logs.
- Add or update tests for security-sensitive behavior and run verification proportional to the change.

### Documentation language and style

- All Markdown files in this repository must be written in English.
- Use concise technical English, ISO dates (`YYYY-MM-DD`), repository-relative paths, and exact command examples.
- Describe the current state in present tense. Describe historical work in past tense.
- Do not copy secrets, connection strings, customer data, email addresses, raw tokens, or production log content into Markdown.
- Link to another document instead of maintaining conflicting duplicate instructions.
- Update `CONTEXT.md` only when the current architecture, workflow, invariant, configuration, or known debt changes.
- Update the nearest `README.md` when setup commands, prerequisites, directory responsibilities, or entry points change.
- Put future designs in `docs/planning` with a status, evidence, dependencies, decision owners and acceptance criteria. Only update current runtime descriptions after implementation is verified; plans do not authorize deployment or code changes by themselves.

### Historical change records

- Every material feature, security fix, migration, architecture change, or API-contract change requires a file under `docs/history`.
- Name it `YYYY-MM-DD-short-kebab-case-title.md`, using the Europe/Bratislava calendar date.
- A history file must include: summary, motivation, implementation, affected files/contracts, data or deployment impact, verification, and remaining risks.
- Prefer one focused file per workstream. Multiple related changes made on the same day may share one file.
- History is append-only. Do not rewrite an old entry to make it match the current application. For a factual correction, append a dated correction note and link the newer entry.
- Never invent commit hashes, deployment results, test results, incidents, or dates. Include a commit/PR link only when verified.
- A historical entry records what was known at that time; `CONTEXT.md` remains the source of truth for current behavior.

### Completion checklist

- Is authorization enforced by the API, not only an Angular guard?
- Is the user ID taken from verified JWT claims instead of request input?
- Does the response avoid password hashes, reset tokens, JWTs, and stored capability hashes?
- Did any token or PII enter a query string, URL log, client-readable auth storage, or documentation?
- Are price and inventory derived and validated from database data?
- Is the mutation safe against retries and concurrent requests?
- Does the endpoint have an appropriate rate-limit policy?
- Do input DTOs have required, length, and allowlist validation?
- Are startup migrations and unique indexes still valid?
- Did backend build, relevant tests, frontend production build, and dependency audits pass?
- If an external origin changed, was CSP updated narrowly?
- Was the appropriate README, current context, and dated history entry updated?

## 13. Known technical debt

The [2026-09-15 architecture audit](docs/planning/01-architecture-audit.md) adds evidence beyond the earlier security hardening. In particular, admin order updates still directly assign lifecycle state, several responses still serialize order documents containing token hashes, and payment attempts are represented by one mutable session ID. The security rules above are preservation targets, not proof that every code path already satisfies every rule.

- `UserControllerIntegrationTests.Test1` is empty and imports a test-platform `Program`; the previous passing test count does not establish HTTP integration coverage. Public-controller expectations also need updating.
- Startup migration reads entire collections and uses token shape as a hash marker; explicit schema versions, bounded migration steps and coordination are future work.

- The production Angular API URL is not configured yet.
- MongoDB integration tests need an isolated test environment and updated legacy response expectations.
- Order creation uses compensating writes rather than a MongoDB multi-document transaction.
- Angular still contains unused imports, the obsolete `decoding-token.service.ts`, and the currently unused `jwt-decode` dependency.
- `AdminMiddleware` is obsolete; admin authority is enforced by role authorization plus database-backed JWT validation.
- Firebase CSP allows the Packeta widget. Validate CSP, cookie behavior, Stripe, and Packeta in a real staging browser before release.

Security is not a permanent state. Keep this file synchronized whenever authentication, data migration, capability-token handling, payment flow, or deployment topology changes.
