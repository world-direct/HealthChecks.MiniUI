using HealthChecks.MiniUI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.MiniUI.Tests;

public sealed class SimpleHealthPageTests
{
    [Fact]
    public void SimpleHealthPageOptions_HaveExpectedDefaults()
    {
        var options = new SimpleHealthPageOptions();

        Assert.Equal("Health Checks", options.Title);
        Assert.Null(options.AuthorizationPolicy);
        Assert.Equal(StatusCodes.Status200OK, options.StatusCodes.Healthy);
        Assert.Equal(StatusCodes.Status200OK, options.StatusCodes.Degraded);
        Assert.Equal(StatusCodes.Status200OK, options.StatusCodes.Unhealthy);
    }

    [Fact]
    public void StatusCodeResolution_UsesConfiguredPerStatusValues()
    {
        var statusCodes = new SimpleHealthPageStatusCodesOptions
        {
            Healthy = StatusCodes.Status200OK,
            Degraded = StatusCodes.Status429TooManyRequests,
            Unhealthy = StatusCodes.Status503ServiceUnavailable
        };

        Assert.Equal(StatusCodes.Status200OK,
            SimpleHealthPageEndpointRouteBuilderExtensions.ResolveStatusCode(HealthStatus.Healthy, statusCodes));

        Assert.Equal(StatusCodes.Status429TooManyRequests,
            SimpleHealthPageEndpointRouteBuilderExtensions.ResolveStatusCode(HealthStatus.Degraded, statusCodes));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable,
            SimpleHealthPageEndpointRouteBuilderExtensions.ResolveStatusCode(HealthStatus.Unhealthy, statusCodes));
    }

    [Fact]
    public void HtmlRenderer_EncodesPotentiallyUnsafeValues()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["db<script>"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "desc<script>",
                    TimeSpan.FromMilliseconds(12),
                    exception: new InvalidOperationException("boom<script>"),
                    data: new Dictionary<string, object>
                    {
                        ["reason"] = "x<y"
                    },
                    tags: new[] { "db", "ready<script>" })
            },
            TimeSpan.FromMilliseconds(12));

        var renderer = new SimpleHealthPageHtmlRenderer
        {
            Title = "title<script>",
            Report = report,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        var html = renderer.Render();

        Assert.DoesNotContain("title<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("db<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("desc<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ready<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("boom<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("title&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("db&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("desc&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.Contains(">Tags</th>", html, StringComparison.Ordinal);
        Assert.Contains(">Exception</th>", html, StringComparison.Ordinal);
        Assert.Contains("ready&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("boom&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("class=\"exception-link\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"overlay\"", html, StringComparison.Ordinal);
        Assert.Contains("Exception details", html, StringComparison.Ordinal);
        Assert.Contains("id=\"theme-select\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Data</th>", html, StringComparison.Ordinal);
    }
}
