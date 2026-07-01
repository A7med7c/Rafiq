using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Rafiq.Infrastructure.Persistence;

public sealed class RafiqDbContextFactory : IDesignTimeDbContextFactory<RafiqDbContext>
{
    public RafiqDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("src/Rafiq.API/appsettings.json", optional: true)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=LAPTOP-M0P8U45N\\SQLEXPRESS;Database=RafiqDbTwo;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<RafiqDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new RafiqDbContext(optionsBuilder.Options);
    }
}
