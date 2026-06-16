namespace Application.DTOs;

/// <summary>
/// Read-side shape of a Product returned to API clients.
/// Includes audit fields (clients may read them) but the entity is never
/// exposed directly. Items are included as DTOs for the "get related items" path.
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    // Populated only when the caller asks for a product *with* its items.
    // Left empty otherwise so list endpoints stay lean.
    public List<ItemDto> Items { get; set; } = new();
}
