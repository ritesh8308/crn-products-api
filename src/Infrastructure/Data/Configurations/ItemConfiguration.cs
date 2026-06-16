using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API mapping for Item. The FK relationship to Product is declared once,
/// on the Product side (ProductConfiguration), so we don't repeat HasOne here.
/// </summary>
public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Item");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
               .ValueGeneratedOnAdd();        // IDENTITY(1,1)

        builder.Property(i => i.ProductId)
               .IsRequired();                  // FK NOT NULL

        builder.Property(i => i.Quantity)
               .IsRequired();                  // INT NOT NULL

        // Index the FK column — speeds up "get items for a product" lookups.
        builder.HasIndex(i => i.ProductId);
    }
}
