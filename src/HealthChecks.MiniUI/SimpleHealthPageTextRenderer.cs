using System.Globalization;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.MiniUI;

internal sealed class SimpleHealthPageTextRenderer
{
    private static readonly string[] Headers = ["NAME", "STATUS", "DURATION"];

    public string Title { get; set; } = "Health Checks";

    public HealthReport? Report { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public string Render()
    {
        var report = Report ?? throw new InvalidOperationException("Report must be set before rendering.");

        var builder = new StringBuilder();
        builder.Append(Title).Append('\n');

        AppendField(builder, "Status", report.Status.ToString());
        AppendField(builder, "Duration", FormatDuration(report.TotalDuration));
        AppendField(builder, "Generated", GeneratedAtUtc.ToString("u", CultureInfo.InvariantCulture));

        if (report.Entries.Count == 0)
        {
            return builder.ToString();
        }

        var rows = report.Entries
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new[] { x.Key, x.Value.Status.ToString(), FormatDuration(x.Value.Duration) })
            .ToList();

        var widths = Headers
            .Select((header, i) => Math.Max(header.Length, rows.Max(r => r[i].Length)))
            .ToArray();

        builder.Append('\n');
        AppendRow(builder, Headers, widths);
        AppendRow(builder, widths.Select(w => new string('-', w)).ToArray(), widths, pad: '-');

        foreach (var row in rows)
        {
            AppendRow(builder, row, widths);
        }

        return builder.ToString();
    }

    private static void AppendField(StringBuilder builder, string label, string value)
    {
        const int LabelWidth = 11; // "Generated:" plus a separating space
        builder.Append((label + ':').PadRight(LabelWidth)).Append(value).Append('\n');
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> cells, IReadOnlyList<int> widths, char pad = ' ')
    {
        builder.Append('|');

        for (var i = 0; i < cells.Count; i++)
        {
            // The duration column is right-aligned so the numbers line up.
            var cell = i == cells.Count - 1 ? cells[i].PadLeft(widths[i], pad) : cells[i].PadRight(widths[i], pad);
            builder.Append(pad).Append(cell).Append(pad).Append('|');
        }

        builder.Append('\n');
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalMilliseconds.ToString("0.##", CultureInfo.InvariantCulture) + " ms";
}
