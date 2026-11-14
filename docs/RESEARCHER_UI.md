# Researcher Interface

The Blazor researcher workspace covers the administrative workflow in Milestone 3.

## Pages

- `/` shows participant, interaction, review, submission, and assessment progress.
- `/participants` creates anonymous participants and coding tasks, then links them as task assignments.
- `/review` displays every AI interaction and supports category, validity, status, and reviewer notes.
- `/assessments` lists source submissions and records the five-part 10-point coding rubric.

The source download route is `/api/submissions/{submissionId}/download`. It uses the configured file storage service, so the same UI works with local storage and private Supabase Storage.

## Research metric rule

Only interactions marked valid with a `Reviewed` status contribute to participant research metrics. A saved assessment may contain zero scores; the absence of an assessment remains a separate database state.
