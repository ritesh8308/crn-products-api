namespace Application.DTOs;

/// <summary>
/// Write-side shape for updating a product (the PUT body).
/// Like Create, it exposes only the mutable business field. The route supplies
/// the id; ModifiedBy/ModifiedOn are stamped by the service, not the client.
/// </summary>
public class UpdateProductDto
{
    public string ProductName { get; set; } = string.Empty;
}
