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
            .Append(":root{color-scheme:light dark;--bg:#f6f8fa;--text:#1f2328;--surface:#fff;--border:#d1d9e0;--muted:#59636e;--code-bg:#f6f8fa;--link:#0969da;--overlay-bg:rgba(31,35,40,.55);--healthy-bg:#dafbe1;--healthy-fg:#055d20;--degraded-bg:#fff8c5;--degraded-fg:#8a4600;--unhealthy-bg:#ffebe9;--unhealthy-fg:#cf222e;--tag-bg:#ddf4ff;--tag-fg:#0969da}")
            .Append("@media (prefers-color-scheme: dark){:root{--bg:#0d1117;--text:#e6edf3;--surface:#161b22;--border:#30363d;--muted:#9da7b3;--code-bg:#0d1117;--link:#58a6ff;--overlay-bg:rgba(1,4,9,.75);--healthy-bg:#12321e;--healthy-fg:#7ee787;--degraded-bg:#3d2f00;--degraded-fg:#f2cc60;--unhealthy-bg:#3b1717;--unhealthy-fg:#ff7b72;--tag-bg:#0b3255;--tag-fg:#79c0ff}}")
            .Append("html[data-theme='light']{color-scheme:light;--bg:#f6f8fa;--text:#1f2328;--surface:#fff;--border:#d1d9e0;--muted:#59636e;--code-bg:#f6f8fa;--link:#0969da;--overlay-bg:rgba(31,35,40,.55);--healthy-bg:#dafbe1;--healthy-fg:#055d20;--degraded-bg:#fff8c5;--degraded-fg:#8a4600;--unhealthy-bg:#ffebe9;--unhealthy-fg:#cf222e;--tag-bg:#ddf4ff;--tag-fg:#0969da}")
            .Append("html[data-theme='dark']{color-scheme:dark;--bg:#0d1117;--text:#e6edf3;--surface:#161b22;--border:#30363d;--muted:#9da7b3;--code-bg:#0d1117;--link:#58a6ff;--overlay-bg:rgba(1,4,9,.75);--healthy-bg:#12321e;--healthy-fg:#7ee787;--degraded-bg:#3d2f00;--degraded-fg:#f2cc60;--unhealthy-bg:#3b1717;--unhealthy-fg:#ff7b72;--tag-bg:#0b3255;--tag-fg:#79c0ff}")
            .Append("body{font-family:Segoe UI,Arial,sans-serif;margin:1.5rem;background:var(--bg);color:var(--text)}")
            .Append("main{max-width:960px;margin:0 auto;background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:1rem 1.25rem}")
            .Append("h1{margin:.2rem 0 1rem;font-size:1.5rem}")
            .Append(".toolbar{display:flex;justify-content:flex-end;align-items:center;gap:.45rem;margin-bottom:.6rem}")
            .Append(".toolbar label{font-size:.85rem;color:var(--muted)}")
            .Append(".toolbar select{padding:.2rem .35rem;border:1px solid var(--border);border-radius:6px;background:var(--surface);color:var(--text);font-size:.85rem}")
            .Append(".status{font-weight:700;padding:.25rem .6rem;border-radius:999px;display:inline-block}")
            .Append(".healthy{background:var(--healthy-bg);color:var(--healthy-fg)}")
            .Append(".degraded{background:var(--degraded-bg);color:var(--degraded-fg)}")
            .Append(".unhealthy{background:var(--unhealthy-bg);color:var(--unhealthy-fg)}")
            .Append("table{width:100%;border-collapse:collapse;margin-top:1rem}")
            .Append("th,td{border-top:1px solid var(--border);padding:.55rem;text-align:left;vertical-align:top}")
            .Append("th{font-size:.85rem;color:var(--muted);text-transform:uppercase}")
            .Append(".meta{color:var(--muted);font-size:.9rem;margin:.5rem 0}")
            .Append("code{background:var(--code-bg);padding:.1rem .3rem;border-radius:4px}")
            .Append(".exception{display:block;white-space:pre-wrap;word-break:break-word}")
            .Append(".exception-link{color:var(--link);text-decoration:underline;display:inline-block;max-width:16rem;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;vertical-align:bottom}")
            .Append(".exception-link:hover{text-decoration:underline}")
            .Append(".tags{display:flex;flex-wrap:wrap;gap:.3rem}")
            .Append(".tag{display:inline-block;padding:.15rem .45rem;border-radius:999px;background:var(--tag-bg);color:var(--tag-fg);font-size:.8rem;font-weight:600;line-height:1.2}")
            .Append(".overlay{display:none;position:fixed;inset:0;background:var(--overlay-bg);z-index:1000;padding:1rem;align-items:center;justify-content:center}")
            .Append(".overlay:target{display:flex}")
            .Append(".overlay-card{max-width:900px;width:100%;max-height:90vh;overflow:auto;background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:1rem}")
            .Append(".overlay-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:.75rem}")
            .Append(".overlay-close{color:var(--link);text-decoration:underline}")
            .Append(".overlay-close:hover{text-decoration:underline}")
            .Append(".exception-full{display:block;padding:.6rem;line-height:1.4}")
            .Append("</style></head><body><main><h1>")
            .Append("<div class=\"toolbar\"><label for=\"theme-select\">Theme</label><select id=\"theme-select\" aria-label=\"Theme\"><option value=\"system\">System</option><option value=\"light\">Light</option><option value=\"dark\">Dark</option></select></div>")
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
                var exceptionTypeName = entry.Value.Exception.GetType().Name;

                builder.Append("<a class=\"exception-link\" href=\"#")
                    .Append(_encoder.Encode(overlayId))
                    .Append("\">")
                    .Append(_encoder.Encode(exceptionTypeName))
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
            .Append("<script>(function(){var storageKey='healthchecks-miniui-theme';var select=document.getElementById('theme-select');var prefersDark=window.matchMedia&&window.matchMedia('(prefers-color-scheme: dark)');function normalizeTheme(value){return value==='light'||value==='dark'?value:'system';}function applyTheme(value){var resolved=normalizeTheme(value);if(resolved==='system'){document.documentElement.removeAttribute('data-theme');}else{document.documentElement.setAttribute('data-theme',resolved);}}function closeOverlay(){if(location.hash&&location.hash.indexOf('#ex-overlay-')===0){var sx=window.scrollX;var sy=window.scrollY;location.hash='';window.scrollTo(sx,sy);if(history.replaceState){history.replaceState(null,'',location.pathname+location.search);}}}var storedTheme='system';try{storedTheme=normalizeTheme(localStorage.getItem(storageKey));}catch(e){storedTheme='system';}if(select){select.value=storedTheme;select.addEventListener('change',function(){var nextTheme=normalizeTheme(select.value);applyTheme(nextTheme);try{localStorage.setItem(storageKey,nextTheme);}catch(e){}});}applyTheme(storedTheme);if(prefersDark&&typeof prefersDark.addEventListener==='function'){prefersDark.addEventListener('change',function(){var activeTheme='system';try{activeTheme=normalizeTheme(localStorage.getItem(storageKey));}catch(e){activeTheme='system';}if(activeTheme==='system'){applyTheme('system');}});}document.addEventListener('keydown',function(e){if(e.key==='Escape'){closeOverlay();}});document.addEventListener('click',function(e){if(e.target&&e.target.classList&&e.target.classList.contains('overlay')){closeOverlay();}});})();</script>")
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
