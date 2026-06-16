using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Infrastructure.Data.Repositories;

/// <summary>
/// Product repository. Inherits the generic CRUD from Repository<Product> and adds
/// the Product-specific reads the assessment requires: paginated list, and the
/// related-items query. Both read paths use AsNoTracking().
/// </summary>
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns one page of products plus the total count (for the client to compute
    /// page counts). OrderBy is required — without a stable sort, Skip/Take results
    /// are not guaranteed consistent across pages.
    /// </summary>
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Context.Products.AsNoTracking().OrderBy(p => p.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Loads a single product with its Items navigation collection populated
    /// (eager load via Include). AsNoTracking — this is a read path.
    /// </summary>
    public async Task<Product?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Products
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// The "get related Items for a product" endpoint backing query.
    /// </summary>
    public async Task<IReadOnlyList<Item>> GetItemsForProductAsync(
        int productId, CancellationToken cancellationToken = default)
    {
        return await Context.Items
            .AsNoTracking()
            .Where(i => i.ProductId == productId)
            .ToListAsync(cancellationToken);
    }
}
