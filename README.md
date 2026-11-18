# AI Coding Research Log Analyzer

A lightweight research-support web application for collecting and reviewing how students use AI coding assistants during programming tasks. The application links anonymized AI interaction logs and source-code submissions to coding assessments and research outcomes.

## Current status

| Milestone | Status | Summary |
| --- | --- | --- |
| 1. Database Code First | Complete | Eight-entity EF Core model, PostgreSQL migration, snake-case schema, Supabase SQL script |
| 2. Application services | Complete | Catalog, log import, storage, review, submission, assessment, metrics and demo seeding |
| 3. Researcher interface | Complete | Dashboard, participant/task management, log review and rubric assessment |
| 4. Participant interface | Complete | Role split, assignment selection, manual/imported logs and source upload |
| 5. Metrics and export | Complete | Participant summary, research outcomes, CSV/XLSX export and explicit missing-value handling |
| 6. Supabase and deployment | Not started | Production database, private bucket, secrets and deployment configuration |
| 7. End-to-end verification | Not started | Full workflow, responsive, PostgreSQL and production smoke tests |

Detailed checklist: [PROJECT_PROGRESS.md](PROJECT_PROGRESS.md)

## Role workspaces

- `/` — choose Participant or Researcher workspace.
- `/participant` — select an anonymous participant ID and assigned task, record/import AI interactions, and upload source code.
- `/researcher` — view collection and review progress.
- `/researcher/participants` — manage participants, coding tasks and assignments.
- `/researcher/review` — validate and classify AI interactions.
- `/researcher/assessments` — download source submissions and save the 10-point rubric.
- `/researcher/summary` — compare AI-usage metrics, coding scores and outcomes, then export the research dataset.

The current development build uses anonymous participant selection without authentication. Production access control is still required.

## Implemented research workflow

1. A researcher creates an anonymous participant and assigns a coding task.
2. The participant records interactions manually or imports JSON, CSV or TXT logs.
3. The participant uploads `.java`, `.cs`, `.js`, `.py` or `.zip` source files.
4. The researcher checks validity, assigns an AI-usage category and changes review status.
5. The researcher downloads the source and records correctness, quality, efficiency, testing and explanation scores.
6. Metrics include only interactions that are both valid and reviewed.

## Technology

- .NET 8 and Blazor Interactive Server
- Entity Framework Core Code First
- PostgreSQL through Npgsql
- In-memory database for the zero-configuration development demo
- Local file storage with an optional private Supabase Storage implementation
- xUnit service tests
- ClosedXML for the SPSS-preparation XLSX export and data dictionary

## Run locally

Prerequisites: .NET 8 SDK or a newer SDK capable of targeting .NET 8.

```powershell
git clone https://github.com/ngodien2905/AILogAnalyzerResearch.git
cd AILogAnalyzerResearch
dotnet restore
dotnet run --project AILogAnalyzerResearch/AILogAnalyzerResearch.csproj --launch-profile https
```

Open `https://localhost:7141`. Development mode seeds anonymous demonstration data automatically when no PostgreSQL connection string is configured.

## Use PostgreSQL

Configure the connection string with user secrets or an environment variable; do not commit database passwords.

```powershell
dotnet user-secrets --project AILogAnalyzerResearch/AILogAnalyzerResearch.csproj set "ConnectionStrings:ResearchDb" "Host=localhost;Port=5432;Database=ai_log_analyzer;Username=postgres;Password=YOUR_PASSWORD"
dotnet ef database update --project AILogAnalyzerResearch/AILogAnalyzerResearch.csproj
```

Without `ConnectionStrings:ResearchDb`, the application intentionally falls back to the in-memory demo database.

For Supabase setup, migration choices and secret names, see [Database Code First](docs/DATABASE_CODE_FIRST.md).

## Verify the project

```powershell
dotnet build AILogAnalyzerResearch.sln -c Release
dotnet test AILogAnalyzerResearch.sln -c Release --no-build
dotnet ef migrations has-pending-model-changes --project AILogAnalyzerResearch/AILogAnalyzerResearch.csproj
```

Current local verification:

- Release build: 0 warnings, 0 errors
- Automated tests: 11 passed
- EF model: no changes missing from the migration

## Documentation

- [Database and migrations](docs/DATABASE_CODE_FIRST.md)
- [Application services](docs/APPLICATION_SERVICES.md)
- [Researcher interface](docs/RESEARCHER_UI.md)
- [Role workflows](docs/ROLE_WORKFLOWS.md)
- [Metrics and research export](docs/METRICS_EXPORT.md)

## Remaining MVP work

- Authentication and role authorization
- Real PostgreSQL/Supabase integration and private storage bucket verification
- Container deployment and end-to-end tests

The project intentionally does not implement an AI assistant, automatic AI-usage classification, background monitoring of AI websites, an online IDE, or automatic code execution.
