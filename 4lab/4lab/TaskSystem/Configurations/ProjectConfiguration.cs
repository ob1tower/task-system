using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskSystem.Entities;

namespace TaskSystem.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    public void Configure(EntityTypeBuilder<ProjectEntity> builder)
    {
        builder.HasKey(x => x.ProjectId);

        builder.Property(b => b.Name)
               .IsRequired();

        builder.Property(b => b.Description)
               .IsRequired(false);

        builder.Property(b => b.CreatedAt)
               .IsRequired();

        builder.HasMany(b => b.Job)
               .WithOne(b => b.Project)
               .HasForeignKey(b => b.ProjectId)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired();
    }
}