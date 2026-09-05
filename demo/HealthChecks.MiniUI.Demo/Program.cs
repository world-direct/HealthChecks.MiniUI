using HealthChecks.MiniUI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("Application is running."),
        tags: ["liveness", "readiness", "app"])

    .AddCheck(
        "db",
        () => HealthCheckResult.Healthy("Database is OK."),
        tags: ["readiness", "db", "dependencies"])

    .AddCheck(
        "failing",
        () => HealthCheckResult.Unhealthy("This check is failing."),
        tags: ["readiness", "demo", "error"])

    .AddAsyncCheck("slow", async () =>
    {
        await Task.Delay(1000);
        return HealthCheckResult.Healthy("This check is slow.");
    }, tags: ["performance", "readiness", "demo"])

    .AddAsyncCheck("timed-out", async cancellationToken =>
    {
        await Task.Delay(1000, cancellationToken);
        return HealthCheckResult.Healthy("This check should time out before completion.");
    }, timeout: TimeSpan.FromMilliseconds(200), tags: ["performance", "readiness", "timeout", "demo"])

    .AddCheck(
        "warning",
        () => HealthCheckResult.Degraded(
            "This check is warning.",
            data: new Dictionary<string, object>
            {
                ["reason"] = "This is a warning.",
                ["code"] = 1234
            }),
        tags: ["readiness", "degraded", "demo"])

    .AddCheck("will-throw", () =>
    {
        throw new InvalidOperationException("Invalid operation.");
    }, tags: ["readiness", "demo", "error"]);


var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health")
    {
        var accept = context.Request.Headers.Accept.ToString();
        var wantsHtml = accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);

        // Internal server-side rewrite for this request (no 3xx redirect to the client).
        context.Request.Path = wantsHtml ? "/health-ui" : "/healthz";
    }

    await next();
});

app.UseRouting();

app.MapGet("/", () => Results.Redirect("/health"));

app.MapHealthChecks("/healthz");

app.MapSimpleHealthPage("/health-ui", options =>
{
    options.Title = "HealthChecks MiniUI Demo";
    options.EnablePlainText = true;
    options.StatusCodes.Unhealthy = StatusCodes.Status503ServiceUnavailable;
});

app.Run();
