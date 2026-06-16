using Application.Interfaces;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data;

/// <summary>
/// Unit of Work. Owns the single DbContext for a request and the SaveChangesAsync()
/// call, so several repository operations commit together in one transaction.
/// EF Core already implements UoW + repository internally (DbContext + DbSet), but
/// wrapping it keeps the Application layer depending on our own interfaces, not EF.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Products = new ProductRepository(_context);
    }

    public IProductRepository Products { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // EF inspects every tracked entity, generates INSERT/UPDATE/DELETE, and
        // runs them inside an implicit transaction. Returns rows affected.
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
