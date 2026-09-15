# KorSoft ESHOP API

ASP.NET Core 8 Web API currently serving the WAFFL store implementation of KorSoft ESHOP. It owns authentication, authorization, catalog data, customization pricing, order integrity, inventory changes, payments, email delivery, and media metadata. The planned module/layer extraction is described in [the backend architecture proposal](../../docs/planning/03-backend-architecture.md); current controllers still contain business and persistence logic.

## Main dependencies

- MongoDB.Driver
- ASP.NET Core JWT Bearer authentication
- ASP.NET Core Identity password hashing
- Stripe.net
- Swashbuckle in Development

## Run locally

Configure secrets first; see [`../../docs/secure-setup.md`](../../docs/secure-setup.md).

```powershell
dotnet restore waffl.sln
dotnet run --project nia_api.csproj
```

Development launch profiles expose `https://localhost:7115` and `http://localhost:5013`. Swagger is available only in Development.

## API areas

- `/public` — catalog reads, registration, login/logout, session refresh, verification, password reset, and public forms.
- `/user` — authenticated profile, customizations, and owned orders.
- `/guest` — guest customization/order creation and capability-token tracking/cancellation.
- `/admin` — administrator-only catalog, order, KPI, user, and store-setting operations.
- `/payment` — Stripe checkout creation, return verification, and signed webhook.
- `/file` — public file reads and administrator-only upload/delete operations.

`/seed` has no routable seed action. The destructive seed method is marked `[NonAction]` and must remain unavailable over HTTP.

## Security model

Authentication uses a one-hour JWT stored in the `__Host-waffl_session` HttpOnly cookie. The API validates token signature, issuer, audience, lifetime, database user, token version, and current admin role. Guest order links use random capability tokens whose hashes and expiration times are stored in MongoDB.

See [`../../CONTEXT.md`](../../CONTEXT.md) before changing authentication, order, payment, or migration code.

## Build and tests

```powershell
dotnet build waffl.sln --no-restore --verbosity quiet
dotnet test waffl.sln --no-build --filter "FullyQualifiedName!~PublicControllerIntegrationTests"
dotnet list waffl.sln package --vulnerable --include-transitive
```

The startup `SecurityDataInitializer` updates legacy records and creates unique indexes. Test migration changes against a staging copy of MongoDB before deployment.
