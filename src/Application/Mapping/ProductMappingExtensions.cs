using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

/// <summary>
/// Hand-written mappers between Product/Item entities and their DTOs.
/// Chosen over AutoMapper deliberately: the mapping is tiny and explicit, every
/// line is interview-defensible, and there's no reflection or profile scanning.
/// Entity -> DTO is for read paths; Create/Update DTO -> entity is for writes,
/// where audit fields are stamped by the service, not mapped from the client.
/// </summary>
public static class ProductMappingExtensions
{
    // ---- Item ----
    public static ItemDto ToDto(this Item item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        Quantity = item.Quantity
    };

    // ---- Product (read) ----
    public static ProductDto ToDto(this Product product, bool includeItems = false) => new()
    {
        Id = product.Id,
        ProductName = product.ProductName,
        CreatedBy = product.CreatedBy,
        CreatedOn = product.CreatedOn,
        ModifiedBy = product.ModifiedBy,
        ModifiedOn = product.ModifiedOn,
        Items = includeItems
            ? product.Items.Select(i => i.ToDto()).ToList()
            : new List<ItemDto>()
    };

    // ---- CreateProductDto -> new Product (write) ----
    // Only the client-supplied name is mapped. Id is identity-generated; audit
    // fields (CreatedBy/CreatedOn) are stamped by ProductService in Phase 4.
    public static Product ToEntity(this CreateProductDto dto) => new()
    {
        ProductName = dto.ProductName
    };

    // ---- UpdateProductDto -> existing Product (write) ----
    // Mutates only the business field on an already-loaded, tracked entity.
    // ModifiedBy/ModifiedOn are set by the service, not here.
    public static void ApplyTo(this UpdateProductDto dto, Product product)
    {
        product.ProductName = dto.ProductName;
    }
}
