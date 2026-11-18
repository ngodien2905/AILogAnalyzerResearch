# Researcher Interface

The Blazor researcher workspace covers the administrative workflow in Milestone 3.

## Pages

- `/` is the role-selection entry point.
- `/researcher` shows participant, interaction, review, submission, and assessment progress.
- `/researcher/participants` creates anonymous participants and coding tasks, then links them as task assignments.
- `/researcher/review` displays every AI interaction and supports category, validity, status, and reviewer notes.
- `/researcher/assessments` lists source submissions and records the five-part 10-point coding rubric.
- `/researcher/summary` compares reviewed AI-usage metrics, coding scores, and optional research outcomes. It also provides CSV and XLSX exports.
- `/participant` lets a participant select an anonymous ID and task, record or import AI logs, and upload source code.

The source download route is `/api/submissions/{submissionId}/download`. It uses the configured file storage service, so the same UI works with local storage and private Supabase Storage.

## Research metric rule

Only interactions marked valid with a `Reviewed` status contribute to participant research metrics. A saved assessment may contain zero scores; the absence of an assessment remains a separate database state.

The summary page makes that distinction visible: an assessed score of zero is shown as `0.0 / 10`, while an assignment without an assessment is shown as `Missing`.
