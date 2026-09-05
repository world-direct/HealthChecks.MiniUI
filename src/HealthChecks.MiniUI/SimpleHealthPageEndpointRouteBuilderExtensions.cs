using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.MiniUI;

/// <summary>
/// Extension methods for mapping the simple health page endpoint.
/// </summary>
public static class SimpleHealthPageEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a human-readable health page endpoint at the given route pattern.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder used to map the endpoint.</param>
    /// <param name="pattern">The route pattern where the health page should be exposed, for example <c>/health</c>.</param>
    /// <param name="configure">Optional per-endpoint options configuration.</param>
    /// <returns>The endpoint convention builder for further endpoint configuration.</returns>
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
            var report = await healthCheckService.CheckHealthAsync(options.Predicate, context.RequestAborted);

            var generatedAtUtc = DateTimeOffset.UtcNow;
            var usePlainText = options.EnablePlainText && !AcceptsHtml(context.Request);

            string body;
            if (usePlainText)
            {
                body = new SimpleHealthPageTextRenderer
                {
                    Title = options.Title,
                    Report = report,
                    GeneratedAtUtc = generatedAtUtc
                }.Render();
            }
            else
            {
                body = new SimpleHealthPageHtmlRenderer
                {
                    Title = options.Title,
                    Report = report,
                    GeneratedAtUtc = generatedAtUtc
                }.Render();
            }

            context.Response.StatusCode = ResolveStatusCode(report.Status, options.StatusCodes);
            context.Response.ContentType = usePlainText ? "text/plain; charset=utf-8" : "text/html; charset=utf-8";

            if (options.EnablePlainText)
            {
                context.Response.Headers.Vary = "Accept";
            }

            await context.Response.WriteAsync(body, context.RequestAborted);
        });

        if (!string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
        {
            endpoint.RequireAuthorization(options.AuthorizationPolicy);
        }

        return endpoint;
    }

    private static bool AcceptsHtml(HttpRequest request)
    {
        foreach (var accept in request.Headers.Accept)
        {
            if (accept is null)
            {
                continue;
            }

            foreach (var range in accept.Split(','))
            {
                var mediaType = range.Split(';')[0].Trim();

                if (mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase) ||
                    mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static int ResolveStatusCode(HealthStatus status, SimpleHealthPageStatusCodesOptions statusCodes)
    {
        ArgumentNullException.ThrowIfNull(statusCodes);

        return status switch
        {
            HealthStatus.Healthy => statusCodes.Healthy,
            HealthStatus.Degraded => statusCodes.Degraded,
            HealthStatus.Unhealthy => statusCodes.Unhealthy,
            _ => statusCodes.Unhealthy
        };
    }
}
