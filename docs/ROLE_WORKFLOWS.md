# Role Workflows

The application separates the two actors described by the research design.

## Participant workspace

Route: `/participant`

Participants select their anonymous participant ID and an assigned coding task. They can record a prompt and AI response manually, import JSON CSV or TXT logs, and upload a final or draft source file. New interactions enter the researcher review queue with Pending status.

The MVP stores testing evidence and source files but does not compile or execute submitted code. A pre test or post test module requires a separate question and attempt model and remains pending scope confirmation.

## Researcher workspace

Routes use the `/researcher` prefix.

Researchers manage participants tasks and assignments, review and classify AI interactions, download source submissions, and save rubric assessments. Only reviewed and valid interactions are included in research metrics.

## Development identity model

The current local MVP uses anonymous participant selection without authentication. Production deployment should replace this selector with participant authentication or a one time study access code so one participant cannot open another participant record.
