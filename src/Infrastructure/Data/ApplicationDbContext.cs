using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Data;

/// <summary>
/// The EF Core "session" with the database. One instance lives for the length of a
/// single HTTP request (registered as scoped in DI) and is then disposed.
/// Coming from ADO.NET: this is the rough equivalent of a SqlConnection, but it also
/// tracks changes to loaded entities and turns them into SQL on SaveChangesAsync().
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Each DbSet<T> is a strongly-typed handle to a table you query with LINQ.
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Discover and apply every IEntityTypeConfiguration<T> in this assembly
        // (ProductConfiguration, ItemConfiguration). Keeps mapping out of the entities.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
