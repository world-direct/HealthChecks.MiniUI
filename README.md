# HealthChecks.MiniUI

HealthChecks.MiniUI is a tiny ASP.NET Core health page helper that renders a human-friendly HTML status page on top of Microsoft health checks.

## What it does

- Keeps your machine endpoint separate from the human UI
- Renders health checks as a simple HTML page
- Shows status, description, duration, tags, and exception details
- Uses the existing Microsoft health check pipeline

## Installation

Install the package from NuGet (currently not working due policy):

```bash
dotnet add package WorldDirect.HealthChecks.MiniUI
```

If you want to consume the package from GitHub Packages instead, register the feed first:

GitHub Packages may still require authentication for NuGet client access, even when the package is public.

```bash
dotnet nuget add source https://nuget.pkg.github.com/world-direct/index.json \
    --name github-world-direct \
    --username YOUR_GITHUB_USERNAME \
    --password YOUR_GITHUB_TOKEN \
    --store-password-in-clear-text
```

Then install the package from that source:

```bash
dotnet add package WorldDirect.HealthChecks.MiniUI --source github-world-direct
```

## Basic usage

Map the page in your app startup code:

```csharp
// Microsoft.Extensions.HealthChecks
app.MapHealthChecks("/healthz");

// MiniUI HealthCheck page
app.MapSimpleHealthPage("/health-ui", options =>
{
    options.Title = "My App Health";
});
```

## Example for dynamic response

If you want one entry URL, you can dispatch based on the Accept header:
Browsers typically include `text/html` on page navigation, so interactive users will see the UI automatically.

- `/health` with `Accept: text/html` -> MiniUI endpoint (`/health-ui`)
- `/health` with other Accept values -> machine endpoint (`/healthz`)

```csharp
// Custom Middleware for Accept header based routing
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

// Required in this setup so endpoint routing sees the rewritten /health path.
app.UseRouting();

app.MapHealthChecks("/healthz");

app.MapSimpleHealthPage("/health-ui", options =>
{
    options.Title = "My App Health";
});
```

## Demo app

The repository includes a small demo app in `demo/HealthChecks.MiniUI.Demo`.

Run it from the repo root:

```bash
dotnet run --project demo/HealthChecks.MiniUI.Demo/HealthChecks.MiniUI.Demo.csproj
```

Then open:

- `/health` as the dynamic entry URL (UI for browser requests, machine response for non-HTML Accept headers)
- `/health-ui` for the UI endpoint directly
- `/healthz` for the machine endpoint directly

## Notes

- The UI is intentionally small and stateless.
- Route configuration is map-time, matching the usual ASP.NET Core endpoint style.
- The page is designed to stay simple and easy to extend later.
