# KorSoft ESHOP

KorSoft ESHOP is a standalone commercial e-commerce product being prepared for multiple customer businesses. The current implementation is the WAFFL custom-clothing store, supporting registered and guest checkout, product and design management, Stripe/IBAN/cash-on-delivery payments, Packeta delivery, order tracking, and an administrator area.

Product architecture, reusable modules, commercial configuration, and a separate `admin.<customer-domain>.sk` site are planned in [the product and architecture plan](docs/planning/README.md). These capabilities are not implemented yet. Existing source paths, branding, and deployment identifiers remain unchanged.

## Repository map

- [`api/nia_api`](api/nia_api/README.md) — ASP.NET Core 8 API.
- [`api/nia_api.Tests`](api/nia_api.Tests/README.md) — xUnit tests.
- [`web`](web/README.md) — Angular 21 frontend.
- [`docs`](docs/README.md) — security, deployment, and dated change history.
- [`docs/planning`](docs/planning/README.md) — future product architecture, audit, library assessment, and delivery roadmap.
- [`CONTEXT.md`](CONTEXT.md) — current architecture, workflows, invariants, and repository rules for developers and AI assistants.

## Prerequisites

- .NET 8 SDK
- Node.js and npm compatible with Angular 21
- MongoDB for local API use
- Stripe test credentials for payment testing
- SMTP credentials for registration and password-reset email testing

Do not commit credentials. Configure the API through .NET User Secrets or environment variables as described in [`docs/secure-setup.md`](docs/secure-setup.md).

## Quick start

Restore and start the API:

```powershell
dotnet restore api\nia_api\waffl.sln
dotnet run --project api\nia_api\nia_api.csproj
```

Start the frontend in another terminal:

```powershell
cd web
npm install
npm start
```

The development frontend expects the API at `https://localhost:7115` and runs at `http://localhost:4200`.

## Verification

```powershell
dotnet build api\nia_api\waffl.sln --no-restore --verbosity quiet
dotnet test api\nia_api\waffl.sln --no-build --filter "FullyQualifiedName!~PublicControllerIntegrationTests"
cd web
npm run build -- --progress=false
npm audit
```

MongoDB integration tests require a dedicated local test database. Never point tests at production data.

## Documentation workflow

Before making changes, read [`CONTEXT.md`](CONTEXT.md) and the nearest component README. Every material change must add or update a dated entry under [`docs/history`](docs/history/README.md). All Markdown documentation is written in English.
