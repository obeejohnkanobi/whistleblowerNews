# whistleblowerNews

Backend proof-of-concept for two assignments:
- Assignment 2: authorization policies for a news site
- Assignment 1: secure whistleblower reporting (design + implementation)

## Prerequisites
- .NET SDK 10.0 (project targets net10.0)
- Optional: SQLite CLI if you want to inspect the DB file

## Configuration
JWT settings live in `whistleblowerNews/appsettings.json`. `Jwt:SigningKey` must be at least 32 characters.
You can override with environment variables, for example `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience`.

## Run (local)
```powershell
# from repo root
dotnet run --project whistleblowerNews --launch-profile http
```
The app runs on http://localhost:5089 by default (see `whistleblowerNews/Properties/launchSettings.json`).

## Database and migrations
- SQLite file: `whistleblowerNews/whistleblowerNews.db`
- In Development, the app auto-applies migrations and seeds demo users.
- To reset the DB: delete `whistleblowerNews/whistleblowerNews.db` and run again.
- Optional (manual): `dotnet ef database update --project whistleblowerNews`

## Seeded users (Development only)
- `subscriber` / `subscriber123` (Subscriber)
- `writer` / `writer123` (Writer)
- `editor` / `editor123` (Editor)
- `investigator` / `investigator123` (Investigator)

## HTTP examples
See `whistleblowerNews/whistleblowerNews.http` for login examples.
Use the login response token as `Authorization: Bearer <token>` when calling protected endpoints.

## News site endpoints (Assignment 2)
- `GET /articles` (public)
- `GET /articles/{id}` (public)
- `POST /articles` (Writer)
- `PUT /articles/{id}` (Writer owner or Editor)
- `DELETE /articles/{id}` (Writer owner or Editor)
- `GET /articles/{id}/comments` (public)
- `POST /articles/{id}/comments` (Subscriber)
- `PUT /comments/{id}` (Subscriber owner or Editor)
- `DELETE /comments/{id}` (Subscriber owner or Editor)
- `DELETE /articles/{articleId}/comments/{commentId}` (Writer owns article)

## Whistleblower design (Assignment 1)
See `docs/whistleblower-design.md` for requirements, security requirements, use-cases, misuse-cases, and diagrams.

## Whistleblower endpoints (Assignment 1)
- `POST /reports` (anonymous, returns caseId + reporterToken once)
- `GET /reports/{caseId}?token=...` (anonymous with reporter token)
- `POST /reports/{caseId}/request-info` (Investigator or Editor)
- `PATCH /reports/{caseId}/status` (Investigator or Editor)

## Tests
```powershell
dotnet test
```
Tests use an in-memory EF Core database.

## GitHub Codespaces
1. Open the repo in Codespaces.
2. Ensure .NET SDK 10.0 is available (`dotnet --version`).
3. Set `Jwt__SigningKey` (>=32 chars) in environment variables or update `whistleblowerNews/appsettings.json`.
4. Run `dotnet restore`.
5. Run `dotnet run --project whistleblowerNews --launch-profile http`.
6. Open forwarded port 5089.
7. Run `dotnet test`.
