# WORKING

## Completed work
- Clean Architecture monolith conversion (Domain/Application/Infrastructure/Web).
- MVC + API in single host (areas + default routes).
- Cookie authentication (login/logout) with seeded demo users.
- Role-based areas and views: Public, Subscriber, Writer, Editor, Investigator, Whistleblower.
- Whistleblower token header-only, CSRF on MVC forms.
- Business logic moved to Application services.
- Global error handling + ProblemDetails for API.
- Security headers + health check endpoint.
- Docs updated (architecture + auth flow).
- README updated for new paths and endpoints.
- Repo scaffolding (README + tests).
- Domain entities + EF mappings (Articles/Comments).
- Articles endpoints (public reads + writer/editor rules).
- Authorization policies + integration tests.
- Comments endpoints + rules + tests.
- Whistleblower design doc + diagrams + README link.
- Whistleblower domain + migration.
- Anonymous report flow + tests.
- Investigator workflow + audit + tests.
- Serilog console logging with correlation id + user context enrichment.
- Sensitive-data redaction policy (no Authorization/cookie/token/password properties).
- Correlation ID middleware and response header `X-Correlation-ID`.
- Security event logging for 403/429 and deprecated token query usage.
- Audit service + expanded audit schema (event type, outcome, target, actor, correlation, IP, UA).
- Centralized audit helpers (Success/Denied/Failed).
- Audited: report status changes, request info, comment/article moderation, login success/failure.
- Proxy-aware IP capture for audit entries.
- Audit tests + correlation id header test + rate limit audit test.
- CSP tightened to remove inline scripts; header tradeoffs documented.
- New migration: `AuditLogEnhancements`.
- Tests: report status audit, authorization denied audit, correlation id header, rate limit audit, login failure audit.
- UX polish: enterprise layout with top nav + sidebar.
- Role-aware menu in shared layout.
- Design system with CSS variables + consistent buttons/forms/tables.
- Validation + alert styling with shared `_Alerts` partial.
- Card components on home dashboard.
- Whistleblower UX: token shown once, warnings, no URL usage, no storage.
- Public area polish: pagination, latest sorting, ads-free copy, safe rendering.
- Subscriber UX: comment CRUD polish, filters, friendly validation.
- Writer UX: CRUD polish, confirmation, comment moderation view.
- Editor UX: global moderation filters + danger zone confirmations.
- Investigator UX: status timeline + transition rules.
- Accessibility + security UX: labels, focus styles, ARIA alerts, friendly error pages.

## Assignment coverage
- Guest can read articles and comments (public GET endpoints).
- Subscriber can create/update/delete their own comments.
- Editor can read/edit/delete all articles.
- Editor can read/edit/delete all comments.
- Writer can read all articles.
- Writer can create/edit/delete their own articles.
- Writer can delete comments on their own articles.
- Integration tests cover Guest/Subscriber/Writer/Editor rules.
- REST API only for assignment 2.
- Design doc includes requirements, security requirements, use-cases, misuse-cases, diagrams.
- Anonymous reporting supported.
- Investigator workflow supported.
- Reporter can follow case progress securely (token-based access).
- Only hashed reporter secret stored.
- Tests verify reporter token checks + investigator access.

## Recent updates (2026-02-26)
- Removed legacy `whistleblowerNews` project from the repo to avoid building a project with no entry point.
- Moved HTTP examples to `src/WhistleblowerNews.Web/whistleblowerNews.http` and updated README path.
- Startup fix: `AddHttpContextAccessor()` registered before Serilog; Serilog uses `GetService<IHttpContextAccessor>() ?? new HttpContextAccessor()`.
- API HTTPS enforcement: reject non-HTTPS `/api` requests with 400; keep HTTPS redirection for non-API routes.
- HTTPS-only listeners: launch profile is HTTPS-only; test clients use HTTPS base address; docs and HTTP examples updated.
- Subscriber sign-up: registration view, validation, password hashing, audit logging, and navigation links.
- Migrated auth to ASP.NET Identity (UserManager/SignInManager, roles, lockout, Identity tables + migration).
- Login/register rate limiting added (per-IP) and tests updated for Identity password policy.
- Email verification + password reset implemented with file-based dev email delivery (`logs/dev-emails.log`).

## Next improvements (backlog)
Work order: Phase 1 → Phase 5, unless re-prioritized.

**Phase 1 — Identity and Account Security**
- Replace custom auth with ASP.NET Identity (password policy, lockout, MFA-ready, email confirmation).
- Password reset flow with token/email verification (dev SMTP stub).
- Rate limit signup and login (per-IP and per-account).
- Admin-only role assignment; remove self-service for privileged roles.
- Account lifecycle controls: disable user, force password reset, audit role changes.

**Phase 2 — Whistleblower Realism**
- Secure message channel for follow-up questions (reporter replies without identity).
- Attachments with size limits, content-type validation, AV scanning, and storage outside web root.
- Token lifecycle: one-time display, optional rotation, and printable recovery code.
- Case escalation statuses with immutable status history.

**Phase 3 — Data Integrity and Audit**
- Append-only audit log enforcement at DB level (trigger or separate schema).
- Audit log retention policy and export (CSV/JSON).
- “Who accessed case data” audit entries.

**Phase 4 — Ops and Security Hardening**
- HTTPS-only Kestrel endpoints in production config.
- Secrets management (User Secrets for dev, env/Key Vault for prod).
- Structured logging + metrics (Serilog sinks + OpenTelemetry).
- Health checks expanded (db, storage, background jobs).

**Phase 5 — UX, Product, and Testing**
- Editorial workflow: Draft → Review → Published.
- Moderation queues for comments and article changes; abuse reporting.
- Search, filters, and pagination across all list pages.
- Subscriber profile page (change password, preferences, alerts).
- Integration tests for signup, login lockout, and audit events.
- Smoke tests for all area routes and API auth behaviors.
- Load tests for report submission and reporter token endpoints.

## New TODOs (next steps)
- Add account lifecycle controls (disable user, force password reset) and audit role changes.
- Add admin-only role assignment UI/API; remove any path for self-service privileged roles.
- Add tests for login/signup rate limiting behavior.

## Product vision backlog (good, modern newsroom)
- Corrections ledger page with transparent change history.
- Source/provenance panel per article (evidence links, interviews, data sources).
- Editorial principles and funding/COI disclosure pages.
- Issue hubs that group ongoing stories with timelines and follow-ups.
- “What we know / What we don’t know” callouts for high-risk topics.
- Impact tracker showing outcomes and policy changes tied to reporting.
- Reader questions queue with public responses from editors.
- Tip line integration page with safe reporting guidance.
- Fact-check and review workflow gates before publish.
- Signal-focused feeds (local relevance, public interest, investigative).
- Accessibility toggles (high contrast, dyslexia-friendly font, read-aloud).
- Privacy-first analytics with clear data policy.
- Membership/sponsorship features tied to transparency and accountability.
- Plain-language summaries and key takeaways.
- Localized alerts for breaking safety/health/transport issues.
- Topic follows with quiet notifications (no spam).
- “Why this matters to you” section tuned by location/role.
- Open data downloads for investigative pieces (CSV/JSON).
- Public methodology notes for data-driven stories.
- Reader trust scorecards (what was verified, what is pending).
- Commentary vs reporting labels to prevent confusion.
- Time-to-read estimates and update timestamps.
- Community fact-check requests and rebuttal tracking.
- Optional anonymous feedback on articles.
- Editorial AMA sessions and Q&A archives.
- Cross‑linking related cases and ongoing investigations.
- Multi‑language summaries for major stories.
