using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.MiniUI;

public static class SimpleHealthPageEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapSimpleHealthPage(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Action<SimpleHealthPageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var options = new SimpleHealthPageOptions();
        configure?.Invoke(options);

        var endpoint = endpoints.MapGet(pattern, async context =>
        {
            var healthCheckService = context.RequestServices.GetRequiredService<HealthCheckService>();
            var report = await healthCheckService.CheckHealthAsync(context.RequestAborted);

            var renderer = new SimpleHealthPageHtmlRenderer
            {
                Title = options.Title,
                Report = report,
                GeneratedAtUtc = DateTimeOffset.UtcNow
            };

            var html = renderer.Render();

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html, context.RequestAborted);
        });

        if (!string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
        {
            endpoint.RequireAuthorization(options.AuthorizationPolicy);
        }

        return endpoint;
    }
}
