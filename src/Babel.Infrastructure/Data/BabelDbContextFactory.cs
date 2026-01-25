using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Babel.Infrastructure.Data;

/// <summary>
/// Factory para crear BabelDbContext en tiempo de diseño (migraciones EF Core).
/// </summary>
public class BabelDbContextFactory : IDesignTimeDbContextFactory<BabelDbContext>
{
    public BabelDbContext CreateDbContext(string[] args)
    {
        // Buscar el directorio del proyecto API para cargar la configuración
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Babel.API");

        // Si no existe, usar el directorio actual (cuando se ejecuta desde la raíz)
        if (!Directory.Exists(basePath))
        {
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Babel.API");
        }

        // Si tampoco existe, usar directorio actual
        if (!Directory.Exists(basePath))
        {
            basePath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Usar una conexión por defecto si no hay configuración
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Server=localhost,1433;Database=BabelDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";
        }

        var optionsBuilder = new DbContextOptionsBuilder<BabelDbContext>();
        optionsBuilder.UseSqlServer(connectionString, b =>
            b.MigrationsAssembly(typeof(BabelDbContext).Assembly.FullName));

        return new BabelDbContext(optionsBuilder.Options);
    }
}
