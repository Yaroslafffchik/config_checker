using Microsoft.Extensions.Configuration;

namespace Task3.Web.Configuration;

public static class AppConfiguration
{
    public static void Configure(ConfigurationManager configuration, IWebHostEnvironment environment, string[] args)
    {
        configuration.Sources.Clear();
        configuration.SetBasePath(environment.ContentRootPath);
        configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        configuration.AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
        configuration.AddEnvironmentVariables();
        configuration.AddCommandLine(args);
    }

    public static IConfigurationRoot BuildForTests(
        string contentRoot,
        string environmentName,
        IDictionary<string, string?>? environmentOverrides,
        string[]? args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false);

        if (environmentOverrides is not null)
        {
            builder.AddInMemoryCollection(environmentOverrides);
        }

        builder.AddCommandLine(args ?? Array.Empty<string>());
        return builder.Build();
    }
}

