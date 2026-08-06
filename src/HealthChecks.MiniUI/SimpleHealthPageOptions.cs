namespace HealthChecks.MiniUI;

/// <summary>
/// Options for rendering and securing the simple health page endpoint.
/// </summary>
public sealed class SimpleHealthPageOptions
{
    /// <summary>
    /// Gets or sets the page title displayed at the top of the health page.
    /// </summary>
    public string Title { get; set; } = "Health Checks";

    /// <summary>
    /// Gets or sets an optional authorization policy name to apply to the mapped endpoint.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets the HTTP status code mapping used for the page response based on overall health status.
    /// </summary>
    public SimpleHealthPageStatusCodesOptions StatusCodes { get; } = new();
}

/// <summary>
/// HTTP status code mapping for each <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus"/> value.
/// </summary>
public sealed class SimpleHealthPageStatusCodesOptions
{
    /// <summary>
    /// Gets or sets the response status code to use when the overall report status is healthy.
    /// </summary>
    public int Healthy { get; set; } = Microsoft.AspNetCore.Http.StatusCodes.Status200OK;

    /// <summary>
    /// Gets or sets the response status code to use when the overall report status is degraded.
    /// </summary>
    public int Degraded { get; set; } = Microsoft.AspNetCore.Http.StatusCodes.Status200OK;

    /// <summary>
    /// Gets or sets the response status code to use when the overall report status is unhealthy.
    /// </summary>
    public int Unhealthy { get; set; } = Microsoft.AspNetCore.Http.StatusCodes.Status200OK;
}
