namespace Task3.Web.Models;

public enum AppMode
{
    Training,
    Production
}

public sealed class AppSettings
{
    public AppMode Mode { get; set; } = AppMode.Training;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public RateLimitSettings RateLimits { get; set; } = new();
}

public sealed class RateLimitSettings
{
    public FixedWindowLimitSettings Read { get; set; } = new();
    public FixedWindowLimitSettings Write { get; set; } = new();
}

public sealed class FixedWindowLimitSettings
{
    public int PermitLimit { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 0;
}

