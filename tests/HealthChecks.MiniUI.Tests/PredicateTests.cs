using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace HealthChecks.MiniUI.Tests;

public sealed class PredicateTests
{
    [Fact]
    public void Predicate_DefaultsToNull()
    {
        Assert.Null(new SimpleHealthPageOptions().Predicate);
    }

    [Fact]
    public async Task Predicate_Null_RunsAllHealthChecks()
    {
        using var host = await CreateHostAsync(configure: null);

        var html = await host.GetTestClient().GetStringAsync("/health");

        Assert.Contains("ready-check", html, StringComparison.Ordinal);
        Assert.Contains("live-check", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Predicate_FiltersByTag()
    {
        using var host = await CreateHostAsync(o => o.Predicate = r => r.Tags.Contains("ready"));

        var html = await host.GetTestClient().GetStringAsync("/health");

        Assert.Contains("ready-check", html, StringComparison.Ordinal);
        Assert.DoesNotContain("live-check", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Predicate_ExcludingAllChecks_ReportsHealthy()
    {
        using var host = await CreateHostAsync(o => o.Predicate = _ => false);

        var response = await host.GetTestClient().GetAsync("/health");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("ready-check", html, StringComparison.Ordinal);
        Assert.DoesNotContain("live-check", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Predicate_FilteringOutUnhealthyCheck_ChangesStatusCode()
    {
        using var host = await CreateHostAsync(o =>
        {
            o.Predicate = r => r.Tags.Contains("live");
            o.StatusCodes.Unhealthy = StatusCodes.Status503ServiceUnavailable;
        });

        var response = await host.GetTestClient().GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
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
