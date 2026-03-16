# WhistleblowerNews

Backend proof-of-concept for two assignments:
- Assignment 2: authorization policies for a news site
- Assignment 1: secure whistleblower reporting (design + implementation)

## Prerequisites
- .NET SDK 10.0 (project targets net10.0)
- Optional: SQLite CLI if you want to inspect the DB file

## Run (local)
```powershell
# from repo root
dotnet run --project src/WhistleblowerNews.Web --launch-profile https
```
The app runs on https://localhost:7080 only (see `src/WhistleblowerNews.Web/Properties/launchSettings.json`).
If your client complains about the dev certificate, trust it or use `-k` with curl for local testing.

## Database and migrations
- SQLite file: `src/WhistleblowerNews.Web/whistleblowerNews.db`
- In Development, the app auto-applies migrations and seeds demo users.
- To reset the DB: delete `src/WhistleblowerNews.Web/whistleblowerNews.db` and run again.
- Optional (manual):
```powershell
dotnet ef database update --project src/WhistleblowerNews.Infrastructure --startup-project src/WhistleblowerNews.Web
```

## Seeded users (Development only)
- `subscriber` / `subscriber123` (Subscriber)
- `writer` / `writer123` (Writer)
- `editor` / `editor123` (Editor)
- `investigator` / `investigator123` (Investigator)

## Authentication
- Cookie-based authentication (MVC login page at `/Account/Login`)
- API login endpoint `/api/auth/login` sets the auth cookie
- Subscriber sign-up at `/Account/Register`
- Password policy: min 8 chars, at least one digit; lockout after 5 failed attempts (10 minutes)
- Email confirmation is required before login.
- Password reset at `/Account/ForgotPassword` and `/Account/ResetPassword`.
- Dev emails are written to `logs/dev-emails.log`.

## HTTP examples
See `src/WhistleblowerNews.Web/whistleblowerNews.http` for sample requests.

### API login example
```bash
# login and store cookie
curl -c cookies.txt -X POST https://localhost:7080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"writer","password":"writer123"}'

# call a protected endpoint with the cookie
curl -b cookies.txt https://localhost:7080/api/auth/me
```

## News site endpoints (Assignment 2)
- `GET /api/articles` (public)
- `GET /api/articles/{id}` (public)
- `POST /api/articles` (Writer)
- `PUT /api/articles/{id}` (Writer owner or Editor)
- `DELETE /api/articles/{id}` (Writer owner or Editor)
- `GET /api/articles/{id}/comments` (public)
- `POST /api/articles/{id}/comments` (Subscriber)
- `PUT /api/comments/{id}` (Subscriber owner or Editor)
- `DELETE /api/comments/{id}` (Subscriber owner or Editor)
- `DELETE /api/articles/{articleId}/comments/{commentId}` (Writer owns article)

## Whistleblower design (Assignment 1)
See `docs/whistleblower-design.md` for requirements, security requirements, use-cases, misuse-cases, and diagrams.

## Whistleblower endpoints (Assignment 1)
- `POST /api/reports` (anonymous, returns caseId + reporterToken once)
- `GET /api/reports/{caseId}` (anonymous with reporter token header `X-Reporter-Token`)
- `POST /api/reports/{caseId}/request-info` (Investigator or Editor)
- `PATCH /api/reports/{caseId}/status` (Investigator or Editor)

Reporter token transport: use the `X-Reporter-Token` header. Tokens are not accepted via query string.

### Whistleblower curl examples
```bash
# Create report (anonymous)
curl -X POST https://localhost:7080/api/reports \
  -H "Content-Type: application/json" \
  -d '{"title":"Unsafe behavior","description":"Details"}'

# Follow report (anonymous, header token)
curl https://localhost:7080/api/reports/YOUR_CASE_ID \
  -H "X-Reporter-Token: YOUR_REPORTER_TOKEN"
```

### Rate limiting (PoC)
Rate limiting is enabled for report submission and reporter token endpoints to mitigate brute-force attempts.
Default PoC limits are configurable in `src/WhistleblowerNews.Web/appsettings.json` and should be tuned for production.

## Tests
```powershell
dotnet test
```
Tests use an in-memory EF Core database.

> **Note on rate-limiting tests:** `RateLimitingTests.cs` exercises the fixed-window limiters.
> Because the in-memory test host shares rate-limit state across test runs in the same process,
> these tests may be flaky when run in parallel with other test classes.
> Run them in isolation if needed: `dotnet test --filter "FullyQualifiedName~RateLimitingTests"`.

## GitHub Codespaces
1. Open the repo in Codespaces.
2. Ensure .NET SDK 10.0 is available (`dotnet --version`).
3. Run `dotnet restore`.
4. Run `dotnet run --project src/WhistleblowerNews.Web --launch-profile https`.
5. Open forwarded port 7080 or 5048.
6. Run `dotnet test`.


## Krav
- Docker Desktop (Windows/Mac) eller Docker Engine + Docker Compose plugin (Linux/Arch)

## Start projektet
```bash
docker compose up --build
```

API er tilgængeligt på: http://localhost:8080

Tests køres automatisk under build. Hvis tests fejler, stopper buildet.

## Stop projektet
```bash
docker compose down
```
```

---

**Copilot-prompt til at tilpasse** (paste i Rider eller VS Code):
```
I have an ASP.NET Core Web API solution with this structure:
- *.sln at root
- src/<ProjectName>/<ProjectName>.csproj
- tests/<TestProject>/<TestProject>.csproj (xUnit)

Update the Dockerfile so the publish command references the correct .csproj path.
Verify the test stage runs all xUnit tests before building.