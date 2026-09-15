# KorSoft ESHOP API Tests

This xUnit project contains unit, security, and full HTTP pipeline integration tests.

## Test groups

- `ApiTestFixture.cs` — `ApiWebApplicationFactory` managing an isolated, self-contained MongoDB instance (via `Mongo2Go`), test JWT configuration, and test authentication helpers.
- `SecurityAndAccessIntegrationTests.cs` — tests for unauthorized admin and customer access (401/403), revoked/tampered sessions, and CSRF Origin protection.
- `OrderMutationIntegrationTests.cs` — tests for order status updates, capability token expiration/revocation, and rejection of unsafe state mutations.
- `UserControllerIntegrationTests.cs` — integration tests for user profiles, customer orders, and cancellation authorization.
- `PublicControllerIntegrationTests.cs` — integration tests for public endpoints (registration, login, verification, forgot-password) asserting secure anti-enumeration contracts and dynamic test data seeding.
- `OrderServiceTests.cs` — order service invariants and lifecycle validation.
- `SecurityTests.cs` — capability-token format/hashing, authentication-cookie attributes, and email-normalization invariants.
- `PasswordServiceTests.cs` — password hashing and legacy SHA-256 migration compatibility.

## Execution

Run the complete test suite:

```powershell
dotnet test
```

All integration tests run against isolated ephemeral databases; no local MongoDB service or Docker installation is required.
