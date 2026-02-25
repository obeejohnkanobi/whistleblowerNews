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
