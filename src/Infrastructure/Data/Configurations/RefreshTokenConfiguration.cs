using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API mapping for RefreshToken. The token value is indexed (unique) because
/// every refresh/revoke looks the row up by that value. IsExpired/IsActive are
/// computed properties on the entity, so they're explicitly NOT mapped to columns.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.Token)
               .HasColumnType("nvarchar(256)")
               .IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();

        builder.Property(t => t.UserId).IsRequired();

        builder.Property(t => t.ExpiresOn).HasColumnType("datetime").IsRequired();
        builder.Property(t => t.CreatedOn).HasColumnType("datetime").IsRequired();
        builder.Property(t => t.RevokedOn).HasColumnType("datetime");          // nullable
        builder.Property(t => t.ReplacedByToken).HasColumnType("nvarchar(256)"); // nullable

        // Computed, behavior-only properties — keep them out of the table.
        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsActive);
    }
}
