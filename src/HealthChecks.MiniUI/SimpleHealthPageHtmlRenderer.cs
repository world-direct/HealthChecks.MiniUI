using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.MiniUI;

internal sealed class SimpleHealthPageHtmlRenderer
{
    private readonly HtmlEncoder _encoder = HtmlEncoder.Default;

    public string Title { get; set; } = "Health Checks";

    public HealthReport? Report { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public string Render()
    {
        var report = Report ?? throw new InvalidOperationException("Report must be set before rendering.");

        var safeTitle = _encoder.Encode(Title);
        var overallStatus = report.Status.ToString();
        var overallClass = GetStatusClass(report.Status);

        var builder = new StringBuilder();
        var overlays = new StringBuilder();
        var overlayIndex = 0;

        builder.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
            .Append("<title>")
            .Append(safeTitle)
            .Append("</title><style>")
            .Append("body{font-family:Segoe UI,Arial,sans-serif;margin:1.5rem;background:#f6f8fa;color:#1f2328}")
            .Append("main{max-width:960px;margin:0 auto;background:#fff;border:1px solid #d1d9e0;border-radius:8px;padding:1rem 1.25rem}")
            .Append("h1{margin:.2rem 0 1rem;font-size:1.5rem}")
            .Append(".status{font-weight:700;padding:.25rem .6rem;border-radius:999px;display:inline-block}")
            .Append(".healthy{background:#dafbe1;color:#055d20}")
            .Append(".degraded{background:#fff8c5;color:#8a4600}")
            .Append(".unhealthy{background:#ffebe9;color:#cf222e}")
            .Append("table{width:100%;border-collapse:collapse;margin-top:1rem}")
            .Append("th,td{border-top:1px solid #d1d9e0;padding:.55rem;text-align:left;vertical-align:top}")
            .Append("th{font-size:.85rem;color:#59636e;text-transform:uppercase}")
            .Append(".meta{color:#59636e;font-size:.9rem;margin:.5rem 0}")
            .Append("code{background:#f6f8fa;padding:.1rem .3rem;border-radius:4px}")
            .Append(".exception{display:block;white-space:pre-wrap;word-break:break-word}")
            .Append(".exception-link{color:#0969da;text-decoration:none;font-weight:600}")
            .Append(".exception-link:hover{text-decoration:underline}")
            .Append(".tags{display:flex;flex-wrap:wrap;gap:.3rem}")
            .Append(".tag{display:inline-block;padding:.15rem .45rem;border-radius:999px;background:#ddf4ff;color:#0969da;font-size:.8rem;font-weight:600;line-height:1.2}")
            .Append(".overlay{display:none;position:fixed;inset:0;background:rgba(31,35,40,.55);z-index:1000;padding:1rem;align-items:center;justify-content:center}")
            .Append(".overlay:target{display:flex}")
            .Append(".overlay-card{max-width:900px;width:100%;max-height:90vh;overflow:auto;background:#fff;border:1px solid #d1d9e0;border-radius:8px;padding:1rem}")
            .Append(".overlay-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:.75rem}")
            .Append(".overlay-close{color:#0969da;text-decoration:none;font-weight:600}")
            .Append(".overlay-close:hover{text-decoration:underline}")
            .Append(".exception-full{display:block;padding:.6rem;line-height:1.4}")
            .Append("</style></head><body><main><h1>")
            .Append(safeTitle)
            .Append("</h1><p><span class=\"status ")
            .Append(overallClass)
            .Append("\">")
            .Append(_encoder.Encode(overallStatus))
            .Append("</span></p><p class=\"meta\">Generated: ")
            .Append(_encoder.Encode(GeneratedAtUtc.ToString("u", CultureInfo.InvariantCulture)))
            .Append(" | Duration: ")
            .Append(_encoder.Encode(report.TotalDuration.TotalMilliseconds.ToString("0.##", CultureInfo.InvariantCulture)))
            .Append(" ms</p><table><thead><tr><th>Name</th><th>Status</th><th>Description</th><th>Duration</th><th>Tags</th><th>Exception</th></tr></thead><tbody>");

        foreach (var entry in report.Entries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var safeName = _encoder.Encode(entry.Key);
            var status = entry.Value.Status.ToString();
            var statusClass = GetStatusClass(entry.Value.Status);
            var description = string.IsNullOrWhiteSpace(entry.Value.Description)
                ? "-"
                : _encoder.Encode(entry.Value.Description);

            builder.Append("<tr><td><strong>")
                .Append(safeName)
                .Append("</strong></td><td><span class=\"status ")
                .Append(statusClass)
                .Append("\">")
                .Append(_encoder.Encode(status))
                .Append("</span></td><td>")
                .Append(description)
                .Append("</td><td>")
                .Append(_encoder.Encode(entry.Value.Duration.TotalMilliseconds.ToString("0.##", CultureInfo.InvariantCulture)))
                .Append(" ms</td><td>");

            if (!entry.Value.Tags.Any())
            {
                builder.Append("-");
            }
            else
            {
                var tags = entry.Value.Tags
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Select(x => $"<span class=\"tag\">{_encoder.Encode(x)}</span>");

                builder.Append("<div class=\"tags\">")
                    .Append(string.Join(string.Empty, tags))
                    .Append("</div>");
            }

            builder.Append("</td><td>");

            if (entry.Value.Exception is null)
            {
                builder.Append("-");
            }
            else
            {
                var overlayId = $"ex-overlay-{overlayIndex++}";
                var exceptionMessage = string.IsNullOrWhiteSpace(entry.Value.Exception.Message)
                    ? entry.Value.Exception.GetType().Name
                    : entry.Value.Exception.Message;

                builder.Append("<a class=\"exception-link\" href=\"#")
                    .Append(_encoder.Encode(overlayId))
                    .Append("\">")
                    .Append(_encoder.Encode(exceptionMessage))
                    .Append("</a>");

                overlays.Append("<div id=\"")
                    .Append(_encoder.Encode(overlayId))
                    .Append("\" class=\"overlay\"><div class=\"overlay-card\"><div class=\"overlay-head\"><strong>Exception details</strong><a class=\"overlay-close\" href=\"#\">Close</a></div><code class=\"exception exception-full\">")
                    .Append(_encoder.Encode(entry.Value.Exception.ToString()))
                    .Append("</code></div></div>");
            }

            builder.Append("</td></tr>");
        }

        builder.Append("</tbody></table></main>")
            .Append(overlays)
            .Append("</body></html>");
        return builder.ToString();
    }

    private static string GetStatusClass(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "healthy",
        HealthStatus.Degraded => "degraded",
        HealthStatus.Unhealthy => "unhealthy",
        _ => "unhealthy"
    };

}
