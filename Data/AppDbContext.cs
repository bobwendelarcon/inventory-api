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



        //daily order

        public DbSet<DailyOrderHeader> DailyOrderHeaders { get; set; }
        public DbSet<DailyOrderLine> DailyOrderLines { get; set; }
        public DbSet<DailyOrderAllocation> DailyOrderAllocations { get; set; }

        public DbSet<DeliveryChecklistHeader> DeliveryChecklistHeaders { get; set; }
        public DbSet<DeliveryChecklistLine> DeliveryChecklistLines { get; set; }


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



            // Daily Order tables
            modelBuilder.Entity<DailyOrderHeader>().ToTable("daily_order_header");
            modelBuilder.Entity<DailyOrderHeader>().HasKey(x => x.order_id);

            modelBuilder.Entity<DailyOrderLine>().ToTable("daily_order_line");
            modelBuilder.Entity<DailyOrderLine>().HasKey(x => x.order_line_id);

            modelBuilder.Entity<DailyOrderAllocation>().ToTable("daily_order_allocation");
            modelBuilder.Entity<DailyOrderAllocation>().HasKey(x => x.allocation_id);

            // Relationships
            modelBuilder.Entity<DailyOrderLine>()
                .HasOne(x => x.Header)
                .WithMany(h => h.Lines)
                .HasForeignKey(x => x.order_id);

            modelBuilder.Entity<DailyOrderAllocation>()
                .HasOne(x => x.Line)
                .WithMany(l => l.Allocations)
                .HasForeignKey(x => x.order_line_id);

            modelBuilder.Entity<DeliveryChecklistHeader>().ToTable("delivery_checklist_header");
            modelBuilder.Entity<DeliveryChecklistHeader>().HasKey(x => x.checklist_id);

            modelBuilder.Entity<DeliveryChecklistLine>().ToTable("delivery_checklist_line");
            modelBuilder.Entity<DeliveryChecklistLine>().HasKey(x => x.checklist_line_id);

            modelBuilder.Entity<DeliveryChecklistLine>()
                .HasOne(x => x.Header)
                .WithMany(h => h.Lines)
                .HasForeignKey(x => x.checklist_id);



        }
    }
}