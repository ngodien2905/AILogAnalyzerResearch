using System.Text;
using AILogAnalyzerResearch.Data;
using AILogAnalyzerResearch.Services;

namespace AILogAnalyzerResearch.Tests;

public sealed class LogImportServiceTests
{
    [Fact]
    public async Task Parses_json_csv_and_text_exports()
    {
        var service = new LogImportService();
        var json = """
            {"logs":[{"participantId":"P001","taskId":"T01","platform":"ChatGPT","prompt":"Help","response":"Answer","category":"Debugging"}]}
            """;
        var csv = "participantId,taskId,platform,prompt,response\r\nP002,T02,Claude,\"Explain, please\",Detailed answer";
        var text = """
            Participant: P003
            Task: T03
            Platform: Gemini
            Category: Testing
            Prompt:
            Suggest tests for this method.
            Response:
            Cover the happy path and boundary values.
            """;

        var jsonRows = await service.ParseAsync(Stream(json), "logs.json");
        var csvRows = await service.ParseAsync(Stream(csv), "logs.csv");
        var textRows = await service.ParseAsync(Stream(text), "logs.txt");

        Assert.Equal(UsageCategory.Debugging, Assert.Single(jsonRows).Category);
        Assert.Equal("Explain, please", Assert.Single(csvRows).Prompt);
        Assert.Equal(UsageCategory.Testing, Assert.Single(textRows).Category);
    }

    [Fact]
    public async Task Unsupported_format_is_rejected()
    {
        var service = new LogImportService();
        await Assert.ThrowsAsync<InvalidDataException>(() => service.ParseAsync(Stream("data"), "logs.xml"));
    }

    private static MemoryStream Stream(string value) => new(Encoding.UTF8.GetBytes(value));
}
