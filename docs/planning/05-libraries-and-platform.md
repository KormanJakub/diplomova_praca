# Library and Platform Assessment

Reviewed: 2026-09-15. Status: candidates only; no packages installed, removed, upgraded or licensed. Official documentation was consulted for the capabilities below. Package versions, licenses and hosting compatibility must be checked again at adoption.

## Prefer the framework when sufficient

| Existing code / concern | Candidate | Recommendation and limits |
| --- | --- | --- |
| Repeated time conversion in `LocalTimeService` and static clock calls | .NET `TimeProvider`, `DateTimeOffset`, `TimeZoneInfo` | Adopt injected time for expiry/reservations and store UTC. Available in .NET 8+; no new clock library needed. [Microsoft source documentation](https://github.com/dotnet/docs/blob/main/docs/standard/datetime/timeprovider-overview.md) |
| Inconsistent anonymous error objects and controller catches | ASP.NET Core ProblemDetails and exception handling | Standardize safe HTTP errors and application error mapping. Business failures still need explicit codes; no custom error framework. [Microsoft guidance](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0) |
| Partial custom Origin middleware for cookie mutations | ASP.NET Core antiforgery plus Angular XSRF support | Adopt and test framework token exchange alongside Origin checks. Keep provider webhooks separately signed. [ASP.NET guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0), [Angular guidance](https://angular.dev/best-practices/security) |
| Static/global provider initialization and unvalidated configuration | Built-in DI, typed Options with startup validation, provider clients per installation | Use existing platform capabilities. Avoid runtime global credentials in shared processes. |
| Basic rate limiting and logging | Existing ASP.NET rate limiting and `ILogger` | Keep them. Add deployment-wide limits/telemetry only when scale requires it; a per-process limiter does not become distributed automatically. |
| Primitive hashing/random helpers | Existing `RandomNumberGenerator`, SHA-256, PasswordHasher | Keep vetted primitives. A generic cryptography library is unnecessary for these operations; token lifecycle and password policy still require domain decisions. |

## Focused library candidates

| Candidate | Replaces / improves | Adoption condition |
| --- | --- | --- |
| FluentValidation | Repeated cross-field DTO validation in controllers | Use explicit `ValidateAsync` at the application boundary. Keep domain/index constraints. Do not adopt the legacy synchronous MVC auto-validation package for new work; its documentation no longer recommends that approach. [Official integration guidance](https://docs.fluentvalidation.net/en/latest/aspnet.html) |
| MailKit / MimeKit | Direct `System.Net.Mail.SmtpClient` use in `EmailSenderService` | Use behind `INotificationSender`; preserve HTML encoding and move templates outside transport code. A library does not provide durable delivery or solve the order/email transaction gap. Review exact release license and runtime dependencies. [Official repository](https://github.com/jstedfast/MailKit), [Microsoft SmtpClient guidance](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient) |
| ASP.NET Core Identity or an OIDC identity provider | Hand-maintained staff account/session/MFA lifecycle | Perform an identity spike first. Built-in Identity is not a drop-in MongoDB replacement; choose and maintain a compatible store or separate identity persistence/provider. Compare migration, MFA, revocation, recovery, cost, customer installations and lock-in. [Identity documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0) |
| Testcontainers for .NET (MongoDB) | Shared hard-coded local database tests | Use disposable fixtures, unique database identity, realistic replica-set support for transaction cases, deterministic cleanup and CI container runtime. Test the real API host rather than an empty fixture. [MongoDB module](https://dotnet.testcontainers.org/modules/mongodb/) |
| Existing Stripe.net SDK with injected client and idempotency | Direct `new SessionService()` and single mutable session field | Retain the SDK; change integration ownership and payment-attempt model. Pass stable provider request keys and reconcile ambiguous results. [Stripe idempotency documentation](https://docs.stripe.com/api/idempotent_requests) |
| Microsoft.FeatureManagement | Scattered rollout switches | Consider when there are actual staged rollouts. It does not implement commercial authorization, tenant isolation, quotas or billing. Those need application policy. [Feature management documentation](https://learn.microsoft.com/en-us/azure/azure-app-configuration/feature-management-dotnet-reference) |
| Angular workspace libraries | Copy/pasted admin/storefront UI and request types | Start with CLI/workspace capabilities and public exports; avoid adopting another monorepo tool until task orchestration becomes a measured issue. [Angular libraries](https://angular.dev/tools/libraries/creating-libraries) |

## Evaluate later, not baseline requirements

- Durable jobs: choose a scheduler or queue only after specifying persistence, job leases, idempotency, retries and operational ownership. An outbox is still required when a database change must reliably schedule an external effect.
- OpenAPI client generation: compare generator output for the chosen API contract and Angular version. Generated code must be reproducible and never manually patched.
- Image processing: evaluate a maintained decoder/encoder with pixel/memory limits and acceptable commercial license before accepting arbitrary image formats. Do not assume an image package is free for this commercial product.
- Observability: prefer supported OpenTelemetry integration when traces/metrics across HTTP, persistence and jobs are needed; keep PII out of attributes.
- Mediation and mapping: direct use-case injection and explicit small mappings are sufficient initially. MediatR, AutoMapper, a new event bus, or a generic repository are not prerequisites for Clean Architecture. Assess the exact version/license before any later choice.

These later candidates are investigation categories, not verified package selections.

## Existing code/dependencies to simplify

- Remove obsolete `AdminMiddleware` only after confirming registration/reflection use; role policies replace its purpose.
- Remove the old frontend JWT decoder and `jwt-decode` if the full project search remains empty for active use. Do not reintroduce browser token parsing.
- `crypto-js` appears in `web/package.json` but no source reference was found in `web/src` during this audit. Check tests/build configuration before removing it.
- Retain `ngx-cookie-service` until cart handling is migrated: `CartCustomizations` still uses it. Cookie-based cart storage deserves a separate size/privacy/design review.
- Replace duplicated guest/user checkout orchestration with shared application use cases; no library can supply the merchant's pricing and order rules correctly by itself.
- Consolidate email templates and status-label mappings with explicit contracts/localization. Avoid framework additions for a few static mappings.
- Retire destructive legacy seed helpers through an explicit development-data tool; do not reactivate them as HTTP endpoints.

## Runtime and commercial dependency gate

The API currently targets .NET 8. The [official support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) lists support ending on 2026-11-10 and .NET 10 LTS support through 2028-11-14. Plan a tested .NET 10 migration before commercial support spans that deadline. Select the supported patched release at implementation time rather than freezing this document's version assumptions.

Before accepting any dependency, record exact version, official source, license text/SPDX where provided, copyright notices, transitive dependencies, security history, supported runtime, maintenance/release activity, redistribution/SaaS implications, costs, replacement strategy, and reviewer/date. Record the result in an SBOM/license inventory. No commercial license conclusion is established by this candidate list.

For a SaaS library/service, additionally evaluate data location, customer credential handling, export, availability dependency and account ownership. A dependency must remove more code/risk than it introduces and must be exercised by a representative prototype before becoming a product requirement.
