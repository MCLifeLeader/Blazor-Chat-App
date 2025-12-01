# Repository Guidelines

## Project Structure & Module Organization
- Core solution: `Blazor.Chat.App.sln` under `src/Blazor.Chat.App/` with services split by responsibility.
- API: `Blazor.Chat.App.ApiService/` exposes the backend endpoints; shared defaults in `Blazor.Chat.App.ServiceDefaults/`.
- Data layer: `Blazor.Chat.App.Data/` contains EF Core models and migrations; SQL Server backing services spin up via `containers/docker-compose-common.yml`.
- Web UI: `Blazor.Chat.App.Web/` hosts the Blazor front end; `Blazor.Chat.App.AppHost/` orchestrates the Aspire host for local runs.
- Tests live alongside projects: `*.Tests/` folders use NUnit; infra scripts live at repo root and `devops/`.

## Build, Test, and Development Commands
- Restore/build all projects: `dotnet restore Blazor.Chat.App.sln` then `dotnet build Blazor.Chat.App.sln -c Release`.
- Run the Aspire host locally: `dotnet run --project src/Blazor.Chat.App/Blazor.Chat.App.AppHost`.
- Execute the full test suite with coverage: `dotnet test Blazor.Chat.App.sln --collect:"XPlat Code Coverage"`.
- Start local infra (SQL, messaging, smtp4dev): `./docker_setup.ps1`; stop with `./docker_down.ps1`.
- Apply data migrations during local setup: `dotnet ef database update -p src/Blazor.Chat.App/Blazor.Chat.App.Data -s src/Blazor.Chat.App/Blazor.Chat.App.ApiService`.

## Coding Style & Naming Conventions
- Follow `.editorconfig`: 4-space indentation, UTF-8, `nullable` and `implicit usings` enabled.
- Use PascalCase for types/namespaces, camelCase for locals/fields, and suffix interfaces with `I`.
- Favor async/await; keep DI registrations in `Program.cs` grouped by feature.
- Run analyzers/linting implicitly via the SDK; keep files scoped to their feature folder.

## Testing Guidelines
- NUnit with FluentAssertions and NSubstitute; in-memory EF Core for data tests.
- Name tests with behavior-first style (`Method_Should_ExpectedResult`).
- Prefer arranging fixtures through test helpers/fakes in each `*.Tests` project; avoid hitting external services outside Aspire test hosts.
- Require green `dotnet test` before opening a PR; include coverage artifacts when relevant.

## Commit & Pull Request Guidelines
- Mirror existing history: short, imperative subjects with optional scope and PR/issue tag (e.g., `Fix OpenAPI services (#14)`).
- Write commits that stay focused; avoid bundling refactors with feature work.
- PRs should describe intent, key changes, and test evidence; link issues/tickets and include UI screenshots when touching the Blazor front end.
- Flag breaking changes, new migrations, and any config updates in the PR description.

## Security & Configuration
- Do not commit secrets; keep connection strings and passwords in user secrets or env vars. Default passwords in docs are for local use only.
- Prefer containerized dependencies via `containers/`; align local configs with `appsettings.*` and `ServiceDefaults` conventions.
