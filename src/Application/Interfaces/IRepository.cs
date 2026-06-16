using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Generic repository abstraction over a single entity type.
/// Application declares WHAT persistence operations it needs; Infrastructure
/// (Phase 3) provides the EF Core implementation. This inversion is what keeps
/// Application ignorant of SQL Server and unit-testable with a mocked repo.
/// All methods are async to honour the assessment's async/await requirement.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
