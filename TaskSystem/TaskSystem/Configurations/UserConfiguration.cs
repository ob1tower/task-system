using TaskSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaskSystem.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.HasKey(x => x.UserId);

        builder.Property(b => b.Email)
               .IsRequired();

        builder.Property(b => b.PasswordHash)
               .IsRequired();

        builder.Property(b => b.CreatedAt)
               .IsRequired();

        builder.HasOne(b => b.Role)
               .WithMany(b => b.User)
               .HasForeignKey(b => b.RoleId)
               .IsRequired();
    }
}
