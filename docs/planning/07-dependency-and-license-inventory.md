# Dependency, Runtime, and Asset License Inventory

Prepared: 2026-09-15. Status: baseline audit for milestone **E02**; verified against repository working tree.

This inventory fulfills the commercial compliance gate established in [docs/planning/05-libraries-and-platform.md](05-libraries-and-platform.md) and [docs/planning/02-product-and-reuse.md](02-product-and-reuse.md). It documents runtime support lifecycles, backend NuGet packages, frontend npm dependencies, and assets/intellectual property.

---

## 1. Runtime Lifecycle and Support Assessment

| Runtime / SDK | Support Status | End of Support | Assessment for KorSoft ESHOP |
| :--- | :--- | :--- | :--- |
| **.NET 8 (LTS)** | Current production target (`net8.0`) | **2026-11-10** | Supported currently, but reaches end of life within ~2 months. Retained for initial baseline stability. |
| **.NET 9 (STS)** | Standard Term Support | 2026-05-12 | Reached end of support earlier than .NET 8; skip as a production target. |
| **.NET 10 (LTS)** | Long Term Support target | **2028-11-14** | **Recommended production upgrade target**. .NET SDK 10.0.301 and Runtime 10.0.9 are already installed and verified on the development host. |
| **Node.js** | Active LTS (`v22.19.0`) | April 2027 | Fully supported; modern ESM, native fetch, and Angular CLI 21 compatibility verified. |

### Migration Prototype Findings
- Target framework upgrade from `net8.0` to `net10.0` was evaluated.
- All core backend dependencies (`MongoDB.Driver 3.11.2`, `Stripe.net 47.0.0`, `Microsoft.IdentityModel.Tokens 8.22.0`, `System.IdentityModel.Tokens.Jwt 8.22.0`) target .NET Standard 2.0 / 2.1 or .NET 8/9/10 and execute cleanly on .NET 10.
- `Microsoft.AspNetCore.Authentication.JwtBearer` has official 10.0.x builds matching the installed .NET 10 SDK.
- Recommendation: Maintain `net8.0` build compatibility while ensuring source code has no deprecations blocking the `net10.0` cutover prior to November 2026.

---

## 2. Backend Dependencies (NuGet)

All direct dependencies in `api/nia_api/nia_api.csproj` and test dependencies in `api/nia_api.Tests/nia_api.Tests.csproj`:

| Package ID | Resolved Version | License (SPDX) | Commercial Redistribution | Purpose & Governance |
| :--- | :--- | :--- | :--- | :--- |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.31 | MIT | Permitted | Server-side JWT validation, cookie event hooks. |
| `Microsoft.IdentityModel.Tokens` | 8.22.0 | MIT | Permitted | Cryptographic keys and token validation parameters. |
| `System.IdentityModel.Tokens.Jwt` | 8.22.0 | MIT | Permitted | JWT creation and signing. |
| `MongoDB.Driver` | 3.11.2 | Apache-2.0 | Permitted | MongoDB client and BSON serialization. |
| `Stripe.net` | 47.0.0 | MIT | Permitted | Official Stripe payment gateway SDK. |
| `Swashbuckle.AspNetCore` | 6.6.2 | MIT | Permitted | OpenAPI / Swagger specification generation. |
| `Mongo2Go` *(test only)* | 5.0.0 | MIT | Permitted | Ephemeral MongoDB runner for integration test isolation. |
| `Microsoft.AspNetCore.Mvc.Testing` *(test only)* | 8.0.31 | MIT | Permitted | In-memory API hosting via `WebApplicationFactory`. |
| `Microsoft.NET.Test.Sdk` *(test only)* | 17.11.1 | MS-EULA / MIT | Permitted | Test discovery and execution engine. |
| `xunit` / `xunit.runner.visualstudio` *(test only)* | 2.9.3 / 2.5.3 | Apache-2.0 / MIT | Permitted | Test framework runner. |
| `Moq` *(test only)* | 4.20.72 | BSD-3-Clause | Permitted | Test doubles and email interface mocking. |
| `coverlet.collector` *(test only)* | 6.0.0 | MIT | Permitted | Code coverage collection. |

**Backend License Conclusion**: All dependencies use permissive open-source licenses (MIT, Apache 2.0, BSD-3-Clause). No copyleft (GPL/AGPL) dependencies exist in the backend solution.

---

## 3. Frontend Dependencies (npm)

Direct production and development dependencies in `web/package.json`:

| Package Name | Resolved Version | License | Commercial Redistribution | Notes / Status |
| :--- | :--- | :--- | :--- | :--- |
| `@angular/*` (core, router, forms, etc.) | 21.2.23 | MIT | Permitted | Core frontend framework. |
| `@angular/build` / `@angular/cli` | 21.2.24 | MIT | Permitted | Build tooling using Vite / esbuild under the hood. |
| `primeng` | 21.1.10 | MIT | Permitted | UI component library. |
| `@primeuix/themes` | 3.0.0 | MIT | Permitted | Styling design tokens for PrimeNG. |
| `primeicons` | 7.0.0 | MIT | Permitted | Icon set. |
| `primeflex` | 3.3.1 | MIT | Permitted | CSS utility classes. |
| `chart.js` | 4.4.8 | MIT | Permitted | Admin dashboard KPI charts. |
| `ngx-cookie-service` | 21.0.0 | MIT | Permitted | Cart/cookie manipulation in frontend. |
| `tailwindcss` / `postcss` / `autoprefixer` | 3.4.17 / 8.5.3 / 10.4.20 | MIT | Permitted | CSS compilation. |
| `rxjs` | 7.8.0 | Apache-2.0 | Permitted | Reactive programming extensions. |
| `zone.js` | 0.16.3 | MIT | Permitted | Angular change detection polyfill. |
| `tslib` | 2.3.0 | 0BSD | Permitted | TypeScript runtime helpers. |
| `vitest` / `jsdom` *(dev only)* | 4.0.8 / 27.0.0 | MIT | Permitted | Frontend test runner. |
| *`crypto-js`* | *Removed in E02* | *MIT* | *N/A* | *Confirmed unreferenced; removed from package.json.* |
| *`jwt-decode`* | *Removed in E02* | *MIT* | *N/A* | *Confirmed unreferenced; removed from package.json.* |

**Frontend License Conclusion**: All active frontend dependencies use permissive licenses (MIT, Apache-2.0, 0BSD). No restrictive copyleft licenses are present.

---

## 4. Asset and Intellectual Property Audit

### Fonts
- **Panchang Family** (`web/src/assets/fonts/`):
  - Distributed by Indian Type Foundry via Fontshare.
  - License: **Fontshare Font License** (free for personal and commercial use in software, web, and desktop).
  - Status: **Cleared for commercial use**.

### Sample Graphics and Imagery
- **Product Mockups**: Generic hoodie blanks (`G18500K_*.jpg`) and sample gallery photos (`Photo_Gallery_*.JPG`) depict physical merchandise blanks.
- **Customized/Brand Imagery**:
  > [!WARNING]
  > **Trademarked Character Designs**: `api/nia_api/wwwroot/Files/` contains design images `mickey.jpg`, `minnie.png`, and `spiderman.png`.
  > - **Ownership**: The Walt Disney Company / Marvel Characters, Inc.
  > - **Commercial Clearance**: **NOT CLEARED** for redistribution.
  > - **Action & Policy**: These files represent legacy student demonstration data from the original WAFFL deployment. They **must never be bundled** into generic merchant installation packages, distributed in default demo seeds, or used in public promotional materials for KorSoft ESHOP. In the multi-tenant product architecture, each customer merchant must supply their own licensed artwork.

---

## 5. Dependency Governance Policy for Future Work

1. **New Package Additions**:
   - Must be verified for license compatibility (prefer MIT, Apache-2.0, BSD).
   - Any package under AGPL, GPL, SSPL, or commercial dual-license requires explicit approval.
2. **Vulnerability Audits**:
   - Run `npm audit` on frontend and `dotnet list package --vulnerable` on backend during CI release builds.
3. **Asset Governance**:
   - All default product media, icons, and theme illustrations in release templates must be original or licensed under CC0/Unsplash/Commercial stock agreements.
