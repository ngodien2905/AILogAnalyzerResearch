using System.Text;
using AILogAnalyzerResearch.Data;
using AILogAnalyzerResearch.Services;
using ClosedXML.Excel;

namespace AILogAnalyzerResearch.Tests;

public sealed class ResearchExportServiceTests
{
    [Fact]
    public async Task Csv_and_xlsx_keep_zero_values_distinct_from_missing_values()
    {
        var factory = new TestDbContextFactory();
        var catalog = new ResearchCatalogService(factory);
        var interactionService = new ResearchInteractionService(factory, new LogImportService());
        var submissionService = new ResearchSubmissionService(factory, new MemoryFileStorageService());
        var participant = await catalog.CreateParticipantAsync(new("P001", CourseCode.PRO, "Group A"));
        var assessedTask = await catalog.CreateCodingTaskAsync(new("T01", "Assessed at zero", null));
        var missingTask = await catalog.CreateCodingTaskAsync(new("T02", "Missing assessment", null));
        var assessedAssignment = await catalog.AssignAsync(participant.Id, assessedTask.Id);
        await catalog.AssignAsync(participant.Id, missingTask.Id);

        var interaction = await interactionService.AddManualAsync(new(
            assessedAssignment.Id,
            "ChatGPT",
            AiPlan.Free,
            "Explain this code",
            "Explanation",
            PreliminaryCategory: UsageCategory.CodeExplanation));
        await interactionService.ReviewAsync(interaction.Id, new(
            UsageCategory.CodeExplanation,
            ReviewStatus.Reviewed,
            true));

        var submission = await submissionService.SubmitAsync(
            assessedAssignment.Id,
            new MemoryStream(Encoding.UTF8.GetBytes("class Solution {}")),
            "solution.cs",
            "text/plain",
            true);
        await submissionService.SaveAssessmentAsync(
            submission.Id,
            new CreateAssessmentRequest(0, 0, 0, 0, 0));
        await catalog.UpsertOutcomeAsync(participant.Id, "Pre-test", 0, 100);

        var service = new ResearchExportService(catalog, new ResearchMetricsService(factory));
        var csv = await service.CreateCsvAsync();
        var csvText = Encoding.UTF8.GetString(csv.Content).TrimStart('\uFEFF');
        var csvRows = csvText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var headers = csvRows[0].Split(',');
        var taskColumn = Array.IndexOf(headers, "task_id");
        var scoreColumn = Array.IndexOf(headers, "coding_score");
        var outcomeColumn = Array.IndexOf(headers, "outcome_pre_test");
        var assessedCsvRow = csvRows.Single(row => row.Split(',')[taskColumn] == "T01").Split(',');
        var missingCsvRow = csvRows.Single(row => row.Split(',')[taskColumn] == "T02").Split(',');

        Assert.Equal("0", assessedCsvRow[scoreColumn]);
        Assert.Equal("", missingCsvRow[scoreColumn]);
        Assert.Equal("0", assessedCsvRow[outcomeColumn]);
        Assert.Equal("0", missingCsvRow[outcomeColumn]);

        var xlsx = await service.CreateXlsxAsync();
        using var workbook = new XLWorkbook(new MemoryStream(xlsx.Content));
        var sheet = workbook.Worksheet("Research Dataset");
        var columns = sheet.Row(1).CellsUsed()
            .ToDictionary(cell => cell.GetString(), cell => cell.Address.ColumnNumber);
        var assessedXlsxRow = sheet.RowsUsed().Skip(1)
            .Single(row => row.Cell(columns["task_id"]).GetString() == "T01");
        var missingXlsxRow = sheet.RowsUsed().Skip(1)
            .Single(row => row.Cell(columns["task_id"]).GetString() == "T02");

        Assert.Equal(0m, assessedXlsxRow.Cell(columns["coding_score"]).GetValue<decimal>());
        Assert.True(missingXlsxRow.Cell(columns["coding_score"]).IsEmpty());
        Assert.Equal(0m, assessedXlsxRow.Cell(columns["outcome_pre_test"]).GetValue<decimal>());
        Assert.True(workbook.TryGetWorksheet("Data Dictionary", out _));
    }
}
