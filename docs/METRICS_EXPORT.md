# Metrics and Research Export

Milestone 5 provides a researcher-facing participant summary at `/researcher/summary` and downloadable datasets at:

- `/api/exports/research-dataset.csv`
- `/api/exports/research-dataset.xlsx`

## Dataset rules

The export contains one row per anonymous participant and coding-task assignment. A participant without an assignment still receives one row so participant-level outcomes are not silently excluded.

AI-usage counts and percentages include only interactions that are both valid and marked `Reviewed`. Counts and percentages are produced for every usage category so the dataset has a stable analysis schema.

The dataset contains:

- Anonymous participant ID, course, and optional research group
- Task ID and title
- Reviewed prompt total and category-level counts and percentages
- Source-submission and assessment indicators
- Coding rubric score
- Dynamic outcome columns for recorded measures such as pre-test, post-test, self-efficacy, or over-reliance

## Missing values and zero

Blank cells mean that a value is missing. Numeric `0` is retained as a real observed value. In particular:

- `coding_score = 0` means that an assessment exists and its rubric total is zero.
- Blank `coding_score` means that no assessment exists.
- `outcome_* = 0` means that the recorded outcome is zero.
- Blank `outcome_*` means that the outcome was not recorded for that participant.

This distinction is covered by an automated CSV and XLSX test.

## File formats

The CSV export is UTF-8 with a byte-order mark for reliable opening in spreadsheet software. The XLSX workbook contains a `Research Dataset` sheet and a `Data Dictionary` sheet that describes every generated column for later SPSS preparation.
