# Product, Customer Isolation, and Reuse Strategy

Status: proposed. Product identity confirmed by the owner: **KorSoft ESHOP**. Exact prices, infrastructure, and service commitments remain undecided.

## Product boundary

KorSoft owns and maintains the reusable commerce product. Each customer receives an independently branded store with its merchant identity, domains, payment account, delivery providers, catalog, content, users, orders, and operational configuration. Existing WAFFL visual identity and clothing workflows should become a customer/theme configuration and optional personalization module.

Do not use customer names in core namespaces, package names, business branches, or deployment code. Proposed code prefix: `KorSoft.EShop`; proposed frontend package scope: `@korsoft/eshop-*`. Rename existing assemblies, secrets IDs, cookie names, assets, and URLs only in a separately planned migration.

## Deployment models

| Model | Advantages | Costs and limitations | Recommendation |
| --- | --- | --- | --- |
| Dedicated runtime + DB + storage/credentials per customer | Strong operational boundary, simple merchant configuration, individual backups and rollout | More deployments and provisioning overhead | First commercial baseline; automate provisioning from one release |
| Shared runtime, database per customer | Shared compute, database separation | Tenant-aware identity/routing/caches/jobs/provider clients still required | Evaluate after onboarding several customers |
| Shared runtime and collections with tenant IDs | High density | Every query/index/cache/export/background job needs tenant enforcement; wider incident impact | Defer until isolation is proven by systematic tests |
| Customer-managed installation | Customer infrastructure ownership | Version drift, upgrade/support burden, license distribution and offline behavior | Separate commercial offering if demand justifies it |

Dedicated installation does not mean source forks. Use the same immutable artifacts, deployment templates, schema version, and module contracts with separate configuration and secrets.

If shared tenancy is later accepted: resolve a registered merchant from a verified host mapping, cross-check staff membership and resource ownership, reject unknown hosts, scope every storage/cache/job/export key, and test forged tenant headers. No merchant decision may come only from a client-provided tenant ID. KorSoft support access requires separate identity, explicit grants, audit, and revocation.

## Modules and extraction candidates

| Module | Owns | Reuse / customization point |
| --- | --- | --- |
| Catalog | Products, stable variant/SKU IDs, categories, availability presentation | Standard storefronts; no required clothing design relationship |
| Pricing | Money, discounts, tax/rounding policy, quote calculation | Merchant pricing policies; snapshot quote at order acceptance |
| Orders | Order snapshots, lifecycle, order numbering | Core commerce; no Stripe or HTTP types in domain |
| Inventory | Stock/reservations and release/commit rules | Physical goods; optional for products not tracking stock |
| Payments | Attempts, captures, refunds, reconciliation | Provider adapters; Stripe first, bank/manual payment as distinct workflows |
| Fulfillment | Delivery choices, shipments and tracking | Packeta adapter or other carrier; policy per merchant |
| Identity and Access | Customers, staff membership, permissions, sessions | Shared infrastructure conventions; staff/customer boundaries remain explicit |
| Media | Asset metadata, approved storage access | Object storage adapter, image processing and quotas |
| Notifications | Templates, delivery jobs and status | Branding/locales and SMTP/provider adapters |
| Personalization | Design selection, custom descriptions, surcharge | Optional clothing-specific add-on; generic commerce must work without it |
| Store Configuration | Branding, merchant identity, domains, locale, enabled modules | Validated configuration with public and private views |
| Reporting | Read projections and exports | Optional advanced reporting; core operational views always available |

Begin with internal project references and public interfaces. Publish versioned NuGet/npm packages only after a second real consumer proves a stable boundary. No `Common` project containing unrelated business rules, runtime DLL uploads, arbitrary customer scripts, or customer-specific subclasses of controllers.

## Configuration contract

Separate configuration into:

- Public store profile: name, logo/theme tokens, locale, public contact details, supported display currency and delivery choices.
- Merchant operations: invoice identity, bank transfer instructions, timezone, tax/pricing policy, stock behavior, support settings, retention policy.
- Secret references: provider credentials, signing keys, email credentials; never returned in storefront configuration or stored in theme files.
- Deployment identity: immutable installation ID, verified domain mapping, database/storage targets, release/schema version.
- Entitlements: enabled commercial features, quotas, effective dates, and compatibility requirements.

Validate at startup and before publishing changes. An unconfigured payment or carrier is unavailable with a useful admin diagnostic. Missing merchant bank details must prevent bank-transfer checkout. Theme schema changes need versioning and migration. Merchant content must not contain executable HTML/scripts by default.

## Commercial packaging: examples for discussion

| Draft offer | Scope | Charge drivers |
| --- | --- | --- |
| Core | Catalog, guest/customer checkout, orders, standard payments/delivery, basic staff operations | Setup, theme, hosting, maintenance, supported usage |
| Growth | Core plus personalization, advanced reporting or selected integrations | Extra modules, data volume, integration maintenance |
| Tailored | Approved adapters, additional storefronts, agreed deployment/service requirements | Scoped engineering, support effort, infrastructure and upgrade commitments |

No numeric prices are proposed without costs and expected usage. Define setup fee, recurring service, included hours, overage treatment, third-party fees, support hours, backup/restore targets, upgrade policy, termination/export process, and ownership of customer extensions.

Security patches, authorization, data isolation, backups required by the chosen service, and essential recovery must not be paid feature gates. Feature flags control rollout; contractual entitlement checks control access on the server. Frontend visibility alone cannot enforce an edition. Define downgrade behavior: preserve order history, finish accepted payments/shipments, prevent new disabled operations, and allow export. Configuration licenses for customer-managed deployments cannot be treated as tamper-proof DRM.

## Product readiness beyond architecture

- Inventory ownership and redistribution rights for code, artwork, fonts, dependencies, and themes; obtain professional contract/license review where needed.
- Verify accessibility, responsive checkout, translations, SEO/rendering requirements, and merchant-specific transactional emails.
- Decide supported countries, taxes, currencies, invoices, returns/refunds, and consent requirements with domain specialists; these documents do not establish legal compliance.
- Provide reproducible onboarding, secret provisioning, domain verification, demo data separation, release notes, backups, restore drills, monitoring, and offboarding exports.
- Keep merchant payment settlement separate from KorSoft product subscription billing. Do not assume KorSoft receives customer-store payments or enable a marketplace payment model implicitly.

Acceptance experiment: provision a clothing merchant and a plain catalog merchant from one release, with different themes, bank/provider credentials, and optional modules. Neither installation may see the other's users, files, jobs, tokens, or orders; the plain catalog merchant must not depend on personalization.
