# Change History

This directory provides a human-readable, append-only history of material repository changes. Git remains the authoritative line-level record; these files explain intent, contracts, migration effects, verification, and remaining risks.

## File naming

Use:

```text
YYYY-MM-DD-short-kebab-case-title.md
```

Use the Europe/Bratislava date. Prefer one focused workstream per file.

## Required sections

Each entry must include:

1. Summary
2. Motivation
3. Implementation
4. Affected files and contracts
5. Data and deployment impact
6. Verification performed
7. Remaining risks or follow-up

Do not invent commit hashes, deployment outcomes, test results, or incident details. Never record secrets, personal data, raw tokens, or production log payloads.

## Entries

- [`2026-09-14-security-audit.md`](2026-09-14-security-audit.md)
- [`2026-09-15-security-hardening.md`](2026-09-15-security-hardening.md)
- [`2026-09-15-documentation-system.md`](2026-09-15-documentation-system.md)
- [`2026-09-15-eshop-product-architecture-plan.md`](2026-09-15-eshop-product-architecture-plan.md)
- [`2026-09-15-e01-api-integration-fixtures.md`](2026-09-15-e01-api-integration-fixtures.md)
- [`2026-09-15-e02-runtime-and-license-assessment.md`](2026-09-15-e02-runtime-and-license-assessment.md)
- [`2026-09-15-e03-orders-domain-slice.md`](2026-09-15-e03-orders-domain-slice.md)
- [`2026-09-15-e04-inventory-reservations-and-snapshots.md`](2026-09-15-e04-inventory-reservations-and-snapshots.md)
- [`2026-09-15-e05-payment-attempts-and-reconciliation.md`](2026-09-15-e05-payment-attempts-and-reconciliation.md)
- [`2026-09-15-e06-merchant-configuration-and-entitlements.md`](2026-09-15-e06-merchant-configuration-and-entitlements.md)
