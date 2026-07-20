using inventory_api.Data;
using inventory_api.Models;
using inventory_api.Models.Manufacturing.Materials;
using inventory_api.Models.Purchasing;
using inventory_api.Models.Purchasing.Canvassing;
using inventory_api.Models.Purchasing.PurchaseOrders;
using inventory_api.Models.Purchasing.QcInspections;
using inventory_api.Models.Purchasing.ReceivingReports;
using inventory_api.Models.Purchasing.Suppliers;
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


        //ptp

        public DbSet<ProductToProduceHeader> ProductToProduceHeaders { get; set; }
        public DbSet<ProductToProduceLine> ProductToProduceLines { get; set; }

        //return

        public DbSet<ReturnHeader> ReturnHeaders { get; set; }
        public DbSet<ReturnLine> ReturnLines { get; set; }



        //manufacturing

        public DbSet<MaterialCategory> MaterialCategories { get; set; }
        public DbSet<Material> Materials { get; set; }

        public DbSet<MaterialSubCategory> MaterialSubCategories { get; set; }
        public DbSet<MaterialLotNumber> MaterialLotNumbers { get; set; }
        public DbSet<MaterialInventoryTransaction> MaterialInventoryTransactions { get; set; }
        public DbSet<MaterialInventoryPlanning> MaterialInventoryPlannings { get; set; }
        public DbSet<MaterialPurchaseRecommendation> MaterialPurchaseRecommendations { get; set; }


        //purchasing
        public DbSet<MprfHeader> PurchasingMprfHeaders { get; set; }
        public DbSet<MprfLine> PurchasingMprfLines { get; set; }


        //purchasing - supplier
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<SupplierMaterial> SupplierMaterials { get; set; }

        //supplier manufacturer

        public DbSet<SupplierManufacturer> SupplierManufacturers { get; set; }
        // canvassing

        public DbSet<PurchasingCanvassHeader> PurchasingCanvassHeaders { get; set; }
        public DbSet<PurchasingCanvassLine> PurchasingCanvassLines { get; set; }
        public DbSet<PurchasingCanvassQuote> PurchasingCanvassQuotes { get; set; }


        //PO
        public DbSet<PurchaseOrderHeader> PurchaseOrderHeaders { get; set; }
        public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }

        public DbSet<PurchaseOrderDeliverySchedule> PurchaseOrderDeliverySchedules  {get; set; }

        public DbSet<PurchaseOrderDeliveryScheduleLine>
            PurchaseOrderDeliveryScheduleLines
        { get; set; }


        //Receiving

        public DbSet<ReceivingReportHeader> ReceivingReportHeaders { get; set; }
        public DbSet<ReceivingReportLine> ReceivingReportLines { get; set; }

        //qc inspection

        public DbSet<QcInspectionHeader> QcInspectionHeaders { get; set; }
        public DbSet<QcInspectionLine> QcInspectionLines { get; set; }

        public DbSet<QcInspectionLineLot> QcInspectionLineLots { get; set; }




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


            //ptp

            modelBuilder.Entity<ProductToProduceHeader>(entity =>
            {
                entity.ToTable("product_to_produce_header");
                entity.HasKey(e => e.ptp_id);

                entity.HasMany(e => e.Lines)
                    .WithOne(e => e.Header)
                    .HasForeignKey(e => e.ptp_id);
            });

            modelBuilder.Entity<ProductToProduceLine>(entity =>
            {
                entity.ToTable("product_to_produce_line");
                entity.HasKey(e => e.ptp_line_id);
            });

            modelBuilder.Entity<DeliveryChecklistHeader>().ToTable("delivery_checklist_header");
            modelBuilder.Entity<DeliveryChecklistHeader>().HasKey(x => x.checklist_id);

            modelBuilder.Entity<DeliveryChecklistLine>().ToTable("delivery_checklist_line");
            modelBuilder.Entity<DeliveryChecklistLine>().HasKey(x => x.checklist_line_id);

            modelBuilder.Entity<DeliveryChecklistLine>()
                .HasOne(x => x.Header)
                .WithMany(h => h.Lines)
                .HasForeignKey(x => x.checklist_id);


            //return 

            modelBuilder.Entity<ReturnHeader>(entity =>
            {
                entity.ToTable("return_header");
                entity.HasKey(e => e.return_id);

                entity.HasMany(e => e.Lines)
                    .WithOne(e => e.ReturnHeader)
                    .HasForeignKey(e => e.return_id);
            });

            modelBuilder.Entity<ReturnLine>(entity =>
            {
                entity.ToTable("return_line");
                entity.HasKey(e => e.return_line_id);

                entity.Property(e => e.quantity).HasPrecision(18, 2);
                entity.Property(e => e.released_qty).HasPrecision(18, 2);
            });

            //Manufacturing


            //purchasing

            modelBuilder.Entity<MprfHeader>()
     .HasMany(x => x.lines)
     .WithOne()
     .HasForeignKey(x => x.mprf_id)
     .OnDelete(DeleteBehavior.Cascade);


            //purchasing-supplier

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("purchasing_suppliers");
                entity.HasKey(e => e.SupplierId);

                entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
                entity.Property(e => e.SupplierCode).HasColumnName("supplier_code");
                entity.Property(e => e.SupplierName).HasColumnName("supplier_name");
                entity.Property(e => e.SupplierType).HasColumnName("supplier_type");
                entity.Property(e => e.ContactPerson).HasColumnName("contact_person");
                entity.Property(e => e.ContactNumber).HasColumnName("contact_number");
                entity.Property(e => e.EmailAddress).HasColumnName("email_address");
                entity.Property(e => e.Address).HasColumnName("address");
                entity.Property(e => e.PaymentTerms).HasColumnName("payment_terms");
                entity.Property(e => e.LeadTimeDays).HasColumnName("lead_time_days");
                entity.Property(e => e.Currency).HasColumnName("currency");
                entity.Property(e => e.IsPreferred).HasColumnName("is_preferred");
                entity.Property(e => e.Remarks).HasColumnName("remarks");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<Manufacturer>(entity =>
            {
                entity.ToTable("purchasing_manufacturers");
                entity.HasKey(e => e.ManufacturerId);

                entity.Property(e => e.ManufacturerId).HasColumnName("manufacturer_id");
                entity.Property(e => e.ManufacturerName).HasColumnName("manufacturer_name");
                entity.Property(e => e.AccreditationStatus).HasColumnName("accreditation_status");
                entity.Property(e => e.AccreditationDate).HasColumnName("accreditation_date");
                entity.Property(e => e.AccreditationExpiry).HasColumnName("accreditation_expiry");
                entity.Property(e => e.CoaRequired).HasColumnName("coa_required");
                entity.Property(e => e.Remarks).HasColumnName("remarks");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<SupplierMaterial>(entity =>
            {
                entity.ToTable("purchasing_supplier_materials");
                entity.HasKey(e => e.SupplierMaterialId);

                entity.Property(e => e.SupplierMaterialId).HasColumnName("supplier_material_id");
                entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
                entity.Property(e => e.MaterialId).HasColumnName("material_id");
                entity.Property(e => e.ManufacturerId).HasColumnName("manufacturer_id");
                entity.Property(e => e.IsPreferred).HasColumnName("is_preferred");
                entity.Property(e => e.Remarks).HasColumnName("remarks");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<SupplierManufacturer>(entity =>
            {
                entity.ToTable("purchasing_supplier_manufacturers");

                entity.HasKey(e => e.SupplierManufacturerId);

                entity.Property(e => e.SupplierManufacturerId).HasColumnName("supplier_manufacturer_id");
                entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
                entity.Property(e => e.ManufacturerId).HasColumnName("manufacturer_id");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });


            //PO

            modelBuilder.Entity<PurchaseOrderHeader>(entity =>
            {
                entity.ToTable("purchasing_po_header");

                entity.HasKey(e => e.PoId);

                entity.Property(e => e.PoId).HasColumnName("po_id");
                entity.Property(e => e.PoNo).HasColumnName("po_no");

                entity.Property(e => e.CanvassId).HasColumnName("canvass_id");
                entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

                entity.Property(e => e.PoDate).HasColumnName("po_date");
                entity.Property(e => e.DeliveryDate).HasColumnName("delivery_date");

                entity.Property(e => e.PaymentTerms).HasColumnName("payment_terms");
                entity.Property(e => e.Remarks).HasColumnName("remarks");

                entity.Property(e => e.Status).HasColumnName("status");

                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");

                entity.Property(e => e.SupplierAddress).HasColumnName("supplier_address");
                entity.Property(e => e.RequestedBy).HasColumnName("requested_by");

                entity.Property(e => e.Subtotal).HasColumnName("subtotal");
                entity.Property(e => e.OtherCharges).HasColumnName("other_charges");
                entity.Property(e => e.TotalAmount).HasColumnName("total_amount");

                entity.Property(e => e.CheckedBy).HasColumnName("checked_by");
                entity.Property(e => e.CheckedAt).HasColumnName("checked_at");
                entity.Property(e => e.PrintedPoNo).HasColumnName("printed_po_no");



                entity.HasMany(e => e.Lines)
                      .WithOne(e => e.Header)
                      .HasForeignKey(e => e.PoId);
            });


            modelBuilder.Entity<PurchaseOrderDeliverySchedule>(entity =>
            {
                entity.ToTable("purchasing_po_delivery_schedule");

                entity.HasKey(e => e.ScheduleId);

                entity.Property(e => e.ScheduleId)
                    .HasColumnName("schedule_id");

                entity.Property(e => e.PoId)
                    .HasColumnName("po_id");

                entity.Property(e => e.ScheduleNo)
                    .HasColumnName("schedule_no");

                entity.Property(e => e.ScheduledDate)
                    .HasColumnName("scheduled_date");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(40);

                entity.Property(e => e.RescheduledFromScheduleId)
                    .HasColumnName("rescheduled_from_schedule_id");

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedBy)
                    .HasColumnName("updated_by");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasIndex(e => new
                {
                    e.PoId,
                    e.ScheduleNo
                })
                .IsUnique();

                entity.HasOne(e => e.PurchaseOrder)
                    .WithMany(e => e.DeliverySchedules)
                    .HasForeignKey(e => e.PoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RescheduledFromSchedule)
                    .WithMany()
                    .HasForeignKey(e => e.RescheduledFromScheduleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseOrderDeliveryScheduleLine>(entity =>
            {
                entity.ToTable("purchasing_po_delivery_schedule_line");

                entity.HasKey(e => e.ScheduleLineId);

                entity.Property(e => e.ScheduleLineId)
                    .HasColumnName("schedule_line_id");

                entity.Property(e => e.ScheduleId)
                    .HasColumnName("schedule_id");

                entity.Property(e => e.PoLineId)
                    .HasColumnName("po_line_id");

                entity.Property(e => e.ScheduledQty)
                    .HasColumnName("scheduled_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.ReceivedQty)
                    .HasColumnName("received_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.BalanceQty)
                    .HasColumnName("balance_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(40);

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasIndex(e => new
                {
                    e.ScheduleId,
                    e.PoLineId
                })
                .IsUnique();

                entity.HasOne(e => e.Schedule)
                    .WithMany(e => e.Lines)
                    .HasForeignKey(e => e.ScheduleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PurchaseOrderLine)
                    .WithMany(e => e.DeliveryScheduleLines)
                    .HasForeignKey(e => e.PoLineId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseOrderLine>(entity =>
            {
                entity.ToTable("purchasing_po_line");

                entity.HasKey(e => e.PoLineId);

                entity.Property(e => e.PoLineId).HasColumnName("po_line_id");
                entity.Property(e => e.PoId).HasColumnName("po_id");

                entity.Property(e => e.CanvassLineId).HasColumnName("canvass_line_id");
                entity.Property(e => e.QuoteId).HasColumnName("quote_id");

                entity.Property(e => e.MaterialId).HasColumnName("material_id");

                entity.Property(e => e.PoQty).HasColumnName("po_qty");
                entity.Property(e => e.Uom).HasColumnName("uom");

                entity.Property(e => e.QuotationUnitPrice).HasColumnName("quotation_unit_price");
                entity.Property(e => e.PoUnitPrice).HasColumnName("po_unit_price");
                entity.Property(e => e.LineTotal).HasColumnName("line_total");

                entity.Property(e => e.Remarks).HasColumnName("remarks");

                entity.Property(e => e.ReceivedQty).HasColumnName("received_qty");
                entity.Property(e => e.BalanceQty).HasColumnName("balance_qty");

                entity.Property(e => e.Status).HasColumnName("status");

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });


            //receiving

            modelBuilder.Entity<ReceivingReportHeader>(entity =>
            {
                entity.ToTable("purchasing_rr_header");
                entity.HasKey(e => e.RrId);

                entity.Property(e => e.RrId).HasColumnName("rr_id");
                entity.Property(e => e.RrNo).HasColumnName("rr_no");
                entity.Property(e => e.PoId).HasColumnName("po_id");
                entity.Property(e => e.PoNo).HasColumnName("po_no");
                entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
                entity.Property(e => e.SiDrNo).HasColumnName("si_dr_no");
                entity.Property(e => e.DeliveryDate).HasColumnName("delivery_date");
                entity.Property(e => e.Remarks).HasColumnName("remarks");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.QcBy).HasColumnName("qc_by");
                entity.Property(e => e.QcAt).HasColumnName("qc_at");
                entity.Property(e => e.CommittedBy).HasColumnName("committed_by");
                entity.Property(e => e.CommittedAt).HasColumnName("committed_at");

                entity.Property(e => e.ScheduleId)
    .HasColumnName("schedule_id");

                entity.HasOne(e => e.DeliverySchedule)
                    .WithMany()
                    .HasForeignKey(e => e.ScheduleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Lines)
                      .WithOne(e => e.Header)
                      .HasForeignKey(e => e.RrId);
            });

            modelBuilder.Entity<ReceivingReportLine>(entity =>
            {
                entity.ToTable("purchasing_rr_line");
                entity.HasKey(e => e.RrLineId);

                entity.Property(e => e.RrLineId).HasColumnName("rr_line_id");
                entity.Property(e => e.RrId).HasColumnName("rr_id");
                entity.Property(e => e.PoLineId).HasColumnName("po_line_id");
                entity.Property(e => e.MaterialId).HasColumnName("material_id");
                entity.Property(e => e.PoQty).HasColumnName("po_qty");
                entity.Property(e => e.PreviouslyReceivedQty).HasColumnName("previously_received_qty");
                entity.Property(e => e.BalanceQty).HasColumnName("balance_qty");
                entity.Property(e => e.ReceiveQty).HasColumnName("receive_qty");
                entity.Property(e => e.AcceptedQty).HasColumnName("accepted_qty");
                entity.Property(e => e.RejectedQty).HasColumnName("rejected_qty");
                entity.Property(e => e.Uom).HasColumnName("uom");
                entity.Property(e => e.Remarks).HasColumnName("remarks");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });


            //qc inspection

            modelBuilder.Entity<QcInspectionHeader>(entity =>
            {
                entity.ToTable("purchasing_qc_header");

                entity.HasKey(e => e.QcId);

                entity.Property(e => e.QcId).HasColumnName("qc_id");
                entity.Property(e => e.QcNo).HasColumnName("qc_no");

                entity.Property(e => e.RrId).HasColumnName("rr_id");
                entity.Property(e => e.RrNo).HasColumnName("rr_no");

                entity.Property(e => e.PoId).HasColumnName("po_id");
                entity.Property(e => e.PoNo).HasColumnName("po_no");

                entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

                entity.Property(e => e.InspectionDate).HasColumnName("inspection_date");
                entity.Property(e => e.InspectorId).HasColumnName("inspector_id");

                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.Decision).HasColumnName("decision");

                entity.Property(e => e.Remarks).HasColumnName("remarks");

                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.CommittedBy).HasColumnName("committed_by");
                entity.Property(e => e.CommittedAt).HasColumnName("committed_at");

                entity.HasMany(e => e.Lines)
                      .WithOne(e => e.Header)
                      .HasForeignKey(e => e.QcId);
            });

            modelBuilder.Entity<QcInspectionLine>(entity =>
            {
                entity.ToTable("purchasing_qc_line");

                entity.HasKey(e => e.QcLineId);

                entity.Property(e => e.QcLineId)
                    .HasColumnName("qc_line_id");

                entity.Property(e => e.QcId)
                    .HasColumnName("qc_id");

                entity.Property(e => e.RrLineId)
                    .HasColumnName("rr_line_id");

                entity.Property(e => e.PoLineId)
                    .HasColumnName("po_line_id");

                entity.Property(e => e.MaterialId)
                    .HasColumnName("material_id");

                entity.Property(e => e.ReceivedQty)
                    .HasColumnName("received_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.AcceptedQty)
                    .HasColumnName("accepted_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.RejectedQty)
                    .HasColumnName("rejected_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks");

                entity.Property(e => e.Status)
                    .HasColumnName("status");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasMany(e => e.Lots)
                    .WithOne(e => e.QcLine)
                    .HasForeignKey(e => e.QcLineId)
                    .OnDelete(DeleteBehavior.Cascade);
            });



            modelBuilder.Entity<QcInspectionLineLot>(entity =>
            {
                entity.ToTable("purchasing_qc_line_lot");

                entity.HasKey(e => e.QcLineLotId);

                entity.HasIndex(e => new
                {
                    e.QcLineId,
                    e.LotNo
                })
                .IsUnique();
            });




        }
    }
}