using Microsoft.Extensions.Configuration;
using Task3.Web.Configuration;

namespace Task3.Tests;

public class ConfigurationPriorityTests
{
    [Fact]
    public void CommandLine_HasHighestPriority()
    {
        var contentRoot = GetContentRoot();
        var args = new[] { "App:Mode=Production" };
        var env = new Dictionary<string, string?>
        {
            ["App:Mode"] = "Training"
        };

        var configuration = AppConfiguration.BuildForTests(contentRoot, "Development", env, args);

        Assert.Equal("Production", configuration["App:Mode"]);
    }

    [Fact]
    public void Environment_Overrides_File()
    {
        var contentRoot = GetContentRoot();
        var env = new Dictionary<string, string?>
        {
            ["App:Mode"] = "Production"
        };

        var configuration = AppConfiguration.BuildForTests(contentRoot, "Development", env, Array.Empty<string>());

        Assert.Equal("Production", configuration["App:Mode"]);
    }

    private static string GetContentRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Task3.Web"));
    }
}
