using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Product-specific persistence contract. Extends the generic repository with
/// the queries the Product feature needs that don't fit the generic shape:
/// a paginated page slice, a single product loaded WITH its items, and the
/// related-items lookup. Implementations live in Infrastructure (Phase 3) and
/// use AsNoTracking() on these read paths per the performance requirement.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Returns one page of products plus the total row count, in a single round.
    /// </summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a product with its Items navigation collection populated (eager load).
    /// </summary>
    Task<Product?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the items belonging to a product (the "get related items" endpoint).
    /// </summary>
    Task<IReadOnlyList<Item>> GetItemsForProductAsync(int productId, CancellationToken cancellationToken = default);
}
