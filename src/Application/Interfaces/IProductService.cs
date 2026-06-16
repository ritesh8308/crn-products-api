using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Application service contract for the Product feature — the use cases the API
/// layer (Phase 5) calls. Speaks only in DTOs, never entities, so the web layer
/// has no path to the domain model. Implemented in Phase 4.
/// </summary>
public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateProductAsync(
        CreateProductDto dto, string createdBy, CancellationToken cancellationToken = default);

    Task UpdateProductAsync(
        int id, UpdateProductDto dto, string modifiedBy, CancellationToken cancellationToken = default);

    Task DeleteProductAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItemDto>> GetItemsForProductAsync(
        int productId, CancellationToken cancellationToken = default);
}
