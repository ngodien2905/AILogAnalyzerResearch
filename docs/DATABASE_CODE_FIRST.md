# Database Code First Setup

Milestone 1 implements the target research dataset as an EF Core Code First model backed by PostgreSQL. The migration is the source of truth. The SQL file under `supabase` is generated from that migration and should not be edited manually.

## Schema

| Table | Purpose | Important rule |
| --- | --- | --- |
| `participants` | Anonymous research participants | `participant_code` is unique |
| `coding_tasks` | Programming tasks used by the study | `task_code` is unique |
| `task_assignments` | Participant and task join | Participant and task pair is unique |
| `ai_interactions` | Prompt response platform and time | Belongs to one assignment |
| `ai_classifications` | Manual usage category and review status | At most one row per interaction |
| `source_submissions` | Private source file metadata | File bytes are stored outside PostgreSQL |
| `coding_assessments` | Rubric scores | At most one row per submission and total scale is 10 |
| `research_outcomes` | Optional pre test post test and survey values | Metric name is unique per participant |

Research metrics such as prompt count debugging count and code generation proportion are calculated from valid reviewed interactions. They are not stored in a separate metrics table.

## Rubric constraints

- Correctness is from 0 to 4.
- Code quality is from 0 to 2.
- Solution efficiency is from 0 to 1.
- Testing and error handling is from 0 to 1.
- Ability to explain is from 0 to 2.
- The maximum total score is 10.

## Create a new migration

Run from the solution directory.

```powershell
dotnet ef migrations add MigrationName `
  --project .\AILogAnalyzerResearch\AILogAnalyzerResearch.csproj `
  --startup-project .\AILogAnalyzerResearch\AILogAnalyzerResearch.csproj `
  --output-dir Data\Migrations
```

Then confirm that the model and migration match.

```powershell
dotnet ef migrations has-pending-model-changes `
  --project .\AILogAnalyzerResearch\AILogAnalyzerResearch.csproj `
  --startup-project .\AILogAnalyzerResearch\AILogAnalyzerResearch.csproj
```

## Configure the database securely

For local development use .NET user secrets instead of committing the password.

```powershell
dotnet user-secrets set "ConnectionStrings:ResearchDb" `
  "Host=localhost;Port=5432;Database=ai_log_analyzer;Username=postgres;Password=YOUR_PASSWORD" `
  --project .\AILogAnalyzerResearch\AILogAnalyzerResearch.csproj
```

For a deployed app set the environment variable `ConnectionStrings__ResearchDb`. A Supabase Npgsql connection string has this form:

```text
Host=YOUR_HOST;Port=5432;Database=postgres;Username=YOUR_USERNAME;Password=YOUR_PASSWORD;SSL Mode=Require
```

Copy the exact host port and username from the Supabase Connect dialog. Do not construct the shared pooler hostname manually. Keep the database password and service role key on the server only.

## Apply migrations

The recommended production flow runs migrations as a deployment step.

```powershell
dotnet ef database update `
  --project .\AILogAnalyzerResearch\AILogAnalyzerResearch.csproj `
  --startup-project .\AILogAnalyzerResearch\AILogAnalyzerResearch.csproj `
  --connection "$env:RESEARCH_DB_MIGRATION_CONNECTION"
```

Use the Supabase direct connection for migrations when it is reachable. If the deployment network is IPv4 only use the session pooler on port 5432. The app keeps `Database:ApplyMigrationsOnStartup` disabled by default to avoid two app instances trying to migrate at the same time.

## Apply through the Supabase SQL editor

As an alternative open the Supabase SQL Editor and run:

```text
supabase/schema.sql
```

The generated script is idempotent and records `InitialResearchSchema` in `__EFMigrationsHistory`.

## Generate the Supabase SQL script again

```powershell
dotnet ef migrations script --idempotent `
  --project .\AILogAnalyzerResearch\AILogAnalyzerResearch.csproj `
  --startup-project .\AILogAnalyzerResearch\AILogAnalyzerResearch.csproj `
  --output .\supabase\schema.sql
```
