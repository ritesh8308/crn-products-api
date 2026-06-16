using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

/// <summary>
/// EF tooling (dotnet ef migrations/database update) needs to construct a DbContext
/// at DESIGN time, when the API isn't running and there's no DI container. This
/// factory tells the tooling how. The connection string here is only used by the
/// CLI on your machine; the real runtime string comes from configuration via DI.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=CrnProducts;User Id=sa;Password=Your_strong_Pass123;TrustServerCertificate=True;");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
