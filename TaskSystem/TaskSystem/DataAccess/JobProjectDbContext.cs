using Microsoft.EntityFrameworkCore;
using TaskSystem.Configurations;
using TaskSystem.Entities;

namespace TaskSystem.DataAccess;

public class JobProjectDbContext : DbContext
{
    public JobProjectDbContext(DbContextOptions<JobProjectDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new JobConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
    }

    public DbSet<JobEntity> Jobs { get; set; }
    public DbSet<ProjectEntity> Projects { get; set; }
}