using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API mapping for Product. Lives in Infrastructure so the Domain entity
/// stays a clean POCO with zero persistence attributes (honors the dependency rule).
/// Mirrors the exact schema from the assessment.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
               .ValueGeneratedOnAdd();           // IDENTITY(1,1)

        builder.Property(p => p.ProductName)
               .HasColumnType("nvarchar(255)")
               .IsRequired();                     // NOT NULL

        builder.Property(p => p.CreatedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();                     // NOT NULL

        builder.Property(p => p.CreatedOn)
               .HasColumnType("datetime")
               .IsRequired();                     // NOT NULL

        builder.Property(p => p.ModifiedBy)
               .HasColumnType("nvarchar(100)");   // NULL (nullable by default)

        builder.Property(p => p.ModifiedOn)
               .HasColumnType("datetime");        // NULL

        // One Product has many Items; each Item must have a Product (FK NOT NULL).
        // Deleting a Product cascades to its Items.
        builder.HasMany(p => p.Items)
               .WithOne(i => i.Product)
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
