namespace Application.DTOs;

/// <summary>
/// Write-side shape for creating a product (the POST body).
/// Deliberately carries ONLY what a client is allowed to set: the name.
/// Id is server-assigned (identity); CreatedBy/CreatedOn are stamped by the
/// service (Phase 4), never trusted from the client. FluentValidation rules
/// for this type are added in Phase 4.
/// </summary>
public class CreateProductDto
{
    public string ProductName { get; set; } = string.Empty;
}
