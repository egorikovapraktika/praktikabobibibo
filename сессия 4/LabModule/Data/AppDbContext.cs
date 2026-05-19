using Microsoft.EntityFrameworkCore;
using LabModule.Models;

namespace LabModule.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<QualityControl> QualityControls { get; set; }
        public DbSet<Deviation> Deviations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<ProductionOrder> ProductionOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Batch>().ToTable("batches");
            modelBuilder.Entity<QualityControl>().ToTable("quality_controls");
            modelBuilder.Entity<Deviation>().ToTable("deviations");
            modelBuilder.Entity<Notification>().ToTable("notifications");
            modelBuilder.Entity<Product>().ToTable("products");
            modelBuilder.Entity<RawMaterial>().ToTable("raw_materials");
            modelBuilder.Entity<Recipe>().ToTable("recipes");
            modelBuilder.Entity<ProductionOrder>().ToTable("production_orders");
        }
    }
}