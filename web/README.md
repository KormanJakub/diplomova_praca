# KorSoft ESHOP Frontend

Angular 21 standalone frontend currently implementing the WAFFL custom-clothing store in KorSoft ESHOP. It provides public catalog pages, registered and guest checkout, order tracking, account pages, and an administrator interface within one application. Independent storefront/admin applications and the admin subdomain are described in [the future separation plan](../docs/planning/04-admin-and-domains.md).

## Main technologies

- Angular standalone components and router
- Angular HttpClient with a functional interceptor
- PrimeNG / PrimeUIX themes
- Stripe Checkout redirection
- Packeta widget loaded on demand
- Firebase Hosting

## Install and run

```powershell
npm install
npm start
```

The development server runs at `http://localhost:4200`. `src/Environments/environment.ts` expects the API at `https://localhost:7115`.

Production build:

```powershell
npm run build -- --progress=false
```

Output is written to `dist/web/browser`, which matches `firebase.json`.

## Authentication behavior

The frontend never receives or reads the JWT. The API stores it in the `__Host-waffl_session` HttpOnly cookie. `auth.interceptor.ts` sets `withCredentials: true` only for API requests.

`AuthService` stores `waffl_ui_session` in local storage for display and navigation only. It is untrusted and contains no bearer token. Angular guards are not a security boundary; API authorization remains mandatory.

Do not restore old JWT decoding, bearer headers, or client-managed authentication cookies. See [`../CONTEXT.md`](../CONTEXT.md).

## Checkout behavior

- `CartCustomizations` is a seven-day client-readable cart cookie and must not contain authentication credentials or customer PII.
- The server recalculates prices and validates product variants and stock.
- Order creation returns one-time raw cancellation and follow tokens.
- Tokens are carried between frontend routes in URL fragments and sent to the API only in POST bodies.
- Stripe redirects use the server-created checkout URL.
- Packeta's external script loads only when the user opens the widget.

## Hosting and security headers

`firebase.json` configures SPA rewrites and CSP, referrer, content-type, framing, and permissions headers. Keep external origins narrowly scoped.

The production API URL is not configured yet. Add an Angular production environment/file replacement only after the real HTTPS API address is known. Do not publish a build that still targets localhost.

## Verification

```powershell
npm run build -- --progress=false
npm test
npm audit
```

The build currently reports non-failing diagnostics for unused standalone imports. Treat new warnings separately from this known baseline.
