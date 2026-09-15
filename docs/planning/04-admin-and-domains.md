# Separate Administration Site and Domain Plan

Status: proposed. `admin.<customer-domain>.sk` represents the requested pattern; no real domain is selected or registered here.

## Target applications and routes

| Public host / surface | Purpose | Proposed exposure |
| --- | --- | --- |
| `<customer-domain>.sk` or its configured `www` host | Storefront Angular application | Catalog/customer/checkout endpoints through same-origin `/api/storefront` |
| `admin.<customer-domain>.sk` | Separate staff Angular application | Staff endpoints through same-origin `/api/admin` |
| Verified webhook endpoint | Stripe provider callbacks | Signed callback route, no browser-cookie authentication |
| Internal application origin | Modular backend | Reachable only through approved ingress or authenticated internal routes |

Recommended first design: two Angular build targets plus ingress reverse proxying each site's relative API path to the backend. A BFF (server dedicated to browser sessions) can be added when using an external identity provider; a reverse proxy alone is not a BFF or an authorization mechanism.

Current Firebase hosting rewrites all routes to one `index.html`. Validate whether the selected hosting topology can provide the required API rewrites, header controls, internal-origin restrictions, and custom-domain certificates. If not, choose a compatible hosting/ingress arrangement before implementation. Do not assume a Firebase rewrite can proxy any external API configuration.

Alternative: both applications call `api.<customer-domain>.sk` directly. This requires exact CORS origin rules, explicit credentials/antiforgery design, separate authentication schemes and audience validation, and careful cookie routing. It has more browser integration work and is not the preferred first option.

## Frontend workspace

Proposed layout:

```text
frontend/
  projects/storefront/
  projects/admin/
  projects/ui/
  projects/api-client-storefront/
  projects/api-client-admin/
  projects/configuration/
```

- Storefront and admin have independent bootstrap, router, build output, environment configuration, test suites, and deployment.
- Share visual primitives, typed API contracts and configuration helpers through libraries with public entry points. Avoid a shared library that imports staff features into the storefront.
- Storefront bundles must not import admin screens or admin API clients. The admin base route becomes `/` on its own host.
- Generate clients from explicit OpenAPI contracts after naming/versioning conventions settle. Do not expose database models as shared frontend contracts.
- Add route-level code splitting inside each app. Evaluate storefront SSR/prerendering separately for SEO; administration does not need public indexing.

Angular supports workspace libraries and public library APIs; see [Creating libraries](https://angular.dev/tools/libraries/creating-libraries). The particular split above is a project recommendation.

## Staff session boundary

An admin subdomain is a different origin but can be the same browser site as the storefront. DNS and SameSite settings do not replace CSRF protection. Microsoft documents the risk of sibling subdomains in its [antiforgery guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).

- Define separate customer and staff authentication schemes/cookies, audience or session purpose, permissions, and revocation policy.
- Issue host-only `__Host-` cookies at each browser-facing host over HTTPS, without a shared parent `Domain` attribute. Never broaden the current cookie to `.customer-domain.sk` to make login work.
- The ingress must expose only the intended API surface on each host; the API also enforces staff scheme, merchant membership and permission for every admin endpoint.
- Storefront authentication must never establish a staff session automatically. Staff login requires MFA, appropriate expiry, explicit logout/revocation, and step-up verification for sensitive operations.
- If JWTs remain, scheme/audience validation must reject customer tokens on staff endpoints. If BFF sessions replace JWT cookies, use distinct protected session purpose and server-side token storage.
- Use framework antiforgery and Origin validation on browser mutations, including login/logout. Keep the auth cookie HttpOnly; only an antiforgery token intended for request headers may be browser-readable.
- Angular's XSRF support and backend token/header conventions must be integrated and browser-tested, particularly for absolute cross-origin URLs. See [Angular security](https://angular.dev/best-practices/security).
- Webhooks use provider signature verification, inbox deduplication and endpoint-specific policy; browser antiforgery does not apply to provider callbacks.
- Choose SameSite policy after testing sign-in and payment redirects; document any exception rather than disabling controls globally.

## DNS, TLS, headers and redirects

- Verify ownership of each customer domain; provision DNS and certificate renewal for storefront/admin and any callback host.
- Reject unregistered hostnames at ingress and application configuration. Trust forwarded headers only from configured proxies.
- Derive redirects and Stripe callback URLs from registered configuration, not an arbitrary request Host or return URL. Allow only validated local paths for post-login return navigation.
- Apply stricter admin CSP with no Packeta/storefront marketing scripts. Use `no-store` on sensitive authenticated responses and framing/referrer protections.
- Review HSTS after forwarded-protocol handling; current `Program.cs` invokes HSTS before forwarded headers. Do not enable `includeSubDomains` until all covered hosts support HTTPS.
- Do not index admin content; robots directives are supplemental and do not provide access control.
- Configure exact CORS origins if any direct cross-origin browser API calls remain. Never use a broad customer-domain wildcard with credentials.

## Cutover sequence

1. Establish separate build and API contracts in staging using verified test hosts.
2. Add staff authentication boundary and browser/HTTP tests before exposing the new host.
3. Deploy admin without switching traffic; verify login/MFA, permissions, files, order transitions, CSP and logging.
4. Provision DNS/TLS and update merchant configuration, staff links and allowlisted redirects.
5. Retire storefront `/admin` routes or redirect only GET navigation to validated paths on the configured admin host. Do not redirect credentials, POST requests or arbitrary query strings.
6. Invalidate legacy staff sessions and verify old storefront-origin admin calls are rejected.
7. Retain rollback-compatible builds and a tested route/config rollback; rolling back UI must not undo security fixes or downgrade the database schema blindly.

## Acceptance scenarios

- Admin opens only its own artifact and deep links survive refresh.
- A customer session and forged UI role cannot access admin endpoints on any host, including a backend origin.
- A sibling-origin CSRF attempt, missing antiforgery header and invalid return URL fail as designed.
- Staff MFA, expiry, revocation and logout work; simultaneous customer/staff sessions remain independent.
- Tenant A staff cannot access tenant B resources, uploads, exports or configuration.
- Stripe callbacks and customer checkout continue working after cutover; no raw token is sent through redirects or logs.
