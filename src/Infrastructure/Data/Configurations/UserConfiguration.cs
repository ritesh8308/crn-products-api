using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API mapping for User. Table named "Users" (avoids the reserved word USER).
/// Username is unique so two accounts can't share a name.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.Username)
               .HasColumnType("nvarchar(100)")
               .IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();   // no duplicate usernames

        builder.Property(u => u.PasswordHash)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(u => u.Role)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(u => u.CreatedOn)
               .HasColumnType("datetime")
               .IsRequired();

        // A user owns many refresh tokens; deleting the user removes its tokens.
        builder.HasMany(u => u.RefreshTokens)
               .WithOne(t => t.User)
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
