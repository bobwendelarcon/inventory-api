using inventory_api.Data;
using inventory_api.Models;
using inventory_api.Models.Manufacturing.Materials;
using inventory_api.Models.Manufacturing.Materials.Requisitions;
using inventory_api.Models.Purchasing;
using inventory_api.Models.Purchasing.Canvassing;
using inventory_api.Models.Purchasing.FinalReceiving;
using inventory_api.Models.Purchasing.PurchaseOrders;
using inventory_api.Models.Purchasing.QaQcReceiving;
using inventory_api.Models.Purchasing.QcInspections;
using inventory_api.Models.Purchasing.Quarantine;
using inventory_api.Models.Purchasing.RawMaterialProcessing;
using inventory_api.Models.Purchasing.Receiving;
using inventory_api.Models.Purchasing.ReceivingReports;
using inventory_api.Models.Purchasing.Suppliers;
using inventory_api.Models.SupplierEvaluation;
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

        // Material Requisition / Stock OUT
        public DbSet<MaterialRequisition> MaterialRequisitions { get; set; }
        public DbSet<MaterialRequisitionLine> MaterialRequisitionLines { get; set; }


        //purchasing
        public DbSet<MprfHeader> PurchasingMprfHeaders { get; set; }
        public DbSet<MprfLine> PurchasingMprfLines { get; set; }


        //purchasing - supplier
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<SupplierMaterial> SupplierMaterials { get; set; }


        public DbSet<QaQcReceivingInspection>
    QaQcReceivingInspections
        { get; set; }

        public DbSet<QaQcReceivingInspectionLine>
            QaQcReceivingInspectionLines
        { get; set; }

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

        public DbSet<IncomingReceivingLineLot> IncomingReceivingLineLots { get; set; }

        //qc inspection

        public DbSet<QcInspectionHeader> QcInspectionHeaders { get; set; }
        public DbSet<QcInspectionLine> QcInspectionLines { get; set; }

        public DbSet<QcInspectionLineLot> QcInspectionLineLots { get; set; }

        //supplier evaluation
        public DbSet<SupplierPerformanceEvaluation>
    SupplierPerformanceEvaluations
        { get; set; }

        public DbSet<SupplierEvaluationQualityMetric>
            SupplierEvaluationQualityMetrics
        { get; set; }

        public DbSet<SupplierEvaluationDeliveryMetric>
            SupplierEvaluationDeliveryMetrics
        { get; set; }

        public DbSet<SupplierEvaluationCostMetric>
            SupplierEvaluationCostMetrics
        { get; set; }

        public DbSet<SupplierEvaluationReliabilityScore>
            SupplierEvaluationReliabilityScores
        { get; set; }

        public DbSet<SupplierEvaluationWorkflowHistory>
            SupplierEvaluationWorkflowHistory
        { get; set; }

        public DbSet<SupplierPerformanceEvaluationLine>
    SupplierPerformanceEvaluationLines
        { get; set; }


        public DbSet<IncomingReceiving> IncomingReceivings { get; set; }

        public DbSet<IncomingReceivingLine> IncomingReceivingLines { get; set; }

        public DbSet<IncomingReceivingInspection> IncomingReceivingInspections { get; set; }

        public DbSet<ReceivingTrackingHistory> ReceivingTrackingHistories { get; set; }



        public DbSet<QuarantineHeader> QuarantineHeaders { get; set; }
        public DbSet<QuarantineLine> QuarantineLines { get; set; }


        public DbSet<RmwProcessingHeader> RmwProcessingHeaders { get; set; }
        public DbSet<RmwProcessingLine> RmwProcessingLines { get; set; }

        public DbSet<FinalReceivingHeader> FinalReceivingHeaders { get; set; }
        public DbSet<FinalReceivingLine> FinalReceivingLines { get; set; }


        public DbSet<SystemAccessPoint>
    SystemAccessPoints
        { get; set; }

        public DbSet<SystemUserAccessPoint>
            SystemUserAccessPoints
        { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names (match your MySQL tables)
            ConfigureSupplierPerformanceEvaluation(modelBuilder);
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



            // ============================================================
            // INCOMING RECEIVING / WAREHOUSE PRE-RECEIVING
            // ============================================================

            modelBuilder.Entity<IncomingReceiving>(entity =>
            {
                entity.ToTable("purchasing_incoming_receiving");

                entity.HasKey(e => e.IncomingReceivingId);

                entity.Property(e => e.IncomingReceivingId)
                    .HasColumnName("incoming_receiving_id");

                entity.Property(e => e.IncomingNo)
                    .HasColumnName("incoming_no")
                    .HasMaxLength(30);

                entity.Property(e => e.PoId)
                    .HasColumnName("po_id");

                entity.Property(e => e.ScheduleId)
                    .HasColumnName("schedule_id");

                entity.Property(e => e.SupplierId)
                    .HasColumnName("supplier_id");

                entity.Property(e => e.BranchId)
                    .HasColumnName("branch_id")
                    .HasMaxLength(50);

                entity.Property(e => e.DeliveryDate)
                    .HasColumnName("delivery_date");

                entity.Property(e => e.SiDrNo)
                    .HasColumnName("si_dr_no")
                    .HasMaxLength(100);

                entity.Property(e => e.ReceivingStatus)
                    .HasColumnName("receiving_status")
                    .HasMaxLength(30);

                entity.Property(e => e.DocumentsComplete)
                    .HasColumnName("documents_complete");

                entity.Property(e => e.MissingDocuments)
                    .HasColumnName("missing_documents")
                    .HasMaxLength(500);

                entity.Property(e => e.ReceivingRemarks)
                    .HasColumnName("receiving_remarks")
                    .HasMaxLength(1000);

                entity.Property(e => e.VerifiedBy)
    .HasColumnName("verified_by")
    .HasMaxLength(100);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by")
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedBy)
                    .HasColumnName("updated_by")
                    .HasMaxLength(100);

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasIndex(e => e.IncomingNo)
                    .IsUnique();

                entity.HasMany(e => e.Lines)
                    .WithOne(e => e.IncomingReceiving)
                    .HasForeignKey(e => e.IncomingReceivingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Inspection)
                    .WithOne(e => e.IncomingReceiving)
                    .HasForeignKey<IncomingReceivingInspection>(
                        e => e.IncomingReceivingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // ============================================================
            // INCOMING RECEIVING LINES
            // ============================================================

            modelBuilder.Entity<IncomingReceivingLine>(entity =>
            {
                entity.ToTable("purchasing_incoming_receiving_line");

                entity.HasKey(e => e.IncomingReceivingLineId);

                entity.Property(e => e.IncomingReceivingLineId)
                    .HasColumnName("incoming_receiving_line_id");

                entity.Property(e => e.IncomingReceivingId)
                    .HasColumnName("incoming_receiving_id");

                entity.Property(e => e.PoLineId)
                    .HasColumnName("po_line_id");

                entity.Property(e => e.MaterialId)
                    .HasColumnName("material_id");

                entity.Property(e => e.ScheduledQty)
                    .HasColumnName("scheduled_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.DeliveredQty)
                    .HasColumnName("delivered_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.TareWeight)
    .HasColumnName("tare_weight")
    .HasPrecision(18, 4);

                entity.Property(e => e.Uom)
                    .HasColumnName("uom")
                    .HasMaxLength(50);

                entity.Property(e => e.PackagingOk)
                    .HasColumnName("packaging_ok");

                entity.Property(e => e.ContaminationOk)
                    .HasColumnName("contamination_ok");

                entity.Property(e => e.LabelingOk)
                    .HasColumnName("labeling_ok");

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.HasIndex(e => e.IncomingReceivingId);

                entity.HasIndex(e => e.PoLineId);
            });


            // ============================================================
            // INCOMING RECEIVING LINE LOTS
            // Material Receiving Form lot / manufacturer information
            // ============================================================

            modelBuilder.Entity<IncomingReceivingLineLot>(entity =>
            {
                entity.ToTable(
                    "purchasing_incoming_receiving_line_lot");

                entity.HasKey(e =>
                    e.IncomingReceivingLineLotId);

                entity.Property(e =>
                        e.IncomingReceivingLineLotId)
                    .HasColumnName(
                        "incoming_receiving_line_lot_id")
                    .ValueGeneratedOnAdd();

                entity.Property(e =>
                        e.IncomingReceivingLineId)
                    .HasColumnName(
                        "incoming_receiving_line_id");

                entity.Property(e =>
                        e.ManufacturerId)
                    .HasColumnName(
                        "manufacturer_id");

                entity.Property(e =>
                        e.LotNo)
                    .HasColumnName("lot_no")
                    .HasMaxLength(100);

                entity.Property(e =>
                        e.ManufacturingDate)
                    .HasColumnName(
                        "manufacturing_date");

                entity.Property(e =>
                        e.ExpirationDate)
                    .HasColumnName(
                        "expiration_date");

                entity.Property(e =>
                        e.ItemCount)
                    .HasColumnName("item_count")
                    .HasPrecision(18, 4);

                entity.Property(e =>
                        e.Weight)
                    .HasColumnName("weight")
                    .HasPrecision(18, 4);

                entity.Property(e =>
                        e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(1000);

                entity.Property(e =>
                        e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e =>
                        e.UpdatedAt)
                    .HasColumnName("updated_at");


                // --------------------------------------------------------
                // INDEXES
                // --------------------------------------------------------

                entity.HasIndex(e =>
                    e.IncomingReceivingLineId);

                entity.HasIndex(e =>
                    e.ManufacturerId);

                entity.HasIndex(e =>
                    e.LotNo);


                // --------------------------------------------------------
                // RELATIONSHIP
                // IncomingReceivingLine -> Lots
                // --------------------------------------------------------

                entity.HasOne(e =>
                        e.IncomingReceivingLine)
                    .WithMany(e =>
                        e.Lots)
                    .HasForeignKey(e =>
                        e.IncomingReceivingLineId)
                    .OnDelete(DeleteBehavior.Cascade);
            });



            // ============================================================
            // QUARANTINE HEADER
            // ============================================================

            modelBuilder.Entity<QuarantineHeader>(entity =>
            {
                entity.ToTable("purchasing_quarantine_header");

                entity.HasKey(e => e.QuarantineId);

                entity.Property(e => e.QuarantineId)
                    .HasColumnName("quarantine_id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.QuarantineNo)
                    .HasColumnName("quarantine_no");

                entity.Property(e => e.IncomingReceivingId)
                    .HasColumnName("incoming_receiving_id");

                entity.Property(e => e.QaReceivingId)
                    .HasColumnName("qa_receiving_id");

                entity.Property(e => e.QcId)
                    .HasColumnName("qc_id");

                entity.Property(e => e.PoId)
                    .HasColumnName("po_id");

                entity.Property(e => e.SupplierId)
                    .HasColumnName("supplier_id");

                entity.Property(e => e.Status)
                    .HasColumnName("status");

                entity.Property(e => e.Decision)
                    .HasColumnName("decision");

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.ReleasedBy)
                    .HasColumnName("released_by");

                entity.Property(e => e.ReleasedAt)
                    .HasColumnName("released_at");

                entity.Property(e => e.UpdatedBy)
                    .HasColumnName("updated_by");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasMany(e => e.Lines)
                    .WithOne(e => e.Header)
                    .HasForeignKey(e => e.QuarantineId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // ============================================================
            // QUARANTINE LINE
            // ============================================================

            modelBuilder.Entity<QuarantineLine>(entity =>
            {
                entity.ToTable("purchasing_quarantine_line");

                entity.HasKey(e => e.QuarantineLineId);

                entity.Property(e => e.QuarantineLineId)
                    .HasColumnName("quarantine_line_id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.QuarantineId)
                    .HasColumnName("quarantine_id");

                entity.Property(e => e.QaReceivingLineId)
                    .HasColumnName("qa_receiving_line_id");

                entity.Property(e => e.IncomingReceivingLineId)
                    .HasColumnName("incoming_receiving_line_id");

                entity.Property(e => e.IncomingReceivingLineLotId)
                    .HasColumnName("incoming_receiving_line_lot_id");

                entity.Property(e => e.QcLineId)
                    .HasColumnName("qc_line_id");

                entity.Property(e => e.QcLineLotId)
                    .HasColumnName("qc_line_lot_id");

                entity.Property(e => e.PoLineId)
                    .HasColumnName("po_line_id");

                entity.Property(e => e.MaterialId)
                    .HasColumnName("material_id");

                entity.Property(e => e.LotNo)
                    .HasColumnName("lot_no");

                entity.Property(e => e.ManufacturingDate)
                    .HasColumnName("manufacturing_date");

                entity.Property(e => e.ExpirationDate)
                    .HasColumnName("expiration_date");

                entity.Property(e => e.QcReceivedQty)
                    .HasColumnName("qc_received_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.QcAcceptedQty)
                    .HasColumnName("qc_accepted_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.QcRejectedQty)
                    .HasColumnName("qc_rejected_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.Status)
                    .HasColumnName("status");

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.HasIndex(e => e.QuarantineId);

                entity.HasIndex(e => e.QaReceivingLineId);

                entity.HasIndex(e => e.IncomingReceivingLineId);

                entity.HasIndex(e => e.IncomingReceivingLineLotId);
            });

            // ============================================================
            // WAREHOUSE ANDROID RECEIVING INSPECTION / CHECKLIST
            // ============================================================

            // ============================================================
            // RMW MATERIAL RECEIVING FORM / CHECKLIST
            // ============================================================

            modelBuilder.Entity<IncomingReceivingInspection>(entity =>
            {
                entity.ToTable(
                    "purchasing_incoming_receiving_inspection");

                entity.HasKey(e =>
                    e.InspectionId);

                entity.Property(e =>
                        e.InspectionId)
                    .HasColumnName("inspection_id");

                entity.Property(e =>
                        e.IncomingReceivingId)
                    .HasColumnName("incoming_receiving_id");


                // ========================================================
                // PO / SUPPLIER
                // ========================================================

                entity.Property(e =>
                        e.PoMatched)
                    .HasColumnName("po_matched");

                entity.Property(e =>
                        e.DeliveryScheduled)
                    .HasColumnName("delivery_scheduled");

                entity.Property(e =>
                        e.ApprovedSupplier)
                    .HasColumnName("approved_supplier");


                // ========================================================
                // DOCUMENTS
                // ========================================================

                entity.Property(e =>
                        e.SalesInvoiceAvailable)
                    .HasColumnName(
                        "sales_invoice_available");

                entity.Property(e =>
                        e.DeliveryReceiptAvailable)
                    .HasColumnName(
                        "delivery_receipt_available");

                entity.Property(e =>
                        e.CoaAvailable)
                    .HasColumnName("coa_available");


                // ========================================================
                // OFFICIAL MATERIAL RECEIVING FORM
                // ========================================================

                entity.Property(e =>
                        e.CorrectQuantityDelivered)
                    .HasColumnName(
                        "correct_quantity_delivered");

                entity.Property(e =>
                        e.TruckDoorLockInPlace)
                    .HasColumnName(
                        "truck_door_lock_in_place");

                entity.Property(e =>
                        e.VehicleClean)
                    .HasColumnName("vehicle_clean");

                entity.Property(e =>
                        e.ContainersCleanAndSealed)
                    .HasColumnName(
                        "containers_clean_and_sealed");

                entity.Property(e =>
                        e.NoVisibleContaminationOrSpoilage)
                    .HasColumnName(
                        "no_visible_contamination_or_spoilage");

                entity.Property(e =>
                        e.LabelsPresentAndLegible)
                    .HasColumnName(
                        "labels_present_and_legible");

                entity.Property(e =>
                        e.DriverIdentityVerified)
                    .HasColumnName(
                        "driver_identity_verified");

                entity.Property(e =>
                        e.NoUnauthorizedAccessDuringUnloading)
                    .HasColumnName(
                        "no_unauthorized_access_during_unloading");

                entity.Property(e =>
                        e.HiddenCompartmentChecked)
                    .HasColumnName(
                        "hidden_compartment_checked");

                entity.Property(e =>
                        e.ReceivedInAuthorizedZone)
                    .HasColumnName(
                        "received_in_authorized_zone");


                // ========================================================
                // INSPECTION INFORMATION
                // ========================================================

                entity.Property(e =>
                        e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(1000);

                entity.Property(e =>
                        e.CheckedBy)
                    .HasColumnName("checked_by")
                    .HasMaxLength(100);

                entity.Property(e =>
                        e.CheckedAt)
                    .HasColumnName("checked_at");


                // One RMW checklist per Incoming Receiving
                entity.HasIndex(e =>
                        e.IncomingReceivingId)
                    .IsUnique();
            });


            // ============================================================
            // RECEIVING TRACKING / TIME & MOTION
            // ============================================================

            modelBuilder.Entity<ReceivingTrackingHistory>(entity =>
            {
                entity.ToTable("purchasing_receiving_tracking_history");

                entity.HasKey(e => e.TrackingId);

                entity.Property(e => e.TrackingId)
                    .HasColumnName("tracking_id");

                entity.Property(e => e.PoId)
                    .HasColumnName("po_id");

                entity.Property(e => e.ScheduleId)
                    .HasColumnName("schedule_id");

                entity.Property(e => e.IncomingReceivingId)
                    .HasColumnName("incoming_receiving_id");

                entity.Property(e => e.EventCode)
                    .HasColumnName("event_code")
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(50);

                entity.Property(e => e.EventDescription)
                    .HasColumnName("event_description")
                    .HasMaxLength(500);

                entity.Property(e => e.PerformedBy)
                    .HasColumnName("performed_by")
                    .HasMaxLength(100);

                entity.Property(e => e.PerformedByRole)
                    .HasColumnName("performed_by_role")
                    .HasMaxLength(100);

                entity.Property(e => e.EventAt)
                    .HasColumnName("event_at");

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(1000);

                entity.HasIndex(e => e.PoId);

                entity.HasIndex(e => e.IncomingReceivingId);

                entity.HasIndex(e => e.EventAt);

                entity.HasOne(e => e.IncomingReceiving)
                    .WithMany()
                    .HasForeignKey(e => e.IncomingReceivingId)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // ============================================================
            // QA/QC RECEIVING INSPECTION
            // ============================================================

            modelBuilder.Entity<QaQcReceivingInspection>(entity =>
            {
                entity.ToTable(
                    "purchasing_qa_qc_receiving_inspection");

                entity.HasKey(e => e.QaReceivingId);

                entity.Property(e => e.QaReceivingId)
                    .HasColumnName("qa_receiving_id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.InspectionNo)
                    .HasColumnName("inspection_no")
                    .HasMaxLength(30);

                entity.Property(e => e.IncomingReceivingId)
                    .HasColumnName("incoming_receiving_id");

                entity.Property(e => e.PoId)
                    .HasColumnName("po_id");

                entity.Property(e => e.SupplierId)
                    .HasColumnName("supplier_id");

                entity.Property(e => e.InspectionDate)
                    .HasColumnName("inspection_date");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(40);

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(1000);

                entity.Property(e => e.InspectedBy)
                    .HasColumnName("inspected_by")
                    .HasMaxLength(100);

                entity.Property(e => e.ReceivedBy)
                    .HasColumnName("received_by")
                    .HasMaxLength(100);

                entity.Property(e => e.NotedBy)
                    .HasColumnName("noted_by")
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by")
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedBy)
                    .HasColumnName("updated_by")
                    .HasMaxLength(100);

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasIndex(e => e.InspectionNo)
                    .IsUnique();

                entity.HasIndex(e => e.IncomingReceivingId)
                    .IsUnique();

                entity.HasMany(e => e.Lines)
                    .WithOne(e => e.Header)
                    .HasForeignKey(e => e.QaReceivingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QaQcReceivingInspectionLine>(entity =>
            {
                entity.ToTable(
                    "purchasing_qa_qc_receiving_inspection_line");

                entity.HasKey(e => e.QaReceivingLineId);

                entity.Property(e => e.QaReceivingLineId)
                    .HasColumnName("qa_receiving_line_id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.QaReceivingId)
                    .HasColumnName("qa_receiving_id");

                entity.Property(e => e.IncomingReceivingLineId)
                    .HasColumnName("incoming_receiving_line_id");

                entity.Property(e => e.IncomingReceivingLineLotId)
                    .HasColumnName("incoming_receiving_line_lot_id");

                entity.Property(e => e.MaterialId)
                    .HasColumnName("material_id");

                entity.Property(e => e.PackagingClean)
                    .HasColumnName("packaging_clean");

                entity.Property(e => e.ReadableLabel)
                    .HasColumnName("readable_label");

                entity.Property(e => e.ProperlySealed)
                    .HasColumnName("properly_sealed");

                entity.Property(e => e.NoDeterioration)
                    .HasColumnName("no_deterioration");

                entity.Property(e => e.NoForeignMatter)
                    .HasColumnName("no_foreign_matter");

                entity.Property(e => e.NoInfestation)
                    .HasColumnName("no_infestation");

                entity.Property(e => e.AppearanceNotApplicable)
                    .HasColumnName("appearance_not_applicable");

                entity.Property(e => e.PowderNoLump)
                    .HasColumnName("powder_no_lump");

                entity.Property(e => e.PowderGoodFlowability)
                    .HasColumnName("powder_good_flowability");

                entity.Property(e => e.LiquidNoSolidification)
                    .HasColumnName("liquid_no_solidification");

                entity.Property(e => e.LiquidNoPrecipitation)
                    .HasColumnName("liquid_no_precipitation");

                entity.Property(e => e.ColorResult)
                    .HasColumnName("color_result")
                    .HasMaxLength(20);

                entity.Property(e => e.ActualColor)
                    .HasColumnName("actual_color")
                    .HasMaxLength(200);

                entity.Property(e => e.OdorResult)
                    .HasColumnName("odor_result")
                    .HasMaxLength(20);

                entity.Property(e => e.ActualOdor)
                    .HasColumnName("actual_odor")
                    .HasMaxLength(200);

                entity.Property(e => e.AssayResultStatus)
                    .HasColumnName("assay_result_status")
                    .HasMaxLength(20);

                entity.Property(e => e.CoaAssayResult)
                    .HasColumnName("coa_assay_result")
                    .HasMaxLength(200);

                entity.Property(e => e.BnpiAssaySpecification)
                    .HasColumnName("bnpi_assay_specification")
                    .HasMaxLength(200);

                entity.Property(e => e.SolubilityResult)
                    .HasColumnName("solubility_result")
                    .HasMaxLength(20);

                entity.Property(e => e.SolubilityTest)
                    .HasColumnName("solubility_test")
                    .HasMaxLength(500);

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(30);

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasIndex(e => e.QaReceivingId);

                entity.HasIndex(e => e.IncomingReceivingLineId);

                entity.HasIndex(e => e.IncomingReceivingLineLotId);

                entity.HasIndex(e => e.MaterialId);
            });






            modelBuilder.Entity<MaterialLotNumber>(entity =>
            {
                entity.ToTable("material_lot_number");
                entity.HasKey(e => e.material_lot_id);

                entity.Property(e => e.quantity)
                    .HasPrecision(18, 4);

                entity.Property(e => e.branch_id)
                    .HasMaxLength(50);

                entity.HasIndex(e => new
                {
                    e.material_id,
                    e.branch_id,
                    e.lot_no
                })
                .IsUnique();
            });

            modelBuilder.Entity<MaterialInventoryTransaction>(entity =>
            {
                entity.ToTable("material_inventory_transactions");
                entity.HasKey(e => e.transaction_id);

                entity.Property(e => e.quantity)
                    .HasPrecision(18, 4);

                entity.Property(e => e.branch_id)
                    .HasMaxLength(50);

                entity.Property(e => e.encoded_by)
                    .HasMaxLength(50);
            });


            // Material Requisition / Raw Material Stock OUT
            modelBuilder.Entity<MaterialRequisition>(entity =>
            {
                entity.ToTable("material_requisition");

                entity.HasKey(e => e.RequisitionId);

                entity.Property(e => e.RequisitionId)
                    .HasColumnName("requisition_id");

                entity.Property(e => e.RequisitionNo)
                    .HasColumnName("requisition_no")
                    .HasMaxLength(50);

                entity.Property(e => e.BranchId)
                    .HasColumnName("branch_id")
                    .HasMaxLength(50);

                entity.Property(e => e.RequisitionDate)
                    .HasColumnName("requisition_date");

                entity.Property(e => e.RequestedBy)
                    .HasColumnName("requested_by")
                    .HasMaxLength(100);

                entity.Property(e => e.ReleasedBy)
                    .HasColumnName("released_by")
                    .HasMaxLength(100);

                entity.Property(e => e.ReceivedBy)
                    .HasColumnName("received_by")
                    .HasMaxLength(100);

                entity.Property(e => e.VerifiedBy)
                    .HasColumnName("verified_by")
                    .HasMaxLength(100);

                entity.Property(e => e.TimeRequested)
                    .HasColumnName("time_requested");

                entity.Property(e => e.TimeServed)
                    .HasColumnName("time_served");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(20);

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by")
                    .HasMaxLength(50);


                entity.Property(e => e.SubmittedBy)
    .HasColumnName("submitted_by")
    .HasMaxLength(50);

                entity.Property(e => e.SubmittedAt)
                    .HasColumnName("submitted_at");

                entity.Property(e => e.ApprovedBy)
                    .HasColumnName("approved_by")
                    .HasMaxLength(50);

                entity.Property(e => e.ApprovedAt)
                    .HasColumnName("approved_at");

                entity.Property(e => e.ApprovalRemarks)
                    .HasColumnName("approval_remarks")
                    .HasMaxLength(500);

                entity.Property(e => e.RejectedBy)
                    .HasColumnName("rejected_by")
                    .HasMaxLength(50);

                entity.Property(e => e.RejectedAt)
                    .HasColumnName("rejected_at");

                entity.Property(e => e.RejectionReason)
                    .HasColumnName("rejection_reason")
                    .HasMaxLength(500);


                entity.Property(e => e.PostedBy)
                    .HasColumnName("posted_by")
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.Property(e => e.PostedAt)
                    .HasColumnName("posted_at");

                entity.HasMany(e => e.Lines)
                    .WithOne(e => e.Requisition)
                    .HasForeignKey(e => e.RequisitionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<MaterialRequisitionLine>(entity =>
            {
                entity.ToTable("material_requisition_line");

                entity.HasKey(e => e.RequisitionLineId);

                entity.Property(e => e.RequisitionLineId)
                    .HasColumnName("requisition_line_id");

                entity.Property(e => e.RequisitionId)
                    .HasColumnName("requisition_id");

                entity.Property(e => e.MaterialId)
                    .HasColumnName("material_id");

                entity.Property(e => e.MaterialLotId)
                    .HasColumnName("material_lot_id");

                entity.Property(e => e.LotNo)
                    .HasColumnName("lot_no")
                    .HasMaxLength(100);

                entity.Property(e => e.RequestedQuantity)
                    .HasColumnName("requested_quantity")
                    .HasPrecision(18, 4);

                entity.Property(e => e.ActualQuantity)
                    .HasColumnName("actual_quantity")
                    .HasPrecision(18, 4);

                entity.Property(e => e.Uom)
                    .HasColumnName("uom")
                    .HasMaxLength(50);

                entity.Property(e => e.ExpirationDate)
                    .HasColumnName("expiration_date");

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");
            });



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
                entity.Property(e => e.BranchId)
    .HasColumnName("branch_id")
    .HasMaxLength(50);
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

                entity.Property(e => e.IncomingReceivingId)
    .HasColumnName("incoming_receiving_id");

                entity.Property(e => e.IncomingNo)
                    .HasColumnName("incoming_no")
                    .HasMaxLength(30);

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

                entity.Property(e => e.IncomingReceivingLineId)
    .HasColumnName("incoming_receiving_line_id");

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


        private static void ConfigureSupplierPerformanceEvaluation(
    ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SupplierPerformanceEvaluation>(entity =>
            {
                entity.ToTable("supplier_performance_evaluations");

                entity.HasKey(x => x.EvaluationId);

                entity.HasIndex(x => x.EvaluationNo)
                    .IsUnique()
                    .HasDatabaseName("uq_supplier_evaluation_no");

                entity.HasIndex(x => x.RrId)
                    .IsUnique()
                    .HasDatabaseName("uq_supplier_evaluation_rr");

                entity.HasIndex(x => x.QcId)
                    .IsUnique()
                    .HasDatabaseName("uq_supplier_evaluation_qc");

                entity.HasIndex(x => x.SupplierId)
                    .HasDatabaseName(
                        "ix_supplier_performance_evaluation_supplier_id");

                entity.HasIndex(x => x.PoId)
                    .HasDatabaseName("ix_supplier_evaluation_po");

                entity.HasIndex(x => x.ScheduleId)
                    .HasDatabaseName("ix_supplier_evaluation_schedule");

                entity.HasIndex(x => x.EvaluationDate)
                    .HasDatabaseName("ix_supplier_evaluation_date");

                entity.HasIndex(x => x.Status)
                    .HasDatabaseName("ix_supplier_evaluation_status");

                entity.Property(x => x.Status)
                    .HasMaxLength(40);

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasMany(x => x.Lines)
                    .WithOne(x => x.Evaluation)
                    .HasForeignKey(x => x.EvaluationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.WorkflowHistory)
                    .WithOne(x => x.Evaluation)
                    .HasForeignKey(x => x.EvaluationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Models.Purchasing.Suppliers.Supplier>()
                    .WithMany()
                    .HasForeignKey(x => x.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Models.Purchasing.PurchaseOrders.PurchaseOrderHeader>()
                    .WithMany()
                    .HasForeignKey(x => x.PoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Models.Purchasing.PurchaseOrders.PurchaseOrderDeliverySchedule>()
                    .WithMany()
                    .HasForeignKey(x => x.ScheduleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Models.Purchasing.ReceivingReports.ReceivingReportHeader>()
                    .WithMany()
                    .HasForeignKey(x => x.RrId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Models.Purchasing.QcInspections.QcInspectionHeader>()
                    .WithMany()
                    .HasForeignKey(x => x.QcId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SupplierPerformanceEvaluationLine>(entity =>
            {
                entity.ToTable("supplier_performance_evaluation_lines");

                entity.HasKey(x => x.EvaluationLineId);

                entity.HasIndex(x => x.QcLineId)
                    .IsUnique()
                    .HasDatabaseName("uq_spe_line_qc_line");

                entity.HasIndex(x => x.EvaluationId)
                    .HasDatabaseName("ix_spe_line_evaluation");

                entity.HasIndex(x => x.MaterialId)
                    .HasDatabaseName("ix_spe_line_material");

                entity.HasIndex(x => x.PoLineId)
                    .HasDatabaseName("ix_spe_line_po_line");

                entity.HasIndex(x => x.RrLineId)
                    .HasDatabaseName("ix_spe_line_rr_line");

                entity.HasIndex(x => x.ScheduleLineId)
                    .HasDatabaseName("ix_spe_line_schedule_line");

                entity.Property(x => x.ApprovedQty)
                    .HasPrecision(18, 4);

                entity.Property(x => x.RejectedQty)
                    .HasPrecision(18, 4);

                entity.Property(x => x.TotalInspectedQty)
                    .HasPrecision(18, 4);

                entity.Property(x => x.QualityScore)
                    .HasPrecision(6, 2);

                entity.Property(x => x.QualityGrade)
                    .HasPrecision(6, 2);

                entity.Property(x => x.OnTimeScore)
                    .HasPrecision(6, 2);

                entity.Property(x => x.ScheduledQty)
                    .HasPrecision(18, 4);

                entity.Property(x => x.DeliveredQty)
                    .HasPrecision(18, 4);

                entity.Property(x => x.InFullScore)
                    .HasPrecision(6, 2);

                entity.Property(x => x.DeliveryScore)
                    .HasPrecision(6, 2);

                entity.Property(x => x.DeliveryGrade)
                    .HasPrecision(6, 2);

                entity.Property(x => x.NewUnitPrice)
                    .HasPrecision(18, 4);

                entity.Property(x => x.PreviousUnitPrice)
                    .HasPrecision(18, 4);

                entity.Property(x => x.PriceChangePercent)
                    .HasPrecision(10, 4);

                entity.Property(x => x.CostScore)
                    .HasPrecision(6, 2);

                entity.Property(x => x.CostGrade)
                    .HasPrecision(6, 2);

                entity.Property(x => x.CoaPoints)
                    .HasPrecision(6, 2);

                entity.Property(x => x.TermsPoints)
                    .HasPrecision(6, 2);

                entity.Property(x => x.OtherPoints)
                    .HasPrecision(6, 2);

                entity.Property(x => x.ReliabilityScore)
                    .HasPrecision(6, 2);

                entity.Property(x => x.ReliabilityGrade)
                    .HasPrecision(6, 2);

                entity.Property(x => x.TotalGrade)
                    .HasPrecision(6, 2);

                entity.Property(x => x.CostStatus)
                    .HasMaxLength(40)
                    .HasDefaultValue("NO_PREVIOUS_PRICE");

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne<Models.Purchasing.QcInspections.QcInspectionLine>()
                    .WithMany()
                    .HasForeignKey(x => x.QcLineId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Models.Purchasing.ReceivingReports.ReceivingReportLine>()
                    .WithMany()
                    .HasForeignKey(x => x.RrLineId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Models.Purchasing.PurchaseOrders.PurchaseOrderLine>()
                    .WithMany()
                    .HasForeignKey(x => x.PoLineId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Models.Purchasing.PurchaseOrders.PurchaseOrderDeliveryScheduleLine>()
                    .WithMany()
                    .HasForeignKey(x => x.ScheduleLineId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SupplierEvaluationWorkflowHistory>(entity =>
            {
                entity.ToTable("supplier_evaluation_workflow_history");

                entity.HasKey(x => x.HistoryId);

                entity.HasIndex(x => x.EvaluationId)
                    .HasDatabaseName("ix_supplier_evaluation_history");

                entity.HasIndex(x => new
                {
                    x.EvaluationId,
                    x.ActionAt
                })
                .HasDatabaseName("ix_supplier_evaluation_history_date");

                entity.Property(x => x.ActionAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });



            // ============================================================
            // RAW MATERIAL PROCESSING
            // ============================================================

            modelBuilder.Entity<RmwProcessingHeader>(entity =>
            {
                entity.ToTable("purchasing_rmw_processing_header");

                entity.HasKey(e => e.ProcessingId);

                entity.Property(e => e.ProcessingId)
                    .HasColumnName("processing_id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ProcessingNo)
                    .HasColumnName("processing_no")
                    .HasMaxLength(30);

                entity.Property(e => e.QuarantineId)
                    .HasColumnName("quarantine_id");

                entity.Property(e => e.QcId)
                    .HasColumnName("qc_id");

                entity.Property(e => e.IncomingReceivingId)
                    .HasColumnName("incoming_receiving_id");

                entity.Property(e => e.PoId)
                    .HasColumnName("po_id");

                entity.Property(e => e.SupplierId)
                    .HasColumnName("supplier_id");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(40);

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by")
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedBy)
                    .HasColumnName("updated_by")
                    .HasMaxLength(100);

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.Property(e => e.CompletedBy)
                    .HasColumnName("completed_by")
                    .HasMaxLength(100);

                entity.Property(e => e.CompletedAt)
                    .HasColumnName("completed_at");

                entity.HasIndex(e => e.ProcessingNo)
                    .IsUnique();

                entity.HasIndex(e => e.QuarantineId)
                    .IsUnique();

                entity.HasIndex(e => e.Status);

                entity.HasMany(e => e.Lines)
                    .WithOne(e => e.Header)
                    .HasForeignKey(e => e.ProcessingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<RmwProcessingLine>(entity =>
            {
                entity.ToTable("purchasing_rmw_processing_line");

                entity.HasKey(e => e.ProcessingLineId);

                entity.Property(e => e.ProcessingLineId)
                    .HasColumnName("processing_line_id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ProcessingId)
                    .HasColumnName("processing_id");

                entity.Property(e => e.QuarantineLineId)
                    .HasColumnName("quarantine_line_id");

                entity.Property(e => e.QcLineId)
                    .HasColumnName("qc_line_id");

                entity.Property(e => e.QcLineLotId)
                    .HasColumnName("qc_line_lot_id");

                entity.Property(e => e.IncomingReceivingLineId)
                    .HasColumnName("incoming_receiving_line_id");

                entity.Property(e => e.PoLineId)
                    .HasColumnName("po_line_id");

                entity.Property(e => e.MaterialId)
                    .HasColumnName("material_id");

                entity.Property(e => e.LotNo)
                    .HasColumnName("lot_no")
                    .HasMaxLength(100);

                entity.Property(e => e.ManufacturingDate)
                    .HasColumnName("manufacturing_date");

                entity.Property(e => e.ExpirationDate)
                    .HasColumnName("expiration_date");

                entity.Property(e => e.QaAcceptedQty)
                    .HasColumnName("qa_accepted_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.ActualQty)
                    .HasColumnName("actual_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.VarianceQty)
                    .HasColumnName("variance_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.Uom)
                    .HasColumnName("uom")
                    .HasMaxLength(30);

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(40);

                entity.Property(e => e.WeighingStartedBy)
                    .HasColumnName("weighing_started_by")
                    .HasMaxLength(100);

                entity.Property(e => e.WeighingStartedAt)
                    .HasColumnName("weighing_started_at");

                entity.Property(e => e.WeighingCompletedBy)
                    .HasColumnName("weighing_completed_by")
                    .HasMaxLength(100);

                entity.Property(e => e.WeighingCompletedAt)
                    .HasColumnName("weighing_completed_at");

                entity.Property(e => e.StickerCompletedBy)
                    .HasColumnName("sticker_completed_by")
                    .HasMaxLength(100);

                entity.Property(e => e.StickerCompletedAt)
                    .HasColumnName("sticker_completed_at");

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasIndex(e => e.QuarantineLineId)
                    .IsUnique();

                entity.HasIndex(e => e.ProcessingId);

                entity.HasIndex(e => e.MaterialId);

                entity.HasIndex(e => e.Status);
            });


            // ============================================================
            // FINAL RECEIVING REPORT
            // ============================================================

            modelBuilder.Entity<FinalReceivingHeader>(entity =>
            {
                entity.ToTable("purchasing_final_receiving_header");

                entity.HasKey(e => e.FinalRrId);

                entity.Property(e => e.FinalRrId)
                    .HasColumnName("final_rr_id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.FinalRrNo)
                    .HasColumnName("final_rr_no")
                    .HasMaxLength(30);

                entity.Property(e => e.ProcessingId)
                    .HasColumnName("processing_id");

                entity.Property(e => e.QuarantineId)
                    .HasColumnName("quarantine_id");

                entity.Property(e => e.QcId)
                    .HasColumnName("qc_id");

                entity.Property(e => e.IncomingReceivingId)
                    .HasColumnName("incoming_receiving_id");

                entity.Property(e => e.PoId)
                    .HasColumnName("po_id");

                entity.Property(e => e.SupplierId)
                    .HasColumnName("supplier_id");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(30);

                entity.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by")
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.CommittedBy)
                    .HasColumnName("committed_by")
                    .HasMaxLength(100);

                entity.Property(e => e.CommittedAt)
                    .HasColumnName("committed_at");

                entity.HasIndex(e => e.FinalRrNo)
                    .IsUnique();

                entity.HasIndex(e => e.ProcessingId)
                    .IsUnique();

                entity.HasIndex(e => e.QuarantineId);

                entity.HasIndex(e => e.Status);

                entity.HasMany(e => e.Lines)
                    .WithOne(e => e.Header)
                    .HasForeignKey(e => e.FinalRrId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<FinalReceivingLine>(entity =>
            {
                entity.ToTable("purchasing_final_receiving_line");

                entity.HasKey(e => e.FinalRrLineId);

                entity.Property(e => e.FinalRrLineId)
                    .HasColumnName("final_rr_line_id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.FinalRrId)
                    .HasColumnName("final_rr_id");

                entity.Property(e => e.ProcessingLineId)
                    .HasColumnName("processing_line_id");

                entity.Property(e => e.QuarantineLineId)
                    .HasColumnName("quarantine_line_id");

                entity.Property(e => e.MaterialId)
                    .HasColumnName("material_id");

                entity.Property(e => e.LotNo)
                    .HasColumnName("lot_no")
                    .HasMaxLength(100);

                entity.Property(e => e.ManufacturingDate)
                    .HasColumnName("manufacturing_date");

                entity.Property(e => e.ExpirationDate)
                    .HasColumnName("expiration_date");

                entity.Property(e => e.QaAcceptedQty)
                    .HasColumnName("qa_accepted_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.ActualQty)
                    .HasColumnName("actual_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.VarianceQty)
                    .HasColumnName("variance_qty")
                    .HasPrecision(18, 4);

                entity.Property(e => e.Uom)
                    .HasColumnName("uom")
                    .HasMaxLength(30);

                entity.HasIndex(e => e.ProcessingLineId)
                    .IsUnique();

                entity.HasIndex(e => e.FinalRrId);

                entity.HasIndex(e => e.MaterialId);
            });


            modelBuilder.Entity<SystemAccessPoint>(entity =>
            {
                entity.ToTable("system_access_point");

                entity.HasKey(x =>
                    x.access_point_id);

                entity.Property(x =>
                    x.access_code)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x =>
                    x.access_name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x =>
                    x.module_name)
                    .HasMaxLength(100)
                    .IsRequired();
            });
            modelBuilder.Entity<SystemUserAccessPoint>(entity =>
            {
                entity.ToTable("system_user_access_point");

                entity.HasKey(x =>
                    x.user_access_id);

                entity.HasOne(x =>
                        x.AccessPoint)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.access_point_id)
                    .OnDelete(DeleteBehavior.Cascade);
            });



        }

    }
}