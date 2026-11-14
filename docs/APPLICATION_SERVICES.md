# Application Service Layer

Milestone 2 implements the business operations required by the three research workflows. The services use `IDbContextFactory<ResearchDbContext>` and therefore run against PostgreSQL in production and the in-memory provider during local development and tests.

## Services

| Service | Responsibility |
| --- | --- |
| `ResearchCatalogService` | Create participants and coding tasks assign tasks and record optional research outcomes |
| `ResearchInteractionService` | Add manual interactions import logs detect duplicates flag invalid data and apply researcher review |
| `ResearchSubmissionService` | Store source files maintain the final submission and save rubric assessments |
| `ResearchMetricsService` | Calculate dashboard counts participant summaries category proportions and coding scores |
| `DemoDataSeeder` | Create a repeatable local dataset when development seeding is enabled |

## Log import rules

- Supported formats are JSON CSV and TXT.
- JSON can be an array or an object containing `logs` `interactions` or `data`.
- CSV column aliases support the browser extension field names described in the research document.
- TXT requires `Prompt:` and `Response:` sections and accepts optional participant task platform plan timestamp and category headers.
- A missing participant or task assignment rejects the row because the interaction cannot be linked to the research dataset.
- Missing prompt response or platform data is stored with `is_valid = false` so a researcher can inspect and correct or exclude it.
- An extension category remains preliminary with `Pending` review status.
- Re-imported rows are skipped. Explicit timestamps use exact timestamp matching. Rows without timestamps use participant task platform prompt and response content matching.

## Source submission rules

- Accepted formats are `.java` `.cs` `.js` `.py` and `.zip`.
- The maximum source file size is 20 MB.
- File names are reduced to their final path segment before storage.
- Saving a new final submission clears the previous final flag for the same assignment.
- If database persistence fails after upload the stored object is deleted.
- Rubric bounds are 4 points for correctness 2 for code quality 1 for efficiency 1 for testing and 2 for explanation.

## Storage selection

`IFileStorageService` selects Supabase Storage only when both `Supabase:Url` and `Supabase:ServiceRoleKey` are configured. Otherwise it stores files below `App_Data/uploads` or `Storage:LocalPath`.

The Supabase service role key remains server-side. Upload download and delete operations use the Storage API instead of modifying the `storage` schema.

## Metrics rules

- Metrics include only valid interactions with `Reviewed` status.
- Pending invalid and excluded interactions do not contribute to research counts or proportions.
- Metrics are calculated from interactions and are not persisted in a separate table.
- A missing assessment returns `null`.
- A completed assessment with all zero scores returns `0` so missing data remains distinguishable from a real zero.

## Local demo data

`Database:SeedDemoData` is false in the base configuration and true only in the development configuration. The seeder stops when participant data already exists and is safe to call more than once.

## Verification

Run all service tests from the solution directory.

```powershell
dotnet test .\AILogAnalyzerResearch.sln
```
