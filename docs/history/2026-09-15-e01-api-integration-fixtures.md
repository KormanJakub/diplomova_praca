# E01: Repair API Integration Fixtures and Capture Safe Contracts — 2026-09-15

## Summary

Implemented roadmap deliverable **E01**: established self-contained, automated API integration testing fixtures using `WebApplicationFactory<Program>` and `Mongo2Go`, eliminated placeholder/dummy tests, captured legacy security contracts, and added test coverage for unauthorized admin/customer access, CSRF origin enforcement, session revocation, and unsafe order state mutations.

## Motivation

The architecture audit (`docs/planning/01-architecture-audit.md`, item A14) identified that existing test coverage was limited to unit tests with trivial exits, an empty test importing the test-runner `Program` (`UserControllerIntegrationTests.Test1`), and outdated controller tests that expected account enumeration and unhashed tokens while hanging when MongoDB was unavailable.

## Implementation

1. **Host Visibility**: Added `public partial class Program { }` to `api/nia_api/Program.cs` so test fixtures bind directly to the actual API entry point.
2. **Integration Fixture**: Created `ApiWebApplicationFactory` managing an isolated, in-process ephemeral MongoDB instance via `Mongo2Go` (5.0.0), test JWT credentials, and authenticated HTTP client helpers.
3. **Security & Access Tests**: Added `SecurityAndAccessIntegrationTests` validating 401 Unauthorized for anonymous requests, 403 Forbidden for customer roles accessing `/admin/*`, 401 Unauthorized for revoked/tampered sessions, and 403 Forbidden for untrusted `Origin` headers on authenticated mutations.
4. **Order State Mutation Tests**: Added `OrderMutationIntegrationTests` verifying that admin order updates/cancellations require admin roles, non-existent orders return 404, and guest cancellation/tracking by capability token strictly enforces token expiration and one-time cancellation invariants.
5. **Classify & Repair Legacy Tests**:
   - Replaced empty `UserControllerIntegrationTests.Test1` with tests for user profile, orders, and user-scoped cancellation.
   - Updated `PublicControllerIntegrationTests` to assert current anti-enumeration behavior, cookie-based sessions, and dynamic test data seeding.
   - Updated `OrderServiceTests` to use the fixture and test item validation.

## Affected files and contracts

- `api/nia_api/Program.cs`
- `api/nia_api.Tests/nia_api.Tests.csproj`
- `api/nia_api.Tests/ApiTestFixture.cs` (new)
- `api/nia_api.Tests/SecurityAndAccessIntegrationTests.cs` (new)
- `api/nia_api.Tests/OrderMutationIntegrationTests.cs` (new)
- `api/nia_api.Tests/UserControllerIntegrationTests.cs`
- `api/nia_api.Tests/PublicControllerIntegrationTests.cs`
- `api/nia_api.Tests/OrderServiceTests.cs`
- `api/nia_api.Tests/README.md`

## Data and deployment impact

None on production. All integration test data is created in ephemeral test databases and disposed of after execution.

## Verification performed

Ran `dotnet test api/nia_api.Tests/nia_api.Tests.csproj`.
Result: 58 tests passed, 0 failed, 0 skipped in 2 seconds across all test groups.

## Remaining risks or follow-up

- Order consistency and transactional acceptance remain to be extracted into domain/application services as planned in **E03** and **E04**.
- .NET 8 to .NET 10 upgrade planning is scheduled under **E02**.
