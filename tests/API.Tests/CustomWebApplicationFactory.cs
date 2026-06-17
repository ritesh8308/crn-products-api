using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Boots the real API in-memory for integration tests, but swaps the SQL Server
/// DbContext for EF Core's in-memory provider — so the tests exercise the full
/// pipeline (routing, auth, middleware, the admin seeder) without needing a database.
/// A unique database name per factory keeps test classes isolated from one another.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development keeps HSTS off (Swagger on is harmless for tests).
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Drop the SQL Server registration (its options carry the SqlServer provider)
            // and the context itself, then re-register against the in-memory provider.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                         || d.ServiceType == typeof(ApplicationDbContext))
                .ToList();
            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
