using Microsoft.AspNetCore.Mvc.Testing;

namespace Task3.Tests;

public class StartupValidationTests : IDisposable
{
    private readonly Dictionary<string, string?> _originalEnvironment = new();

    [Fact]
    public void Invalid_Allowed_Origin_Stops_Startup()
    {
        SetEnvironment("App__AllowedOrigins__0", "not-a-url");
        SetEnvironment("App__Mode", "Training");
        SetEnvironment("App__RateLimits__Read__PermitLimit", "1");
        SetEnvironment("App__RateLimits__Read__WindowSeconds", "60");
        SetEnvironment("App__RateLimits__Read__QueueLimit", "0");
        SetEnvironment("App__RateLimits__Write__PermitLimit", "1");
        SetEnvironment("App__RateLimits__Write__WindowSeconds", "60");
        SetEnvironment("App__RateLimits__Write__QueueLimit", "0");

        using var factory = new WebApplicationFactory<Program>();

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("Некорректные настройки", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private void SetEnvironment(string key, string value)
    {
        if (!_originalEnvironment.ContainsKey(key))
        {
            _originalEnvironment[key] = Environment.GetEnvironmentVariable(key);
        }

        Environment.SetEnvironmentVariable(key, value);
    }

    public void Dispose()
    {
        foreach (var item in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(item.Key, item.Value);
        }
    }
}

