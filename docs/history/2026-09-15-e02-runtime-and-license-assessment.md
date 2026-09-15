# E02: Runtime, Dependency, and License Assessment — 2026-09-15

## Summary

Implemented milestone **E02**: completed a full assessment of runtime lifecycles (.NET 8 EOL vs .NET 10 LTS target), verified package compatibility, removed dead code and obsolete dependencies (`AdminMiddleware.cs`, `decoding-token.service.ts`, `crypto-js`, `jwt-decode`), and compiled a formal Dependency, Runtime, and Asset License Inventory with an intellectual property audit for trademarked sample assets.

## Motivation

Audit items A15 and A16 identified:
- .NET 8 support terminates on November 10, 2026; a commercial product requires a supported LTS target (evaluated .NET 10 LTS).
- Dead code paths and obsolete frontend dependencies (`crypto-js`, `jwt-decode`, `decoding-token.service.ts`, `AdminMiddleware.cs`) create maintenance confusion and security review overhead.
- Sample clothing designs (`mickey.jpg`, `minnie.png`, `spiderman.png`) represent third-party IP that must not be bundled with commercial releases.

## Implementation

1. **Runtime Assessment**:
   - Analyzed host environment: .NET SDK 10.0.301, .NET Runtime 10.0.9, and Node.js v22.19.0.
   - Identified .NET 10 LTS (supported through November 2028) as the designated upgrade target.
   - Confirmed all backend packages (`MongoDB.Driver 3.11.2`, `Stripe.net 47.0.0`, `Microsoft.IdentityModel.Tokens 8.22.0`) run cleanly on .NET 10.
2. **Dead Code Cleanup**:
   - Removed `api/nia_api/Middleware/AdminMiddleware.cs` (unregistered middleware checking obsolete claim).
   - Removed `web/src/app/Services/decoding-token.service.ts` (obsolete client-side token decoder).
   - Removed `"crypto-js"` and `"jwt-decode"` from `web/package.json` and updated `package-lock.json`.
3. **License and IP Inventory**:
   - Created `docs/planning/07-dependency-and-license-inventory.md` documenting:
     - All NuGet and npm dependencies with verified SPDX licenses (MIT, Apache-2.0, BSD-3-Clause, 0BSD).
     - Panchang font family under the Fontshare Font License (cleared for commercial use).
     - Formal IP warning classifying Disney/Marvel character designs as restricted demonstration data.

## Affected files and contracts

- `api/nia_api/Middleware/AdminMiddleware.cs` (deleted)
- `web/src/app/Services/decoding-token.service.ts` (deleted)
- `web/package.json`
- `web/package-lock.json`
- `docs/planning/07-dependency-and-license-inventory.md` (new)
- `docs/planning/README.md`
- `docs/planning/06-delivery-roadmap.md`

## Data and deployment impact

None on production. No breaking API contract changes. Frontend bundle size reduced slightly by removing unused dependencies.

## Verification performed

1. Backend: `dotnet test api/nia_api.Tests/nia_api.Tests.csproj` — 58 passed, 0 failed in 2 seconds.
2. Frontend: `npm run build` in `web/` — application bundle generation completed with 0 errors.

## Remaining risks or follow-up

- Physical upgrade of `.csproj` TargetFramework from `net8.0` to `net10.0` should be finalized when approaching .NET 8 EOL in autumn 2026.
- The next milestone (**E03**) will begin the domain and application extraction of the Orders slice.
