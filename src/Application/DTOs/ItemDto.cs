namespace Application.DTOs;

/// <summary>
/// Read-side shape of an Item returned to API clients.
/// Flat projection of the Item entity — no navigation property back to Product,
/// which avoids serialization cycles (Product -> Items -> Product -> ...).
/// </summary>
public class ItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
