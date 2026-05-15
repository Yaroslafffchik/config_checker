using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Task3.Web.Configuration;
using Task3.Web.Middleware;
using Task3.Web.Models;
using Task3.Web.Services;

var builder = WebApplication.CreateBuilder(args);

AppConfiguration.Configure(builder.Configuration, builder.Environment, args);

var settings = builder.Configuration.GetSection("App").Get<AppSettings>() ?? new AppSettings();
var validationErrors = AppSettingsValidator.Validate(settings);

if (validationErrors.Count > 0)
{
    foreach (var error in validationErrors)
    {
        Console.Error.WriteLine(error);
    }

    throw new InvalidOperationException("Некорректные настройки приложения.");
}

if (settings.Mode == AppMode.Training)
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<ItemStore>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("TrustedOrigins", policy =>
    {
        policy.WithOrigins(settings.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Content-Type-Options", "X-Frame-Options", "Cache-Control", "Access-Control-Allow-Origin");
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var message = settings.Mode == AppMode.Training
            ? "Превышен лимит запросов. Подождите и повторите попытку."
            : "Слишком много запросов.";
        await context.HttpContext.Response.WriteAsync($"{{\"error\":\"{message}\"}}");
    };

    options.AddPolicy("ReadPolicy", httpContext =>
    {
        var key = GetClientKey(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.RateLimits.Read.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.RateLimits.Read.WindowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = settings.RateLimits.Read.QueueLimit
        });
    });

    options.AddPolicy("WritePolicy", httpContext =>
    {
        var key = GetClientKey(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.RateLimits.Write.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.RateLimits.Write.WindowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = settings.RateLimits.Write.QueueLimit
        });
    });
});

var app = builder.Build();

if (settings.Mode == AppMode.Training)
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(_ => { });
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("TrustedOrigins");
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok(new { status = "ok", mode = settings.Mode.ToString() }));

app.MapGet("/items", (ItemStore store) => Results.Ok(store.GetAll()))
    .RequireRateLimiting("ReadPolicy");

app.MapPost("/items", (ItemStore store, CreateItemRequest request) =>
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            var message = settings.Mode == AppMode.Training
                ? "Имя элемента обязательно и не должно превышать 100 символов."
                : "Некорректные данные.";

            return Results.BadRequest(new { error = message });
        }

        var item = store.Add(name);
        return Results.Created($"/items/{item.Id}", item);
    })
    .RequireRateLimiting("WritePolicy");

app.Run();

static string GetClientKey(HttpContext context)
{
    if (context.Request.Headers.TryGetValue("Origin", out var origin) && !string.IsNullOrWhiteSpace(origin))
    {
        return origin.ToString();
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

public partial class Program
{
}
