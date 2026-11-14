# AI Coding Research Log Analyzer Progress

## Milestone 1 Database Code First

- [x] Define the eight research entities from the conceptual ERD
- [x] Configure PostgreSQL with Npgsql and EF Core
- [x] Use snake case table column index and constraint names
- [x] Add unique indexes and rubric score constraints
- [x] Add a PostgreSQL design time context factory
- [x] Create `InitialResearchSchema` migration
- [x] Generate an idempotent Supabase SQL script
- [x] Build with zero warnings and zero errors
- [x] Verify that the EF model has no changes missing from the migration

## Milestone 2 Application Services

- [ ] Participant task and assignment services
- [ ] AI log JSON and CSV import validation
- [ ] Source file storage abstraction
- [ ] Research metrics queries
- [ ] Seed data for local demonstration

## Milestone 3 Researcher Interface

- [ ] Research dashboard
- [ ] Participant and coding task management
- [ ] AI log review and manual classification
- [ ] Source submission review and rubric assessment

## Milestone 4 Student Interface

- [ ] Participant and task selection
- [ ] Manual AI interaction submission
- [ ] JSON and CSV import
- [ ] Source code upload

## Milestone 5 Metrics and Export

- [ ] Participant research summary
- [ ] Missing value and zero value distinction
- [ ] Anonymized CSV export
- [ ] XLSX export for SPSS preparation

## Milestone 6 Supabase and Deployment

- [ ] Supabase PostgreSQL connection
- [ ] Private Supabase Storage bucket
- [ ] Container deployment configuration
- [ ] Production secrets and migration runbook

## Milestone 7 End to End Verification

- [ ] Test the three workflows described in the research document
- [ ] Validate responsive layouts and form errors
- [ ] Run production build and deployment smoke test
