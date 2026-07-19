namespace Maliev.PredictionService.Tests.Security;

public class ProgramSecurityConfigurationTests
{
    [Fact]
    public void Program_UsesStandardCorsInsteadOfAllowAnyOrigin()
    {
        var programSource = File.ReadAllText(FindProgramSource());

        Assert.Contains("builder.AddStandardCors()", programSource);
        Assert.DoesNotContain("AllowAnyOrigin", programSource);
    }

    private static string FindProgramSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Maliev.PredictionService.Api",
                "Program.cs");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Maliev.PredictionService.Api/Program.cs.");
    }
}
