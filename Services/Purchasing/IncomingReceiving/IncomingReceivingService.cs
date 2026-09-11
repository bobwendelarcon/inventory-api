using inventory_api.Data;
using inventory_api.DTOs.Purchasing.IncomingReceiving;
using inventory_api.Models.Purchasing.Receiving;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.IncomingReceiving
{
    public class IncomingReceivingService
    {
        private readonly AppDbContext _db;

        public IncomingReceivingService(AppDbContext db)
        {
            _db = db;
        }


        // ============================================================
        // GENERATE INCOMING RECEIVING NUMBER
        // IR-2026-0001
        // ============================================================
        public async Task<string> GenerateIncomingNoAsync()
        {
            var year = DateTime.Now.Year;

            var prefix = $"IR-{year}-";

            var lastNo = await _db.IncomingReceivings
                .Where(x => x.IncomingNo.StartsWith(prefix))
                .OrderByDescending(x => x.IncomingReceivingId)
                .Select(x => x.IncomingNo)
                .FirstOrDefaultAsync();

            var nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var lastPart = lastNo.Split('-').LastOrDefault();

                if (int.TryParse(lastPart, out var parsed))
                {
                    nextNumber = parsed + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }


        // ============================================================
        // CREATE INCOMING RECEIVING
        // ============================================================
        public async Task<int> CreateAsync(
            CreateIncomingReceivingDto dto,
            string userId,
            string? userRole = null)
        {
            if (dto.ScheduleId <= 0)
                throw new Exception("Delivery schedule is required.");

            if (dto.Lines == null || dto.Lines.Count == 0)
                throw new Exception("At least one delivered material is required.");

            var schedule = await _db.PurchaseOrderDeliverySchedules
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.ScheduleId == dto.ScheduleId);

            if (schedule == null)
                throw new Exception("Delivery schedule not found.");

            if (schedule.Status == "CANCELLED")
                throw new Exception("Cancelled delivery schedule cannot be received.");

            if (schedule.Status == "COMPLETED" ||
    schedule.Status == "FOR_QC" ||
    schedule.Status == "RECEIVED")
            {
                throw new Exception(
                    "Delivery schedule has already been fully received."
                );
            }

            var po = await _db.PurchaseOrderHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.PoId == schedule.PoId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            if (po.Status != "APPROVED" &&
                po.Status != "PARTIALLY_RECEIVED")
            {
                throw new Exception(
                    $"PO must be APPROVED or PARTIALLY_RECEIVED. Current status: {po.Status}");
            }

            var now = DateTime.Now;

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                // ----------------------------------------------------
                // Determine receiving result
                // ----------------------------------------------------

                var result = DetermineReceivingStatus(
                    dto.Inspection,
                    dto.Lines);

                var documentsComplete =
                    dto.Inspection.SalesInvoiceAvailable &&
                    dto.Inspection.DeliveryReceiptAvailable &&
                    dto.Inspection.CoaAvailable;

                var missingDocuments =
                    BuildMissingDocuments(dto.Inspection);


                // ----------------------------------------------------
                // Create Incoming Receiving Header
                // ----------------------------------------------------

                var incoming = new Models.Purchasing.Receiving.IncomingReceiving
                {
                    IncomingNo = await GenerateIncomingNoAsync(),

                    PoId = po.PoId,
                    ScheduleId = schedule.ScheduleId,
                    SupplierId = po.SupplierId,

                    BranchId = dto.BranchId,

                    DeliveryDate =
                        dto.DeliveryDate == default
                            ? now
                            : dto.DeliveryDate,

                    SiDrNo = dto.SiDrNo,

                    ReceivingStatus = result,

                    DocumentsComplete = documentsComplete,

                    MissingDocuments = missingDocuments,

                    ReceivingRemarks = dto.Remarks,

                    // Material Receiving Form
                    // CreatedBy = Received By
                    VerifiedBy = dto.VerifiedBy,

                    CreatedBy = userId,
                    CreatedAt = now
                };

                _db.IncomingReceivings.Add(incoming);

                await _db.SaveChangesAsync();


                // ----------------------------------------------------
                // Save Delivered Materials
                // ----------------------------------------------------

                foreach (var dtoLine in dto.Lines)
                {
                    if (dtoLine.DeliveredQty <= 0)
                        continue;

                    var poLine = po.Lines
                        .FirstOrDefault(x =>
                            x.PoLineId == dtoLine.PoLineId);

                    if (poLine == null)
                    {
                        throw new Exception(
                            $"PO line {dtoLine.PoLineId} was not found.");
                    }

                    var scheduleLine = schedule.Lines
                        .FirstOrDefault(x =>
                            x.PoLineId == dtoLine.PoLineId);

                    if (scheduleLine == null)
                    {
                        throw new Exception(
                            $"Material is not included in delivery schedule. " +
                            $"PO Line ID: {dtoLine.PoLineId}");
                    }


                    // ============================================================
                    // CHECK QUANTITY ALREADY RECORDED THROUGH INCOMING RECEIVING
                    // ============================================================

                    var alreadyDelivered =
                        await _db.IncomingReceivingLines
                            .Where(x =>
                                x.PoLineId == dtoLine.PoLineId &&
                                x.IncomingReceiving!.ScheduleId ==
                                    schedule.ScheduleId &&
                                x.IncomingReceiving.ReceivingStatus !=
                                    "NOT_ACCEPTED")
                            .SumAsync(x => (decimal?)x.DeliveredQty)
                        ?? 0m;


                    var remainingQty =
                        scheduleLine.ScheduledQty -
                        alreadyDelivered;


                    if (remainingQty <= 0)
                    {
                        throw new Exception(
                            $"PO Line {dtoLine.PoLineId} has already been " +
                            $"fully delivered for this schedule.");
                    }


                    // ============================================================
                    // ALLOW PHYSICAL OVER-DELIVERY
                    // ============================================================

                    // Quantity that belongs to the scheduled balance
                    var appliedToScheduleQty =
                        Math.Min(
                            dtoLine.DeliveredQty,
                            remainingQty
                        );

                    // Physical quantity received above the scheduled balance
                    var overDeliveredQty =
                        Math.Max(
                            0m,
                            dtoLine.DeliveredQty - remainingQty
                        );

                    // Do NOT reject over-delivery here.
                    // Warehouse records the ACTUAL physical quantity received.
                    // QA/QC will inspect the full physical quantity.


                    // ============================================================
                    // SAVE INCOMING DELIVERY LINE
                    // ============================================================

                    if (dtoLine.TareWeight.HasValue &&
    dtoLine.TareWeight.Value < 0)
                    {
                        throw new Exception(
                            $"Tare weight cannot be negative for PO Line " +
                            $"{dtoLine.PoLineId}.");
                    }

                    var line = new IncomingReceivingLine
                    {
                        IncomingReceivingId =
                            incoming.IncomingReceivingId,

                        PoLineId =
                            poLine.PoLineId,

                        MaterialId =
                            poLine.MaterialId,

                        ScheduledQty =
                            scheduleLine.ScheduledQty,

                        DeliveredQty =
                            dtoLine.DeliveredQty,

                        TareWeight =
    dtoLine.TareWeight,

                        Uom =
                            poLine.Uom,

                        PackagingOk =
                            dtoLine.PackagingOk,

                        ContaminationOk =
                            dtoLine.ContaminationOk,

                        LabelingOk =
                            dtoLine.LabelingOk,

                        Remarks =
                            dtoLine.Remarks,

                        CreatedAt =
                            now
                    };

                    _db.IncomingReceivingLines.Add(line);

                    await _db.SaveChangesAsync();


                    // ============================================================
                    // SAVE MATERIAL RECEIVING LOTS
                    // ============================================================

                    if (dtoLine.Lots != null &&
                        dtoLine.Lots.Count > 0)
                    {
                        foreach (var dtoLot in dtoLine.Lots)
                        {
                            // ----------------------------------------------------
                            // VALIDATE DATES
                            // ----------------------------------------------------

                            if (dtoLot.ManufacturingDate.HasValue &&
                                dtoLot.ExpirationDate.HasValue &&
                                dtoLot.ExpirationDate.Value.Date <
                                dtoLot.ManufacturingDate.Value.Date)
                            {
                                throw new Exception(
                                    $"Expiration date cannot be earlier than " +
                                    $"manufacturing date for PO Line {dtoLine.PoLineId}.");
                            }


                            // ----------------------------------------------------
                            // VALIDATE NUMERIC VALUES
                            // ----------------------------------------------------

                            if (dtoLot.ItemCount.HasValue &&
                                dtoLot.ItemCount.Value < 0)
                            {
                                throw new Exception(
                                    "Item/container count cannot be negative.");
                            }

                            if (dtoLot.Weight.HasValue &&
                                dtoLot.Weight.Value < 0)
                            {
                                throw new Exception(
                                    "Lot weight cannot be negative.");
                            }


                            // ----------------------------------------------------
                            // VALIDATE MANUFACTURER
                            // Manufacturer must belong to this supplier.
                            // ----------------------------------------------------

                            if (dtoLot.ManufacturerId.HasValue)
                            {
                                var manufacturerId =
                                    dtoLot.ManufacturerId.Value;

                                var manufacturerValid =
     await _db.SupplierMaterials
         .AnyAsync(x =>
             x.SupplierId == po.SupplierId &&
             x.MaterialId == poLine.MaterialId &&
             x.ManufacturerId == manufacturerId &&
             x.IsActive &&
             !x.IsDeleted);

                                if (!manufacturerValid)
                                {
                                    throw new Exception(
                                        $"Manufacturer ID {manufacturerId} is not an " +
                                        $"active manufacturer for this supplier/material.");
                                }
                            }


                            // ----------------------------------------------------
                            // SAVE LOT
                            // ----------------------------------------------------

                            var lot =
                                new IncomingReceivingLineLot
                                {
                                    IncomingReceivingLineId =
                                        line.IncomingReceivingLineId,

                                    ManufacturerId =
                                        dtoLot.ManufacturerId,

                                    LotNo =
                                        string.IsNullOrWhiteSpace(dtoLot.LotNo)
                                            ? null
                                            : dtoLot.LotNo.Trim(),

                                    ManufacturingDate =
                                        dtoLot.ManufacturingDate,

                                    ExpirationDate =
                                        dtoLot.ExpirationDate,

                                    ItemCount =
                                        dtoLot.ItemCount,

                                    Weight =
                                        dtoLot.Weight,

                                    Remarks =
                                        dtoLot.Remarks,

                                    CreatedAt =
                                        now
                                };

                            _db.IncomingReceivingLineLots.Add(lot);
                        }
                    }

                    // ============================================================
                    // UPDATE DELIVERY SCHEDULE QUANTITY
                    //
                    // Only accepted physical receiving should affect the
                    // delivery schedule.
                    // ============================================================

                    if (result != "NOT_ACCEPTED")
                    {
                        scheduleLine.ReceivedQty +=
                            appliedToScheduleQty;

                        // Never allow the scheduled received quantity
                        // to exceed the scheduled quantity.
                        if (scheduleLine.ReceivedQty >
                            scheduleLine.ScheduledQty)
                        {
                            scheduleLine.ReceivedQty =
                                scheduleLine.ScheduledQty;
                        }

                        scheduleLine.BalanceQty =
                            Math.Max(
                                0m,
                                scheduleLine.ScheduledQty -
                                scheduleLine.ReceivedQty
                            );

                        if (scheduleLine.BalanceQty <= 0)
                        {
                            scheduleLine.Status =
                                "COMPLETED";
                        }
                        else if (scheduleLine.ReceivedQty > 0)
                        {
                            scheduleLine.Status =
                                "PARTIALLY_RECEIVED";
                        }
                        else
                        {
                            scheduleLine.Status =
                                "OPEN";
                        }
                    }
                }



                // ============================================================
                // UPDATE DELIVERY SCHEDULE HEADER
                // ============================================================

                if (result != "NOT_ACCEPTED")
                {
                    var allCompleted =
                        schedule.Lines
                            .Where(x =>
                                x.Status != "CANCELLED")
                            .All(x =>
                                x.BalanceQty <= 0);

                    var hasReceived =
                        schedule.Lines
                            .Where(x =>
                                x.Status != "CANCELLED")
                            .Any(x =>
                                x.ReceivedQty > 0);

                    if (allCompleted)
                    {
                        schedule.Status = "RECEIVED";
                    }
                    else if (hasReceived)
                    {
                        schedule.Status = "PARTIALLY_RECEIVED";
                    }
                    else
                    {
                        schedule.Status = "OPEN";
                    }

                    schedule.UpdatedAt = now;

                    schedule.UpdatedAt =
                        now;
                }




                // ----------------------------------------------------
                // Warehouse Inspection / Android Checklist
                // ----------------------------------------------------

                var inspection =
    new IncomingReceivingInspection
    {
        IncomingReceivingId =
            incoming.IncomingReceivingId,

        // ====================================================
        // PO / SUPPLIER
        // ====================================================

        PoMatched =
            dto.Inspection.PoMatched,

        DeliveryScheduled =
            dto.Inspection.DeliveryScheduled,

        ApprovedSupplier =
            dto.Inspection.ApprovedSupplier,


        // ====================================================
        // DOCUMENTS
        // ====================================================

        SalesInvoiceAvailable =
            dto.Inspection.SalesInvoiceAvailable,

        DeliveryReceiptAvailable =
            dto.Inspection.DeliveryReceiptAvailable,

        CoaAvailable =
            dto.Inspection.CoaAvailable,


        // ====================================================
        // MATERIAL RECEIVING FORM
        // ====================================================

        CorrectQuantityDelivered =
            dto.Inspection.CorrectQuantityDelivered,

        TruckDoorLockInPlace =
            dto.Inspection.TruckDoorLockInPlace,

        VehicleClean =
            dto.Inspection.VehicleClean,

        ContainersCleanAndSealed =
            dto.Inspection.ContainersCleanAndSealed,

        NoVisibleContaminationOrSpoilage =
            dto.Inspection.NoVisibleContaminationOrSpoilage,

        LabelsPresentAndLegible =
            dto.Inspection.LabelsPresentAndLegible,

        DriverIdentityVerified =
            dto.Inspection.DriverIdentityVerified,

        NoUnauthorizedAccessDuringUnloading =
            dto.Inspection.NoUnauthorizedAccessDuringUnloading,

        HiddenCompartmentChecked =
            dto.Inspection.HiddenCompartmentChecked,

        ReceivedInAuthorizedZone =
            dto.Inspection.ReceivedInAuthorizedZone,


        // ====================================================
        // RECEIVING INFORMATION
        // ====================================================

        Remarks =
            dto.Inspection.Remarks,

        CheckedBy =
            userId,

        CheckedAt =
            now
    };

                _db.IncomingReceivingInspections.Add(
                    inspection);


                await _db.SaveChangesAsync();


              


                // ----------------------------------------------------
                // TIME & MOTION / TRACKING HISTORY
                // ----------------------------------------------------




                await AddTrackingAsync(
                    po.PoId,
                    schedule.ScheduleId,
                    incoming.IncomingReceivingId,
                    "WAREHOUSE_CHECK_COMPLETED",
                    result,
                    GetStatusDescription(result),
                    userId,
                    userRole,
                    now,
                    dto.Inspection.Remarks);


                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return incoming.IncomingReceivingId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // ============================================================
        // DETERMINE RMW RECEIVING RESULT
        // ============================================================
        private static string DetermineReceivingStatus(
            IncomingReceivingInspectionDto inspection,
            List<IncomingReceivingLineDto> lines)
        {
            // --------------------------------------------------------
            // CRITICAL RECEIVING FAILURES
            // Delivery should not proceed to QA/QC.
            // --------------------------------------------------------

            if (!inspection.PoMatched)
                return "NOT_ACCEPTED";

            if (!inspection.DeliveryScheduled)
                return "NOT_ACCEPTED";

            if (!inspection.ApprovedSupplier)
                return "NOT_ACCEPTED";

            if (!inspection.CorrectQuantityDelivered)
                return "NOT_ACCEPTED";

            if (!inspection.VehicleClean)
                return "NOT_ACCEPTED";

            if (!inspection.ContainersCleanAndSealed)
                return "NOT_ACCEPTED";

            if (!inspection.NoVisibleContaminationOrSpoilage)
                return "NOT_ACCEPTED";

            if (!inspection.LabelsPresentAndLegible)
                return "NOT_ACCEPTED";

            if (!inspection.ReceivedInAuthorizedZone)
                return "NOT_ACCEPTED";


            // --------------------------------------------------------
            // LINE-LEVEL PHYSICAL CHECK
            // --------------------------------------------------------

            var hasPhysicalProblem = lines.Any(x =>
                !x.PackagingOk ||
                !x.ContaminationOk ||
                !x.LabelingOk);

            if (hasPhysicalProblem)
                return "NOT_ACCEPTED";


            // --------------------------------------------------------
            // DOCUMENT CHECK
            //
            // Delivery is physically received, but cannot proceed
            // normally if required documents are incomplete.
            // --------------------------------------------------------

            var documentsComplete =
                inspection.SalesInvoiceAvailable &&
                inspection.DeliveryReceiptAvailable &&
                inspection.CoaAvailable;

            if (!documentsComplete)
                return "RECEIVING_ON_HOLD";


            // --------------------------------------------------------
            // RMW RECEIVING COMPLETED
            //
            // IMPORTANT:
            // This DOES NOT mean QA/QC already accepted the material.
            // It is waiting for QA/QC receiving inspection.
            // --------------------------------------------------------

            return "FOR_QA_QC_RECEIVING_INSPECTION";
        }


        // ============================================================
        // BUILD MISSING DOCUMENT LIST
        // ============================================================
        private static string? BuildMissingDocuments(
            IncomingReceivingInspectionDto inspection)
        {
            var missing = new List<string>();

            if (!inspection.SalesInvoiceAvailable)
                missing.Add("Sales Invoice");

            if (!inspection.DeliveryReceiptAvailable)
                missing.Add("Delivery Receipt");

            if (!inspection.CoaAvailable)
                missing.Add(
                    "Certificate of Analysis (COA)");

            if (missing.Count == 0)
                return null;

            return string.Join(", ", missing);
        }


        // ============================================================
        // TRACKING HISTORY
        // ============================================================
        private async Task AddTrackingAsync(
            int poId,
            int? scheduleId,
            int? incomingReceivingId,
            string eventCode,
            string status,
            string description,
            string? performedBy,
            string? performedByRole,
            DateTime eventAt,
            string? remarks)
        {
            var history =
                new ReceivingTrackingHistory
                {
                    PoId = poId,

                    ScheduleId = scheduleId,

                    IncomingReceivingId =
                        incomingReceivingId,

                    EventCode = eventCode,

                    Status = status,

                    EventDescription = description,

                    PerformedBy = performedBy,

                    PerformedByRole =
                        performedByRole,

                    EventAt = eventAt,

                    Remarks = remarks
                };

            await _db.ReceivingTrackingHistories
                .AddAsync(history);
        }


        // ============================================================
        // STATUS DESCRIPTION
        // ============================================================
        private static string GetStatusDescription(
            string status)
        {
            return status switch
            {
                "FOR_QA_QC_RECEIVING_INSPECTION" =>
                    "RMW receiving completed. Material forwarded to QA/QC " +
                    "for receiving inspection.",

                "RECEIVING_ON_HOLD" =>
                    "RMW receiving completed but material is on hold " +
                    "pending completion of receiving requirements.",

                "NOT_ACCEPTED" =>
                    "RMW receiving inspection completed. " +
                    "Supplier delivery was not accepted.",

                _ =>
                    "RMW receiving inspection completed."
            };
        }

        public async Task<object?> GetScheduleDetailsAsync(int scheduleId)
        {
            var schedule = await _db.PurchaseOrderDeliverySchedules
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId);

            if (schedule == null)
                return null;

            var po = await _db.PurchaseOrderHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.PoId == schedule.PoId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            var supplierName = await _db.Suppliers
                .Where(x => x.SupplierId == po.SupplierId)
                .Select(x => x.SupplierName)
                .FirstOrDefaultAsync() ?? "";


            // ============================================================
            // MATERIAL INFORMATION
            // ============================================================

            var materialIds = po.Lines
                .Select(x => x.MaterialId)
                .Distinct()
                .ToList();

            var materials = await _db.Materials
         .Where(x => materialIds.Contains(x.material_id))
         .Select(x => new
         {
             MaterialId = x.material_id,
             MaterialCode = x.material_code,
             MaterialName = x.material_name,
             IsLotTracked = x.is_lot_tracked
         })
         .ToDictionaryAsync(x => x.MaterialId);


            // ============================================================
            // QUANTITY ALREADY PHYSICALLY RECEIVED
            //
            // IMPORTANT:
            // We now use Incoming Receiving instead of the old RR
            // ReceivedQty.
            //
            // NOT_ACCEPTED deliveries are NOT counted.
            // ============================================================

            var deliveredByPoLine =
                await _db.IncomingReceivingLines
                    .Where(x =>
                        x.IncomingReceiving != null &&
                        x.IncomingReceiving.ScheduleId == scheduleId &&
                        x.IncomingReceiving.ReceivingStatus != "NOT_ACCEPTED")
                    .GroupBy(x => x.PoLineId)
                    .Select(g => new
                    {
                        PoLineId = g.Key,
                        DeliveredQty = g.Sum(x => x.DeliveredQty)
                    })
                    .ToDictionaryAsync(
                        x => x.PoLineId,
                        x => x.DeliveredQty);


            // ============================================================
            // BUILD SCHEDULE LINES
            // ============================================================

            var lines = schedule.Lines
                .Where(x => x.Status != "CANCELLED")
                .OrderBy(x => x.ScheduleLineId)
                .Select(scheduleLine =>
                {
                    var poLine = po.Lines
                        .FirstOrDefault(x =>
                            x.PoLineId == scheduleLine.PoLineId);

                    if (poLine == null)
                        return null;

                    materials.TryGetValue(
                        poLine.MaterialId,
                        out var material);


                    // How much has already entered Incoming Receiving?
                    var alreadyDelivered =
                        deliveredByPoLine.TryGetValue(
                            poLine.PoLineId,
                            out var delivered)
                                ? delivered
                                : 0m;


                    // Remaining quantity for THIS delivery schedule
                    var remaining =
                        Math.Max(
                            0m,
                            scheduleLine.ScheduledQty -
                            alreadyDelivered);


                    return new
                    {
                        scheduleLine.ScheduleLineId,

                        PoLineId =
                            poLine.PoLineId,

                        MaterialId =
                            poLine.MaterialId,

                        MaterialCode =
                            material?.MaterialCode ?? "",

                        MaterialName =
                            material?.MaterialName ?? "",

                        IsLotTracked =
    material?.IsLotTracked ?? false,

                        PoQty =
                            poLine.PoQty,

                        ScheduledQty =
                            scheduleLine.ScheduledQty,

                        PreviouslyDeliveredQty =
                            alreadyDelivered,

                        RemainingScheduledQty =
                            remaining,

                        Uom =
                            poLine.Uom,

                        ScheduleLineStatus =
                            remaining <= 0
                                ? "DELIVERED"
                                : scheduleLine.Status


                    };
                })
                .Where(x => x != null)
                .ToList();


            // ============================================================
            // CALCULATE OVERALL DELIVERY STATUS
            // ============================================================

            var hasRemaining =
                lines.Any(x =>
                    x != null &&
                    x.RemainingScheduledQty > 0);

            var hasDelivered =
                lines.Any(x =>
                    x != null &&
                    x.PreviouslyDeliveredQty > 0);

            string calculatedScheduleStatus;

            if (!hasRemaining)
            {
                calculatedScheduleStatus =
                    "DELIVERED";
            }
            else if (hasDelivered)
            {
                calculatedScheduleStatus =
                    "PARTIALLY_DELIVERED";
            }
            else
            {
                calculatedScheduleStatus =
                    schedule.Status;
            }


            // ============================================================
            // RESPONSE
            // ============================================================

            return new
            {
                schedule.ScheduleId,
                schedule.ScheduleNo,
                schedule.ScheduledDate,

                ScheduleStatus =
                    calculatedScheduleStatus,

                po.PoId,
                po.PoNo,
                po.PrintedPoNo,

                po.SupplierId,

                SupplierName =
                    supplierName,

                PoStatus =
                    po.Status,

                Lines =
                    lines
            };
        }



        public async Task<object> GetManufacturersAsync(
    int supplierId,
    int materialId)
        {
            var manufacturers =
                await (
                    from sm in _db.SupplierMaterials

                    join m in _db.Manufacturers
                        on sm.ManufacturerId
                        equals m.ManufacturerId

                    where
                        sm.SupplierId == supplierId &&
                        sm.MaterialId == materialId &&
                        sm.IsActive &&
                        !sm.IsDeleted &&
                        m.IsActive &&
                        !m.IsDeleted

                    orderby m.ManufacturerName

                    select new
                    {
                        manufacturerId =
                            m.ManufacturerId,

                        manufacturerName =
                            m.ManufacturerName
                    }
                )
                .Distinct()
                .ToListAsync();

            return manufacturers;
        }



    }
}