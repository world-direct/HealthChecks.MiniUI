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
}
