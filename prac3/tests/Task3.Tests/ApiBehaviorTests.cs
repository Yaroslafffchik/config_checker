using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Task3.Tests;

public class ApiBehaviorTests : IDisposable
{
    private readonly Dictionary<string, string?> _originalEnvironment = new();

    [Fact]
    public async Task Cors_Allows_Trusted_Origin()
    {
        using var factory = CreateFactoryWithMode("Training");
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/items");
        request.Headers.Add("Origin", "http://allowed.test");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://allowed.test", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Cors_Blocks_Untrusted_Origin()
    {
        using var factory = CreateFactoryWithMode("Training");
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/items");
        request.Headers.Add("Origin", "http://evil.test");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task RateLimiter_Reacts_On_Exceeding_Limit()
    {
        using var factory = CreateFactoryWithMode("Training", readLimit: 2, writeLimit: 1);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/items");
        request.Headers.Add("Origin", "http://allowed.test");

        var first = await client.SendAsync(Clone(request));
        var second = await client.SendAsync(Clone(request));
        var third = await client.SendAsync(Clone(request));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal((HttpStatusCode)429, third.StatusCode);
    }

    [Fact]
    public async Task Security_Headers_Are_Present()
    {
        using var factory = CreateFactoryWithMode("Training");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/items");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Cache-Control"));
    }

    [Fact]
    public async Task Training_Mode_Returns_Detailed_Validation_Error()
    {
        using var factory = CreateFactoryWithMode("Training");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/items", new { Name = "" });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Имя элемента обязательно", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Production_Mode_Returns_Short_Validation_Error()
    {
        using var factory = CreateFactoryWithMode("Production");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/items", new { Name = "" });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Некорректные данные", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Имя элемента", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Write_Limit_Is_Separate_From_Read_Limit()
    {
        using var factory = CreateFactoryWithMode("Training", readLimit: 5, writeLimit: 1);
        using var client = factory.CreateClient();

        var firstWrite = await client.PostAsJsonAsync("/items", new { Name = "first" });
        var secondWrite = await client.PostAsJsonAsync("/items", new { Name = "second" });
        var readResponse = await client.GetAsync("/items");

        Assert.Equal(HttpStatusCode.Created, firstWrite.StatusCode);
        Assert.Equal((HttpStatusCode)429, secondWrite.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactoryWithMode(string mode, int readLimit = 5, int writeLimit = 2)
    {
        SetEnvironment("App__Mode", mode);
        SetEnvironment("App__AllowedOrigins__0", "http://allowed.test");
        SetEnvironment("App__RateLimits__Read__PermitLimit", readLimit.ToString());
        SetEnvironment("App__RateLimits__Read__WindowSeconds", "60");
        SetEnvironment("App__RateLimits__Read__QueueLimit", "0");
        SetEnvironment("App__RateLimits__Write__PermitLimit", writeLimit.ToString());
        SetEnvironment("App__RateLimits__Write__WindowSeconds", "60");
        SetEnvironment("App__RateLimits__Write__QueueLimit", "0");

        return new WebApplicationFactory<Program>();
    }

    private void SetEnvironment(string key, string value)
    {
        if (!_originalEnvironment.ContainsKey(key))
        {
            _originalEnvironment[key] = Environment.GetEnvironmentVariable(key);
        }

        Environment.SetEnvironmentVariable(key, value);
    }

    private static HttpRequestMessage Clone(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    public void Dispose()
    {
        foreach (var item in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(item.Key, item.Value);
        }
    }
}
