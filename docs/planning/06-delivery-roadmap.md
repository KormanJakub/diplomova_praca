# Delivery Roadmap and Decision Register

Status: all work below is **Planned**. No runtime implementation is authorized by this planning document itself. The owner requested documentation only on 2026-09-15.

## Delivery sequence

| ID | Status | Priority / depends on | Deliverable | Acceptance evidence |
| --- | --- | --- | --- | --- |
| E01 | **Verified** | First | Repair real API integration fixtures; capture safe legacy contracts; classify empty/legacy tests | Tests fail for unauthorized admin/customer access and unsafe state mutation; no placeholder passes counted as coverage; isolated test data (58 tests passing) |
| E02 | **Verified** | E01 | Runtime/dependency/license assessment and migration prototype | Supported runtime chosen (.NET 10 LTS target), representative API/Stripe/Mongo tests pass, dependency/assets license inventory created, dead code cleaned |
| E03 | **Verified** | E01 | Extract Orders application/domain slice and thin endpoints | Every admin/customer/guest order mutation uses named lifecycle policy; no controller Mongo access for migrated slice; architectural dependency tests (94 tests passing, 0 direct Mongo order leaks in controllers) |
| E04 | **Verified** | E03 | Inventory reservations, atomic order acceptance, numbering and snapshots | Parallel duplicate checkout, out-of-stock, compensation on partial failure, atomic numbering, idempotency key, and immutable catalog snapshots verified; legacy fallback tested (100 tests passing) |
| E05 | **Verified** | E04 | Payment attempts, provider adapters, inbox/outbox and reconciliation | Decoupled IPaymentGateway, multi-attempt ledger, durable webhook inbox, late payment on cancelled order reconciliation, duplicate attempt refund, and staff refund path verified (107 tests passing) |
| E06 | **Verified** | E03; complete after E04–E05 | Merchant configuration, optional personalization and entitlement policy | Two merchant configurations (clothing vs plain hardware catalog) use same artifacts without source forks; unconfigured payment/delivery methods fail closed; disabled modules return 403 Forbidden; public profile separated from secrets (115 tests passing) |
| E07 | Planned | E01 plus staff identity decision | Separate frontend targets and staff authentication boundary | Storefront artifact excludes admin modules; customer session rejected by staff endpoints; MFA/antiforgery/session tests pass on staging hosts |
| E08 | Planned | E07 plus hosting/domain decision | `admin.<customer-domain>.sk` DNS/TLS/ingress and route cutover | Deep links, certificates, redirects, CSP, host restrictions, old-route retirement and rollback tested |
| E09 | Planned | E03; proceed feature by feature | Extract Catalog, Identity, Media, Notifications, Fulfillment, Reporting; remove proven dead code | Each slice has contracts, tests, pagination/concurrency policy and no cross-module collection access; dependency removals build cleanly |
| E10 | Planned | E02, E05, E06, E08 | Automated customer provisioning, migrations, release/config inventory and monitoring | Clean installation from release, independent secrets/storage/DB, migration locking, backup restore and offboarding export demonstrated |
| E11 | Planned | E09–E10 | Second-customer pilot and package/reuse experiment | Clothing and ordinary catalog configurations both work; no source fork; pricing/support scope and asset rights reviewed |

E07 can proceed alongside order work after its prerequisites. Do not compress the plan into a single rewrite. Deliver reviewable slices with explicit contract changes and compatibility windows. Durations and prices are intentionally unestimated until the experiments below establish effort and operating cost.

## Required design experiments

1. **Order consistency:** reproduce lifecycle bypass/concurrent updates in an isolated fixture, prototype transactional acceptance and durable recovery, document reservation expiry and refunds.
2. **Admin session:** run two local HTTPS hostnames with the intended ingress, test distinct customer/staff schemes, antiforgery, login redirects and direct-origin access.
3. **Merchant reuse:** compose plain catalog checkout without personalization and with a different theme/provider configuration.
4. **Identity:** compare supported Identity persistence with OIDC provider integration; test existing password migration, staff MFA, account recovery and session revocation.
5. **Database:** measure representative queries and transaction/reporting needs; retaining MongoDB is the initial recommendation, not an unchangeable requirement.

## Decision register

| ID | Decision / current recommendation | Owner and evidence required |
| --- | --- | --- |
| D01 | Product name KorSoft ESHOP; standalone commercial product | Confirmed by product owner on 2026-09-15; namespace/asset renaming remains planned |
| D02 | Separate admin at `admin.<customer-domain>.sk` | Requested; owner supplies actual domain(s); engineering validates DNS/hosting |
| D03 | Dedicated customer installations first; shared SaaS deferred | Product/operations approve infrastructure budget and customer count assumptions |
| D04 | Modular monolith and thin controllers | Engineering records accepted dependency/module boundaries after first slice |
| D05 | MongoDB initially; SQL alternative evaluated only by evidence | Engineering demonstrates transactions, reporting and migration cost |
| D06 | Staff identity/MFA provider and customer identity relationship | Product/security review prototype, integration costs and support ownership |
| D07 | Same-origin API ingress per frontend; optional BFF if justified | Engineering/operations validate hosting capabilities and session design |
| D08 | Editions, quotas, setup/recurring prices and customer-managed option | Product owner approves costs, scope, licenses, support and upgrade commitments |
| D09 | Supported currencies/countries, tax, invoices, refunds and returns | Product/domain specialists define exact first-release behavior |
| D10 | Backup/restore objectives, availability and support hours | Operations/product set measurable targets and demonstrate drills before promising them |

Accept decisions through a dated decision record linked here; record rejected alternatives and consequences. User-confirmed goals do not imply approval of an unchosen vendor, price, domain or infrastructure purchase.

## Migration and rollback policy

- Inventory current schema, customer records, assets, identifiers and contracts before renaming.
- Use expand/migrate/contract changes with schema version and checkpoints; support old/new readers only for a bounded interval.
- Take a tested backup and use representative staging data with sensitive values protected.
- Preserve original IDs and accepted monetary facts. Do not fabricate missing historical snapshots or timezone information; mark provenance/unknowns explicitly.
- Invalidate old staff sessions during security-boundary migration rather than broadening cookies.
- Reconcile external payment effects independently of database rollback. Never restore a DB and assume provider transactions also rolled back.
- Roll back code only to a schema-compatible build; otherwise forward-fix or follow the rehearsed restore/reconciliation procedure.

## Commercial release gates

- Orders: all transitions, refunds and inventory effects use one tested policy; immutable snapshots and concurrency controls exist.
- Payments: signed callbacks, durable attempts/events, idempotency and reconciliation work with provider failures.
- Access: staff MFA/permissions, customer ownership and installation isolation are tested at HTTP and storage boundaries.
- Operations: customer provisioning, configuration validation, managed secrets, supported runtime, backup/restore, migration recovery, logs and alert ownership exist.
- Product: merchant-specific branding and settlement details, module combinations, accessibility, checkout UX and contractual scope are verified.
- Documentation: README/setup/context reflect actual implementation; history records results and caveats; no planning item is presented as a shipped capability.

## Test matrix for future implementation

Domain tests cover lifecycle and pricing. Application tests cover actors and orchestration. Mongo integration tests cover index/transaction/concurrency behavior. HTTP tests cover authorization, antiforgery, responses and body limits. Browser tests cover separate origins and real cookie behavior. Provider sandbox tests cover payment and delivery integration. Operations tests cover migrations, backups and independent customer configuration.

A successful build or dependency audit does not substitute for these scenarios. The historical 14-test result remains a limited baseline; do not describe it as proof that these future requirements are satisfied.
