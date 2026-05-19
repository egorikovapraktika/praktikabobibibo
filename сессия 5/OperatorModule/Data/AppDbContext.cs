using Microsoft.EntityFrameworkCore;
using OperatorModule.Models;

namespace OperatorModule.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<TechMap> TechMaps { get; set; }
        public DbSet<TechMapStep> TechMapSteps { get; set; }
        public DbSet<BatchStep> BatchSteps { get; set; }
        public DbSet<Deviation> Deviations { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Batch>().ToTable("batches");
            modelBuilder.Entity<TechMap>().ToTable("tech_maps");
            modelBuilder.Entity<TechMapStep>().ToTable("tech_map_steps");
            modelBuilder.Entity<BatchStep>().ToTable("batch_steps");
            modelBuilder.Entity<Deviation>().ToTable("deviations");
            modelBuilder.Entity<Product>().ToTable("products");
        }
    }
}