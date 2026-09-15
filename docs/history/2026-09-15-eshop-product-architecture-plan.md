# KorSoft ESHOP Product and Architecture Planning — 2026-09-15

## Summary

The owner established KorSoft ESHOP as a standalone commercial product intended for multiple customer businesses, requested an admin subdomain, and requested architecture planning without implementation.

## Motivation

The existing WAFFL store needed a documented path to reusable modules, maintainable backend boundaries, customer configuration, independent administration and differentiated commercial offers.

## Work completed

- Inspected controller responsibilities, persistence coupling, order transitions, tests, frontend routing, configuration and dependency usage.
- Documented additional architectural gaps, including direct admin lifecycle changes, incomplete integration coverage and multi-write consistency windows.
- Prepared proposed product, backend, domain separation, library and delivery plans in `docs/planning`.
- Consulted official framework/library/provider documentation for candidate capabilities and runtime support.
- Updated current documentation identity and linked the planning documents while keeping the runtime baseline explicit.

## Affected files and contracts

Markdown documentation only. Existing source identifiers, API routes, database schema, cookies, dependencies and deployments were not changed in this workstream.

## Data and deployment impact

None. Dedicated customer installations, separate admin artifacts, DNS/TLS changes, provider selection, prices and clean-architecture extraction remain proposed work.

## Verification

Reviewed plan statements against source; checked internal document links and whitespace. No application build or tests were run for this documentation-only workstream. Previous build/test results remain historical, not new evidence.

## Follow-up

Start implementation only under a subsequent implementation request, using the decision register, experiments and acceptance criteria in `docs/planning/06-delivery-roadmap.md`.
