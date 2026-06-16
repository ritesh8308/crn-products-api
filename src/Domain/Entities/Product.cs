namespace Domain.Entities;

/// <summary>
/// Core business entity representing a product.
/// Maps 1:1 to the assessment's Product table schema.
/// Audit fields are inline (not a base class) to match the spec exactly.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    // Navigation property: the related items for this product.
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
