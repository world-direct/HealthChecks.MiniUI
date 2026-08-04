# HealthChecks.MiniUI

HealthChecks.MiniUI is a tiny ASP.NET Core health page helper that renders a human-friendly HTML status page on top of Microsoft health checks.

## What it does

- Keeps your machine endpoint separate from the human UI
- Renders health checks as a simple HTML page
- Shows status, description, duration, tags, and exception details
- Uses the existing Microsoft health check pipeline

## Basic usage

Map the page in your app startup code:

```csharp
// Microsoft.Extensions.HealthChecks
app.MapHealthChecks("/healthz");

// MiniUI HealthCheck page
app.MapSimpleHealthPage("/health", options =>
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

- `/health` for the HTML UI
- `/healthz` for the machine endpoint

## Notes

- The UI is intentionally small and stateless.
- Route configuration is map-time, matching the usual ASP.NET Core endpoint style.
- The page is designed to stay simple and easy to extend later.
