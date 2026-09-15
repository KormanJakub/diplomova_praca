# KorSoft ESHOP: Product and Architecture Plan

Prepared: 2026-09-15. Status: proposed future work; documentation only.

The product owner has established **KorSoft ESHOP** as the commercial product name and requested a separate administrator site at `admin.<customer-domain>.sk`. ESHOP will be sold to multiple businesses with different configurations and commercial packages. WAFFL is the existing storefront implementation and potential first customer configuration, not the future platform identity.

No architecture, package, database, DNS, hosting, or runtime change is implemented by these documents. Existing identifiers such as `nia_api`, `waffl.sln`, and `__Host-waffl_session` remain in the source. [CONTEXT.md](../../CONTEXT.md) documents that implementation.

## Reading order

1. [Architecture audit](01-architecture-audit.md): evidence, remaining weaknesses, and commercial blockers.
2. [Product and reuse strategy](02-product-and-reuse.md): customer isolation, editions, configuration, reusable modules, and operating model.
3. [Backend architecture](03-backend-architecture.md): boundaries, dependency rules, use cases, persistence, and clean-code conventions.
4. [Admin and domain separation](04-admin-and-domains.md): separate frontend builds, routing, authentication, DNS, and rollout.
5. [Libraries and build-versus-buy](05-libraries-and-platform.md): verified candidates, existing code to simplify, and dependency governance.
6. [Delivery roadmap](06-delivery-roadmap.md): ordered backlog, acceptance criteria, experiments, release gates, and decisions.
7. [Dependency and license inventory](07-dependency-and-license-inventory.md): runtime lifecycle, NuGet/npm package licenses, asset IP audit, and governance.

## Recommended baseline

- One maintained product codebase and versioned releases.
- Separate storefront and administration applications in one frontend workspace.
- A modular monolith backend: one application deployment with enforceable module and layer boundaries.
- A dedicated application/database/storage/credential scope per customer for the first commercial release.
- Reusable catalog, orders, inventory, payment, delivery, identity, media, and notification modules; clothing personalization is optional.
- Trusted configuration and server-enforced entitlements for commercial packages; security controls are included in every edition.
- Incremental extraction from the existing implementation, with contract tests and migration checkpoints.

These are recommendations, not approved infrastructure purchases or final pricing decisions. Shared SaaS, customer-managed installation, identity provider, exact domains, database replacement, prices, service commitments, and package licenses require the decisions listed in the roadmap.

## How to maintain plans

- Keep present behavior in `CONTEXT.md` and proposed behavior here.
- Give backlog items stable IDs and explicit acceptance criteria.
- Mark items `Planned`, `In progress`, `Verified`, or `Deferred`; default is `Planned`.
- Record the verified commit and evidence before marking implementation complete.
- Add a dated history entry for actual changes; a plan is not evidence that a feature exists.
- Record accepted architectural decisions with context, alternatives, consequences, owner, and date. Do not silently turn proposals into constraints.
