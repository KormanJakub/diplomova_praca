# Secure Local Setup and Deployment

Sensitive values have been removed from current configuration files. Older commits may still contain them, so rotate the MongoDB credential, Stripe secret key, SMTP password, and JWT signing key. Removing a value from the current tree does not revoke it. Review access and service logs after rotation without copying sensitive payloads into this repository.

## Local secrets

From `api/nia_api`, configure .NET User Secrets:

```powershell
dotnet user-secrets set "NiaDbSettings:ConnectionString" "<new-local-test-database>"
dotnet user-secrets set "JwtConfig:Key" "<new-random-signing-key>"
dotnet user-secrets set "Stripe:SecretKey" "<stripe-test-secret>"
dotnet user-secrets set "Stripe:WebhookSecret" "<stripe-test-webhook-secret>"
dotnet user-secrets set "Smtp:Username" "<smtp-account>"
dotnet user-secrets set "Smtp:Password" "<smtp-password>"
```

Also set the correct JWT issuer/audience and use an isolated local MongoDB database.

## Hosted configuration

Provide values through the hosting secret store or environment variables, replacing `:` with `__`, for example `JwtConfig__Key`.

- `Hosting:Web-Url` must be the exact HTTPS frontend origin without an unrelated wildcard.
- `Hosting:KnownProxies` must contain only explicit IP addresses of trusted reverse proxies or load balancers.
- JWT issuer and audience must match the deployed API configuration.
- The Angular production environment must contain the real HTTPS API URL. It currently has no verified production value.

The API uses an `HttpOnly; Secure; SameSite=None` session cookie. The frontend and API must both use HTTPS, and CORS must remain limited to `Hosting:Web-Url`.

## Startup data migration

On startup, `SecurityDataInitializer`:

- normalizes existing email addresses;
- hashes legacy order capability tokens;
- assigns token expiration timestamps;
- marks customizations belonging to active orders as ordered;
- creates unique indexes.

If duplicate normalized emails exist, the API intentionally refuses to start. Run the migration against a staging copy first, inspect the result, and resolve duplicates deliberately. Do not silently merge user records.

## Stripe

Configure Stripe to send these events to `POST /payment/stripe-webhook`:

- `checkout.session.completed`
- `checkout.session.async_payment_succeeded`

Set `Stripe:WebhookSecret` to the endpoint signing secret. Test order amount, cancellation, successful payment, replayed webhook, and failed payment in an isolated Stripe test environment before release.

## Release checklist

- Rotate all historically exposed credentials.
- Confirm no secret exists in tracked files, documentation, or tests.
- Configure the real frontend/API origins and trusted proxy IPs.
- Run the startup migration against staging.
- Run backend tests and frontend production build.
- Verify cross-site login/logout and session expiry in a real browser.
- Verify CSP with Packeta and the complete Stripe webhook flow.
- Never run MongoDB integration tests against production data.
