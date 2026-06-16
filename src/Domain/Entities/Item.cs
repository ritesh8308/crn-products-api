namespace Domain.Entities;

/// <summary>
/// Core business entity representing a stock item belonging to a product.
/// Maps 1:1 to the assessment's Item table schema.
/// </summary>
public class Item
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    // Navigation property back to the owning product.
    public Product? Product { get; set; }
}
