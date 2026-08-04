namespace HealthChecks.MiniUI;

public sealed class SimpleHealthPageOptions
{
    public string Title { get; set; } = "Health Checks";

    public string? AuthorizationPolicy { get; set; }
}
