namespace Application.Interfaces;

/// <summary>
/// Unit of Work: commits all pending changes tracked across repositories in a
/// single transaction. Repositories stage changes (Add/Update/Remove); nothing
/// is persisted until SaveChangesAsync is called here. This mirrors EF Core's
/// DbContext.SaveChanges but keeps the Application layer free of EF types.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all staged changes. Returns the number of affected rows.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
