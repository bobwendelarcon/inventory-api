using inventory_api.Data;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Tables
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<ProductLotNumber> ProductLotNumbers { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Partner> Partners { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names (match your MySQL tables)
         
            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<Category>().HasKey(x => x.catg_id);

            modelBuilder.Entity<Branch>().ToTable("branches");
            modelBuilder.Entity<Branch>().HasKey(x => x.branch_id);

            modelBuilder.Entity<ProductLotNumber>().ToTable("product_lot_number");
            modelBuilder.Entity<ProductLotNumber>().HasKey(x => x.lot_entry_id);

            modelBuilder.Entity<Product>().ToTable("products");
            modelBuilder.Entity<Product>().HasKey(x => x.product_id);


            modelBuilder.Entity<InventoryTransaction>().ToTable("inventory_transactions");
            modelBuilder.Entity<InventoryTransaction>().HasKey(x => x.transaction_id);

            //modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Partner>().ToTable("partners");
            modelBuilder.Entity<Partner>().HasKey(x => x.partner_id);

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<User>().HasKey(x => x.user_id);

            // Composite / unique keys example
            modelBuilder.Entity<ProductLotNumber>()
                .HasIndex(p => new { p.product_id, p.branch_id, p.lot_no })
                .IsUnique();
        }
    }
}