namespace Application.DTOs;

/// <summary>
/// Generic envelope for paginated collection responses.
/// Carries the current page slice plus the metadata a client needs to render
/// pagination controls (total count, page, size, total pages).
/// Used by "GET all products with pagination" (Phase 4/5).
/// </summary>
/// <typeparam name="T">The item type in the page (e.g. ProductDto).</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    // Computed so it always stays consistent with the values above.
    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
