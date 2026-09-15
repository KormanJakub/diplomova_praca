# KorSoft ESHOP Documentation

## Document types

- [`../CONTEXT.md`](../CONTEXT.md) — authoritative description of the application as it works now, including mandatory working rules.
- [`secure-setup.md`](secure-setup.md) — local secret configuration and production deployment requirements.
- [`audit-2026-09-14.md`](audit-2026-09-14.md) — historical security assessment that led to the hardening work.
- [`history`](history/README.md) — append-only dated records of material changes.
- [`planning`](planning/README.md) — proposed commercial product architecture, admin subdomain, reusable modules, library assessment and implementation backlog. Plans are not current runtime behavior.

## Maintenance rules

- Write all Markdown files in English.
- Use ISO dates and repository-relative file references.
- Never include secrets, customer data, raw capability tokens, or production log payloads.
- Update `CONTEXT.md` for current behavior and a dated history entry for what changed.
- Do not rewrite historical entries to match later behavior. Add a correction or a new dated entry instead.
