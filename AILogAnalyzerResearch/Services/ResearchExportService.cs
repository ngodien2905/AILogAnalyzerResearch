using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AILogAnalyzerResearch.Data;
using ClosedXML.Excel;

namespace AILogAnalyzerResearch.Services;

public sealed class ResearchExportService(
    ResearchCatalogService catalogService,
    ResearchMetricsService metricsService)
{
    private sealed record ExportColumn(string Name, string DataType, string Description);
    private sealed record ExportDataset(IReadOnlyList<ExportColumn> Columns, IReadOnlyList<object?[]> Rows);

    public async Task<ResearchExportFile> CreateCsvAsync(CancellationToken cancellationToken = default)
    {
        var dataset = await BuildDatasetAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', dataset.Columns.Select(column => EscapeCsv(column.Name))));
        foreach (var row in dataset.Rows)
        {
            builder.AppendLine(string.Join(',', row.Select(value => EscapeCsv(FormatCsvValue(value)))));
        }

        var content = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(builder.ToString());
        return new ResearchExportFile(FileName("csv"), "text/csv; charset=utf-8", content);
    }

    public async Task<ResearchExportFile> CreateXlsxAsync(CancellationToken cancellationToken = default)
    {
        var dataset = await BuildDatasetAsync(cancellationToken);
        using var workbook = new XLWorkbook();
        var dataSheet = workbook.Worksheets.Add("Research Dataset");

        for (var columnIndex = 0; columnIndex < dataset.Columns.Count; columnIndex++)
        {
            dataSheet.Cell(1, columnIndex + 1).Value = dataset.Columns[columnIndex].Name;
        }

        for (var rowIndex = 0; rowIndex < dataset.Rows.Count; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < dataset.Columns.Count; columnIndex++)
            {
                SetCellValue(dataSheet.Cell(rowIndex + 2, columnIndex + 1), dataset.Rows[rowIndex][columnIndex]);
            }
        }

        if (dataset.Rows.Count > 0)
        {
            var range = dataSheet.Range(1, 1, dataset.Rows.Count + 1, dataset.Columns.Count);
            var table = range.CreateTable("ResearchDataset");
            table.Theme = XLTableTheme.TableStyleMedium2;
        }
        else
        {
            dataSheet.Range(1, 1, 1, dataset.Columns.Count).Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE6F1");
            dataSheet.Range(1, 1, 1, dataset.Columns.Count).Style.Font.Bold = true;
        }

        dataSheet.SheetView.FreezeRows(1);
        dataSheet.Columns().AdjustToContents();
        foreach (var column in dataSheet.ColumnsUsed())
        {
            if (column.Width > 42) column.Width = 42;
        }

        var dictionarySheet = workbook.Worksheets.Add("Data Dictionary");
        dictionarySheet.Cell("A1").Value = "column_name";
        dictionarySheet.Cell("B1").Value = "data_type";
        dictionarySheet.Cell("C1").Value = "description";
        for (var index = 0; index < dataset.Columns.Count; index++)
        {
            var column = dataset.Columns[index];
            dictionarySheet.Cell(index + 2, 1).Value = column.Name;
            dictionarySheet.Cell(index + 2, 2).Value = column.DataType;
            dictionarySheet.Cell(index + 2, 3).Value = column.Description;
        }

        var dictionaryRange = dictionarySheet.Range(1, 1, dataset.Columns.Count + 1, 3);
        var dictionaryTable = dictionaryRange.CreateTable("DataDictionary");
        dictionaryTable.Theme = XLTableTheme.TableStyleMedium2;
        dictionarySheet.SheetView.FreezeRows(1);
        dictionarySheet.Columns().AdjustToContents();
        dictionarySheet.Column(3).Width = Math.Min(dictionarySheet.Column(3).Width, 80);
        dictionarySheet.Column(3).Style.Alignment.WrapText = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ResearchExportFile(
            FileName("xlsx"),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            stream.ToArray());
    }

    private async Task<ExportDataset> BuildDatasetAsync(CancellationToken cancellationToken)
    {
        var participants = await catalogService.GetParticipantsAsync(cancellationToken);
        var summaries = new List<ParticipantResearchSummary>(participants.Count);
        foreach (var participant in participants)
        {
            summaries.Add(await metricsService.GetParticipantSummaryAsync(participant.Id, cancellationToken));
        }

        var outcomeNames = summaries.SelectMany(summary => summary.Outcomes)
            .Select(outcome => outcome.MetricName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var categories = Enum.GetValues<UsageCategory>();
        var columns = BuildColumns(categories, outcomeNames);
        var rows = new List<object?[]>();

        foreach (var summary in summaries.OrderBy(item => item.ParticipantCode))
        {
            var assignments = summary.Assignments.Count == 0
                ? new AssignmentMetrics?[] { null }
                : summary.Assignments.Cast<AssignmentMetrics?>();
            foreach (var assignment in assignments)
            {
                var row = new List<object?>
                {
                    summary.ParticipantCode,
                    summary.Course.ToString(),
                    summary.ResearchGroup,
                    assignment?.TaskCode,
                    assignment?.TaskTitle,
                    assignment?.ReviewedPromptCount ?? 0
                };

                foreach (var category in categories)
                {
                    var metric = assignment?.Categories.FirstOrDefault(item => item.Category == category);
                    row.Add(metric?.Count ?? 0);
                    row.Add(metric?.Percentage ?? 0m);
                }

                row.Add(assignment?.HasSubmission ?? false);
                row.Add(assignment?.HasAssessment ?? false);
                row.Add(assignment?.CodingScore);

                foreach (var outcomeName in outcomeNames)
                {
                    var outcome = summary.Outcomes.FirstOrDefault(item =>
                        item.MetricName.Equals(outcomeName, StringComparison.OrdinalIgnoreCase));
                    row.Add(outcome?.Value);
                    row.Add(outcome?.ScaleMaximum);
                }

                rows.Add(row.ToArray());
            }
        }

        return new ExportDataset(columns, rows);
    }

    private static IReadOnlyList<ExportColumn> BuildColumns(
        IReadOnlyList<UsageCategory> categories,
        IReadOnlyList<string> outcomeNames)
    {
        var columns = new List<ExportColumn>
        {
            new("participant_id", "string", "Anonymous participant identifier."),
            new("course", "string", "Course code recorded for the participant."),
            new("research_group", "string or missing", "Optional research group; blank means missing."),
            new("task_id", "string or missing", "Coding task identifier; blank when the participant has no assignment."),
            new("task_title", "string or missing", "Coding task title."),
            new("reviewed_prompt_count", "integer", "Valid interactions with Reviewed status for this assignment.")
        };

        foreach (var category in categories)
        {
            var name = ToSnakeCase(category.ToString());
            columns.Add(new($"{name}_count", "integer", $"Reviewed valid interactions classified as {category}."));
            columns.Add(new($"{name}_percentage", "decimal", $"Percentage of reviewed valid interactions classified as {category}."));
        }

        columns.Add(new("has_submission", "boolean", "True when at least one source submission exists."));
        columns.Add(new("has_assessment", "boolean", "True when a rubric assessment exists."));
        columns.Add(new("coding_score", "decimal or missing", "Rubric score from 0 to 10; blank means not assessed and 0 is a real score."));

        foreach (var outcomeName in outcomeNames)
        {
            var name = ToSnakeCase(outcomeName);
            columns.Add(new($"outcome_{name}", "decimal or missing", $"Research outcome '{outcomeName}'; blank means missing and 0 is a real value."));
            columns.Add(new($"outcome_{name}_scale_maximum", "decimal or missing", $"Optional scale maximum for '{outcomeName}'."));
        }

        return columns;
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null: return;
            case int number: cell.Value = number; break;
            case decimal number: cell.Value = number; break;
            case bool flag: cell.Value = flag; break;
            default: cell.Value = value.ToString() ?? string.Empty; break;
        }
    }

    private static string FormatCsvValue(object? value) => value switch
    {
        null => string.Empty,
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0) return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string ToSnakeCase(string value)
    {
        var separated = Regex.Replace(value.Trim(), "([a-z0-9])([A-Z])", "$1_$2");
        var normalized = Regex.Replace(separated, "[^A-Za-z0-9]+", "_").Trim('_').ToLowerInvariant();
        return normalized.Length == 0 ? "metric" : normalized;
    }

    private static string FileName(string extension) => $"research-dataset-{DateTimeOffset.UtcNow:yyyyMMdd}.{extension}";
}
