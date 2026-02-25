0) Codex Operating Rules (copy/paste once)

Use this as your “system prompt” to Codex for the repo:

Codex Prompt (Rules)

Work in small PR-sized steps; one feature per task.

Never rename folders/namespaces unless asked.

Always update: README.md, .http file, and tests when adding endpoints.

Prefer minimal, high-confidence changes.

Use dotnet build and dotnet test after each step.

Add a Git commit after each completed step with a meaningful message.

If you need to add packages, do it via dotnet add package ... and explain why.

Keep Clean Architecture-ish layering: Domain / Infrastructure / Controllers / Services / Authorization.

(If README.md does not exist, create it at repo root with: overview, prerequisites (.NET SDK version),
run instructions, migrations/DB notes, seed users, how to use .http, how to run tests,
and GitHub Codespaces instructions. Assignment 2 feedback requires Codespaces-ready README.)

(If there is no test project yet, create whistleblowerNews.Tests with WebApplicationFactory
and add it to the solution so integration tests can be added in A3/A4.)

(That matches how Codex is intended to work: context + incremental work + checkpoints. )

Status / TODO (update as work completes)

- [x] A0 Repo scaffolding (README + tests)
- [x] A1 Domain entities + EF mappings (Articles/Comments)
- [x] A2 Articles endpoints (public reads + writer/editor rules)
- [x] A3 Authorization policies + integration tests
- [x] A4 Comments endpoints + rules + tests
- [x] B0 Whistleblower design doc + diagrams + README link
- [x] B1 Whistleblower domain + migration
- [x] B2 Anonymous report flow + tests
- [x] B3 Investigator workflow + audit + tests

Note: No Swagger/UI required. We will build our own UI separately.

Assignment coverage checklist (ensure nothing is missed)

Assignment 2 (Authorization):
- [x] Guest can read articles and comments (public GET endpoints)
- [x] Subscriber can create/update/delete their own comments
- [x] Editor can read/edit/delete all articles
- [x] Editor can read/edit/delete all comments
- [x] Writer can read all articles
- [x] Writer can create/edit/delete their own articles
- [x] Writer can delete comments on their own articles
- [x] Integration tests prove each rule (Guest/Subscriber/Writer/Editor)
- [x] REST API only (no ads, no UI)

Assignment 1 (Secure design):
- [x] Document Requirements, Security requirements, Use-cases, Misuse-cases
- [x] Include diagrams (Mermaid) that render on GitHub
- [x] Anonymous reporting supported (no identity required)
- [x] Investigator workflow supported (request info, update status)
- [x] Reporter can follow case progress securely (token-based access)
- [x] Store only hashed reporter secret (no plaintext)
- [x] Tests prove reporter token checks + investigator access

# TODO (Codex) — SSD Security Polish

## [x] 1) Reporter token: move from query string → header (keep PoC compatible)

Goal: Avoid leaking reporter token via browser history, server logs, referrers, proxies.

Tasks
* Update reporter “follow progress” endpoint to read token from header:
* Prefer header: `X-Reporter-Token`
* Optionally allow query string `?token=` for backward compatibility (PoC), but:
* If query token is used, log a warning (without logging the token)
* Mark query token as deprecated in docs

Files likely involved
* `Controllers/ReportsController.cs`
* Any shared token parsing helper (if exists)

Acceptance criteria
* Reporter can access report status using: `X-Reporter-Token: <token>`
* Token value is never written to logs
* Docs updated to recommend header usage
* Existing tests updated + new test added for header behavior

## [x] 2) Rate limiting (anti brute-force) for report/token endpoints

Goal: Mitigate token guessing, abuse, and API spam.

Scope (minimum)
* Apply rate limiting to:
* Reporter access endpoints (where token is used)
* Report submission endpoint (anonymous reporting)
* Use per-IP throttling (simple and effective)

Implementation idea (ASP.NET Core)
* Use built-in ASP.NET Core rate limiting middleware (if target framework supports it)
* Define rate-limit policies such as:
* `reporter-token-policy`: stricter (e.g., 5 req / 10 sec)
* `report-submit-policy`: moderate (e.g., 3 req / 60 sec)

Acceptance criteria
* Excess requests return `429 Too Many Requests`
* Policies are named and applied explicitly (not “global surprise”)
* Docs mention:
* why it exists
* rough limits
* that values are PoC and should be tuned in production

## [x] 3) Threat model mini-table in `docs/whistleblower-design.md`

Goal: Show SSD maturity with a compact threat model that is easy to grade.

Tasks
Add a small table:

| Threat | Impact | Mitigation | Residual Risk |
| ------ | ------ | ---------- | ------------- |

Include at least these threats:
* Token leakage via URLs/logs
* Token brute force / enumeration
* Investigator abuse / overreach
* SQL injection (even if EF/param queries used)
* Sensitive data exposure via error messages
* Insider access to audit logs

Acceptance criteria
* Table exists and is specific to this implementation
* Each mitigation points to a concrete control (policy/handler/hash/rate-limit/etc.)

## [x] 4) Audit log integrity statement (append-only)

Goal: Make it explicit that audit trails are not editable (integrity).

Tasks
* Add one sentence in `docs/whistleblower-design.md` under audit logging:
* Example: “The audit log is append-only; the API exposes no update/delete endpoints for audit entries.”

Acceptance criteria
* Doc contains the explicit statement
* Repo contains no endpoint that edits audit entries

## [x] 5) Security docs: explicitly document tradeoffs (PoC vs production)

Goal: Make the design look intentional and mature (a big grading boost).

Tasks
In `docs/whistleblower-design.md`, add a “PoC limitations / production hardening” section including:
* Rate limit tuning + WAF
* Token transport header requirement + HTTPS-only
* Secrets storage + rotation
* Monitoring and alerting
* Optional: secure message channel / metadata minimization

Acceptance criteria
* Section exists
* It clearly differentiates “what we did now” vs “what we’d do in production”

## [x] 6) Tests update plan

Goal: Prove the security changes didn’t break behavior.

Tasks
* Update existing tests that use `?token=`
* Add new tests:
* Reporter access works via `X-Reporter-Token`
* Rate limiting returns 429 after threshold (integration test if feasible)

Acceptance criteria
* `dotnet test` passes
* Token is verified correctly and constant-time comparison is still used
* No test ever prints secrets into output/logs

## [x] 7) README quick updates

Goal: Make the repo “submit-ready” and easy to run.

Tasks
* In `README.md`, update examples:
* show header usage for reporter token
* mention rate limiting exists
* how to run tests

Acceptance criteria
* README contains exact curl examples (or Swagger notes) showing header token usage
* README mentions that query token is deprecated (if kept)

# After this: Frontend phase

Once the above is done and stable:
* Start minimal frontend (case submission + reporter follow-up + investigator view)
* Focus on safe UX (no accidental identity collection, no token stored in localStorage, no token in URL)

1) Git checkpoints (do this before each Codex task)

In terminal:

git status
git add -A
git commit -m "chore: checkpoint before <TASK>"

Codex docs recommend using Git checkpoints because it can modify the codebase.

2) Task plan for Codex (Assignment 2 first, then Assignment 1)
Phase A — Finish Assignment 2 (Authorization on news site)

This is the best “scaffolding” and gives you tests + policies you can reuse later.

Task A0 — Repo scaffolding (README + tests)

Open files before prompting: root, solution, existing .http, any test folders.

Codex prompt:

If README.md is missing, create it with overview, prerequisites (.NET SDK version),
run instructions, migrations/DB notes, seed users, .http usage, tests, and GitHub Codespaces instructions.
If no test project exists, create whistleblowerNews.Tests using xUnit + WebApplicationFactory
and add it to the solution.

Acceptance:

README.md exists and is Codespaces-ready

Test project exists and builds

Task A1 — Add domain entities + EF mappings

Open files before prompting: Domain/*, Infrastructure/ApplicationDbContext.cs, migrations folder.

Codex prompt:

Add Domain entities Article and Comment with proper relationships to User. Update ApplicationDbContext with DbSet<Article> and DbSet<Comment>. Add EF Core configuration (fluent API if needed) to enforce:

Article.AuthorId FK to Users

Comment.UserId FK to Users

Comment.ArticleId FK to Articles
Generate a migration and update database. Ensure dotnet build passes.

Acceptance:

Migration created

DB has tables Articles + Comments

Build passes

Task A2 — CRUD endpoints for Articles (no auth yet)

Open: Controllers/*, .http, README.md

Codex prompt:

Implement REST endpoints for Articles:

GET /articles (public)

GET /articles/{id} (public)

POST /articles (requires Writer)

PUT /articles/{id} (Writer owner OR Editor)

DELETE /articles/{id} (Writer owner OR Editor)
Use DTOs in Application/Articles/*. Update .http with example requests and README with how to run.

Acceptance:

Endpoints exist and compile

Public reads work

Writer/editor rules not yet enforced? (If you want, enforce now—see A3)

Task A3 — Implement authorization policies (the core learning)

This is where you impress the teacher.

Codex prompt:

Implement authorization using ASP.NET Core policies + handlers:

Policy IsWriter (role claim Writer)

Policy IsEditor (role claim Editor)

Resource policy for Article: Writer can modify only their own articles; Editor can modify all.
Add authorization checks in controllers using IAuthorizationService.
Add at least 6 integration tests (WebApplicationFactory) verifying access for Guest/Subscriber/Writer/Editor.

Acceptance:

Tests prove rules

dotnet test passes

You can demo “writer can’t edit others’ article” live

Task A4 — CRUD endpoints for Comments + rules

Codex prompt:

Implement Comment endpoints:

GET /articles/{id}/comments (public)

POST /articles/{id}/comments (Subscriber)

PUT /comments/{id} (Subscriber owner OR Editor)

DELETE /comments/{id} (Subscriber owner OR Editor)

DELETE /articles/{articleId}/comments/{commentId} (Writer if article owner)
Add policies/handlers and integration tests.

Acceptance:

All policy rules from assignment 2 are met

Tests cover each role/action

Phase B — Add whistleblower module (Assignment 1: secure design)

We’ll reuse your auth + policy patterns but introduce anonymity correctly.

Task B0 — Document secure design (required by Assignment 1)

Codex prompt:

Create docs/whistleblower-design.md with:
Requirements, Security requirements, Use-cases, Misuse-cases.
Add at least 2 diagrams using Mermaid (sequence + component, or similar).
Link this doc from README.md.

Acceptance:

Design doc exists and is linked in README

Diagrams render on GitHub

Task B1 — Create whistleblower domain model

Codex prompt:

Add whistleblower domain:

Report (CaseId, Title, Description, Status, CreatedAt)

ReportMessage (CaseId, SenderType, Content, CreatedAt)

InvestigatorAssignment (CaseId, InvestigatorUserId)

ReporterSecret (CaseId, SecretHash) // store hashed secret, not plaintext
Add EF tables + migration.

Acceptance:

Tables exist

No plaintext reporter token stored

Task B2 — Anonymous create report + follow progress

Codex prompt:

Implement endpoints:

POST /reports (anonymous) returns { caseId, reporterToken } (token shown once)

GET /reports/{caseId}?token=... returns status + messages
The token must be hashed (reuse PasswordHasher) and verified constant-time.
Add tests verifying:

wrong token denied

correct token allowed

no auth required for reporter flow

Acceptance:

Anonymous workflow works

Secure token handling demonstrated

Task B3 — Investigator workflow

Codex prompt:

Implement investigator endpoints (requires role Investigator or Editor):

POST /reports/{caseId}/request-info

PATCH /reports/{caseId}/status
Add audit log entries (simple table or structured logger).
Add tests verifying investigator-only access and case assignment checks.

Acceptance:

Least privilege enforced

Audit evidence exists for teacher
