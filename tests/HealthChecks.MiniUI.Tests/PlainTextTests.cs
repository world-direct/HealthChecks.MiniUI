using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace HealthChecks.MiniUI.Tests;

public sealed class PlainTextTests
{
    [Fact]
    public void EnablePlainText_DefaultsToFalse()
    {
        Assert.False(new SimpleHealthPageOptions().EnablePlainText);
    }

    [Fact]
    public async Task Disabled_AlwaysReturnsHtml()
    {
        using var host = await CreateHostAsync(configure: null);

        var response = await SendAsync(host, accept: "*/*");

        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task Enabled_ReturnsTextWhenHtmlIsNotAccepted()
    {
        using var host = await CreateHostAsync(o => o.EnablePlainText = true);

        var response = await SendAsync(host, accept: "*/*");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Contains("Status:    Unhealthy", body, StringComparison.Ordinal);
        Assert.Contains("| NAME", body, StringComparison.Ordinal);
        Assert.Contains("live-check", body, StringComparison.Ordinal);
        Assert.Contains("ready-check", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Enabled_ReturnsTextWhenAcceptHeaderIsMissing()
    {
        using var host = await CreateHostAsync(o => o.EnablePlainText = true);

        var response = await SendAsync(host, accept: null);

        Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
    public async Task Enabled_ReturnsHtmlWhenHtmlIsAccepted(string accept)
    {
        using var host = await CreateHostAsync(o => o.EnablePlainText = true);

        var response = await SendAsync(host, accept);

        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task Enabled_SetsVaryAcceptHeader()
    {
        using var host = await CreateHostAsync(o => o.EnablePlainText = true);

        var response = await SendAsync(host, accept: "*/*");

        Assert.Equal("Accept", Assert.Single(response.Headers.Vary));
    }

    [Fact]
    public async Task PlainText_UsesConfiguredStatusCode()
    {
        using var host = await CreateHostAsync(o =>
        {
            o.EnablePlainText = true;
            o.StatusCodes.Unhealthy = StatusCodes.Status503ServiceUnavailable;
        });

        var response = await SendAsync(host, accept: "*/*");

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public void TextRenderer_UsesInvariantNumberFormatAndAlignsColumns()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["db"] = new HealthReportEntry(HealthStatus.Healthy, "ok", TimeSpan.FromMilliseconds(1.5), null, null),
                ["failing"] = new HealthReportEntry(HealthStatus.Unhealthy, "bad", TimeSpan.FromMilliseconds(1002.67), null, null)
            },
            TimeSpan.FromMilliseconds(1002.67));

        var text = new SimpleHealthPageTextRenderer
        {
            Title = "My App Health",
            Report = report,
            GeneratedAtUtc = DateTimeOffset.UnixEpoch
        }.Render();

        var lines = text.Split('\n');

        Assert.Equal("My App Health", lines[0]);
        Assert.Equal("Status:    Unhealthy", lines[1]);
        Assert.Equal("Duration:  1002.67 ms", lines[2]);
        Assert.Equal("Generated: 1970-01-01 00:00:00Z", lines[3]);

        var header = Array.FindIndex(lines, l => l.StartsWith("| NAME", StringComparison.Ordinal));
        Assert.True(header > 0);
        Assert.Equal(lines[header].Length, lines[header + 1].Length);
        Assert.Equal(lines[header].Length, lines[header + 2].Length);
        Assert.Equal(lines[header].Length, lines[header + 3].Length);
        Assert.Equal("|---------|-----------|------------|", lines[header + 1]);
        Assert.Equal("| db      | Healthy   |     1.5 ms |", lines[header + 2]);
        Assert.Equal("| failing | Unhealthy | 1002.67 ms |", lines[header + 3]);
    }

    [Fact]
    public void TextRenderer_HandlesEmptyReport()
    {
        var text = new SimpleHealthPageTextRenderer
        {
            Report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero),
            GeneratedAtUtc = DateTimeOffset.UnixEpoch
        }.Render();

        Assert.Contains("Status:    Healthy", text, StringComparison.Ordinal);
        Assert.DoesNotContain("NAME", text, StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> SendAsync(IHost host, string? accept)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");

        if (accept is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept", accept);
        }

        return await host.GetTestClient().SendAsync(request);
    }

    private static Task<IHost> CreateHostAsync(Action<SimpleHealthPageOptions>? configure)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHealthChecks()
                        .AddCheck("ready-check", () => HealthCheckResult.Unhealthy("not ready"), tags: ["ready"])
                        .AddCheck("live-check", () => HealthCheckResult.Healthy("alive"), tags: ["live"]);
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapSimpleHealthPage("/health", configure));
                });
            })
            .StartAsync();
    }
}
