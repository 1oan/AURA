using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Aura.Infrastructure.Persistence;

// Used only by EF Core CLI tooling (dotnet ef migrations add/update)
internal sealed class AuraDbContextFactory : IDesignTimeDbContextFactory<AuraDbContext>
{
    public AuraDbContext CreateDbContext(string[] args)
    {
        var basePath = FindApiProjectDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in appsettings.");

        var optionsBuilder = new DbContextOptionsBuilder<AuraDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.UseVector());

        return new AuraDbContext(optionsBuilder.Options);
    }

    // Walks up from CWD to find the src/Aura.API directory regardless of where dotnet ef is invoked
    private static string FindApiProjectDirectory()
    {
        var directory = Directory.GetCurrentDirectory();

        while (directory is not null)
        {
            var apiPath = Path.Combine(directory, "src", "Aura.API");
            if (Directory.Exists(apiPath))
                return apiPath;

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException(
            "Could not find 'src/Aura.API' directory. " +
            "Run dotnet ef from within the AURA solution directory.");
    }
}
