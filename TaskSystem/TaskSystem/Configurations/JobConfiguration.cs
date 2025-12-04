using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskSystem.Entities;

namespace TaskSystem.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<JobEntity>
{
    public void Configure(EntityTypeBuilder<JobEntity> builder)
    {
        builder.HasKey(x => x.JobId);

        builder.Property(b => b.Title)
               .IsRequired();

        builder.Property(b => b.Description)
               .IsRequired(false);

        builder.Property(b => b.DueDate)
               .IsRequired();

        builder.Property(b => b.CreatedAt)
               .IsRequired();

        builder.HasOne(b => b.Project)
               .WithMany(b => b.Job)
               .HasForeignKey(b => b.ProjectId)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired();
    }
}