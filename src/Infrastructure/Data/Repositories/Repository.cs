using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Infrastructure.Data.Repositories;

/// <summary>
/// Generic repository. Implements the Application-layer IRepository<T> interface
/// against EF Core. Note the read methods use AsNoTracking() — we're fetching to
/// return, not to modify, so we skip the change-tracking overhead.
///
/// Important: this repository does NOT call SaveChangesAsync(). Persisting is the
/// UnitOfWork's job, so multiple repository operations can commit in one transaction.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // FindAsync hits the change tracker first, then the DB. Tracking ON here
        // because callers that load by id often intend to update or delete.
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        DbSet.Remove(entity);
    }
}
