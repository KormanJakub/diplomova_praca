# Security Audit Baseline — 2026-09-14

## Summary

A static review of the ASP.NET Core API, tests, and security-relevant Angular flows identified critical authorization, payment, order-integrity, file-upload, credential, password, and session-management issues.

## Motivation

The review was performed before additional feature work so that publicly reachable destructive operations, payment trust violations, and sensitive-data exposure could be prioritized.

## Main findings at that time

- Administrator routes were not protected by effective server-side role authorization.
- A routable seed operation could erase application data.
- Client-bound database models allowed profile privilege escalation and leaked internal user fields.
- Guest order IDs allowed unauthorized tracking, cancellation, or payment-state changes.
- Stripe amounts and payment completion relied on browser-provided values.
- Customizations were not consistently checked for ownership or reuse.
- File mutations were public and upload validation was insufficient.
- Secrets had existed in tracked configuration or tests and required rotation.
- Password hashing, password-reset tokens, JWT validation, CORS, and login throttling required hardening.
- Frontend session handling was inconsistent between cookies and local storage.

## Scope and limitations

The assessment was static. It did not penetration-test a deployed environment or access production data. Package-audit results did not establish application security.

## Outcome

The findings drove the hardening work recorded in [`2026-09-15-security-hardening.md`](2026-09-15-security-hardening.md). The detailed original assessment remains in [`../audit-2026-09-14.md`](../audit-2026-09-14.md).

## Remaining risk at the close of this day

No remediation had yet been treated as complete. Production credentials were considered exposed until rotated outside the repository.

