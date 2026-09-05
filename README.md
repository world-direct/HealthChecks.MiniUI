# HealthChecks.MiniUI

HealthChecks.MiniUI is a tiny ASP.NET Core health page helper that renders a human-friendly HTML status page on top of Microsoft health checks.

## What it does

- Renders health checks as a simple HTML page
- Serves humans and scripts from one endpoint, HTML for browsers and plain text for `curl`
- Shows status, description, duration, tags, and exception details
- Uses the existing Microsoft health check pipeline

## How it looks

These screenshots are taken from the demo application.

| Dark mode | Light mode | Exception pop-out |
| --- | --- | --- |
| ![Dark mode](assets/screen-dark.png) | ![Light mode](assets/screen-light.png) | ![Exception pop-out](assets/screen-ex.png) |

## Installation

Install the package from NuGet:

```bash
dotnet add package WorldDirect.HealthChecks.MiniUI
```

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

## Response status codes

By default, the page returns HTTP `200` for all overall health states (`Healthy`, `Degraded`, `Unhealthy`).

You can override this per status:

```csharp
using Microsoft.AspNetCore.Http;

app.MapSimpleHealthPage("/health", options =>
{
    options.StatusCodes.Healthy = StatusCodes.Status200OK;
    options.StatusCodes.Degraded = StatusCodes.Status200OK;
    options.StatusCodes.Unhealthy = StatusCodes.Status503ServiceUnavailable;
});
```

## Filtering which checks are executed

By default the page executes all registered health checks. Use `Predicate` to select a subset, for example by tag. Checks that are filtered out are not executed at all:

```csharp
app.MapSimpleHealthPage("/health", options =>
{
    options.Predicate = registration => registration.Tags.Contains("ui");
});
```

This is the same `Func<HealthCheckRegistration, bool>` shape as `HealthCheckOptions.Predicate` used by `MapHealthChecks`.

## Plain text output

Opt in with `EnablePlainText` to serve a `text/plain` rendering to clients that do not accept `text/html`, such as `curl` or a container health check. Browsers still get the HTML page.

```csharp
app.MapSimpleHealthPage("/health", options =>
{
    options.EnablePlainText = true;
});
```

```console
$ curl http://localhost:5000/health
Health Checks
Status:    Unhealthy
Duration:  1002.67 ms
Generated: 2026-09-05 09:12:33Z

| NAME    | STATUS    | DESCRIPTION          |   DURATION |
|---------|-----------|----------------------|------------|
| db      | Healthy   | connection ok        |     1.5 ms |
| failing | Unhealthy | queue backlog too... | 1002.67 ms |
```

Descriptions are shortened with `...` so the table stays readable in a terminal.

Combine it with `StatusCodes.Unhealthy` so `curl --fail` works.

## Demo app

The repository includes a small demo app in `demo/HealthChecks.MiniUI.Demo`.

Run it from the repo root:

```bash
dotnet run --project demo/HealthChecks.MiniUI.Demo/HealthChecks.MiniUI.Demo.csproj
```

Then open:

- `/health` for the MiniUI page (HTML in a browser, plain text for `curl`, `503` when unhealthy)
- `/healthz` for the plain `MapHealthChecks` response

## Notes

- The UI is intentionally small and stateless.
- Route configuration is map-time, matching the usual ASP.NET Core endpoint style.
- The page is designed to stay simple and easy to extend later.
