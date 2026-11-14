using System.Globalization;
using System.Text;
using System.Text.Json;
using AILogAnalyzerResearch.Data;

namespace AILogAnalyzerResearch.Services;

public sealed record ImportedInteraction(
    string? ParticipantCode,
    string? TaskCode,
    string Platform,
    AiPlan Plan,
    string Prompt,
    string Response,
    DateTimeOffset OccurredAt,
    UsageCategory Category);

public sealed class LogImportService
{
    public async Task<IReadOnlyList<ImportedInteraction>> ParseAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        return Path.GetExtension(fileName).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? ParseJson(text)
            : ParseCsv(text);
    }

    private static IReadOnlyList<ImportedInteraction> ParseJson(string text)
    {
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        var items = root.ValueKind == JsonValueKind.Array
            ? root.EnumerateArray()
            : Find(root, "logs", "interactions", "data") is { ValueKind: JsonValueKind.Array } array
                ? array.EnumerateArray()
                : throw new InvalidDataException("JSON must be an array or contain logs/interactions/data array.");

        return items.Select(ReadJsonItem).ToList();
    }

    private static ImportedInteraction ReadJsonItem(JsonElement item)
    {
        var timestamp = Value(item, "timestamp", "occurredAt", "time", "createdAt");
        return new ImportedInteraction(
            Value(item, "participantId", "participantCode", "participant"),
            Value(item, "taskId", "taskCode", "task"),
            Value(item, "platform", "aiTool", "tool") ?? "Unknown",
            ParseEnum(Value(item, "plan", "aiPlan"), AiPlan.Unknown),
            Value(item, "prompt", "question", "userMessage") ?? string.Empty,
            Value(item, "response", "answer", "assistantMessage") ?? string.Empty,
            DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var occurredAt)
                ? occurredAt : DateTimeOffset.UtcNow,
            ParseEnum(Value(item, "category", "usageCategory"), UsageCategory.Unclassified));
    }

    private static IReadOnlyList<ImportedInteraction> ParseCsv(string text)
    {
        var rows = ParseCsvRows(text).ToList();
        if (rows.Count < 2)
        {
            return [];
        }

        var headers = rows[0].Select(Normalize).ToList();
        string? Get(IReadOnlyList<string> row, params string[] aliases)
        {
            var index = headers.FindIndex(header => aliases.Select(Normalize).Contains(header));
            return index >= 0 && index < row.Count ? row[index] : null;
        }

        return rows.Skip(1).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))).Select(row =>
        {
            var timestamp = Get(row, "timestamp", "occurredAt", "time", "createdAt");
            return new ImportedInteraction(
                Get(row, "participantId", "participantCode", "participant"),
                Get(row, "taskId", "taskCode", "task"),
                Get(row, "platform", "aiTool", "tool") ?? "Unknown",
                ParseEnum(Get(row, "plan", "aiPlan"), AiPlan.Unknown),
                Get(row, "prompt", "question", "userMessage") ?? string.Empty,
                Get(row, "response", "answer", "assistantMessage") ?? string.Empty,
                DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var occurredAt)
                    ? occurredAt : DateTimeOffset.UtcNow,
                ParseEnum(Get(row, "category", "usageCategory"), UsageCategory.Unclassified));
        }).ToList();
    }

    private static IEnumerable<IReadOnlyList<string>> ParseCsvRows(string text)
    {
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];
            if (character == '"')
            {
                if (quoted && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if ((character == '\r' || character == '\n') && !quoted)
            {
                if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(field.ToString());
                field.Clear();
                yield return row.ToArray();
                row.Clear();
            }
            else
            {
                field.Append(character);
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            yield return row;
        }
    }

    private static JsonElement? Find(JsonElement element, params string[] names)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (names.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) return property.Value;
        }

        return null;
    }

    private static string? Value(JsonElement element, params string[] names)
    {
        var value = Find(element, names);
        return value?.ValueKind switch
        {
            JsonValueKind.String => value.Value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.Value.ToString(),
            _ => null
        };
    }

    private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var normalized = Normalize(value);
        return Enum.GetValues<T>().FirstOrDefault(item => Normalize(item.ToString()) == normalized, fallback);
    }

    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
