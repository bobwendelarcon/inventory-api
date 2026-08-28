using inventory_api.Data;
using inventory_api.DTOs.Purchasing.IncomingReceiving;
using inventory_api.Models.Purchasing.QcInspections;
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
       schedule.Status == "FOR_QC")
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
                        if (result == "ACCEPTED_FOR_QC" ||
                            result == "FOR_QC_ON_HOLD")
                        {
                            schedule.Status = "FOR_QC";
                        }
                        else
                        {
                            schedule.Status = "RECEIVED";
                        }
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

                        PoMatched =
                            dto.Inspection.PoMatched,

                        DeliveryScheduled =
                            dto.Inspection.DeliveryScheduled,

                        ApprovedSupplier =
                            dto.Inspection.ApprovedSupplier,

                        SalesInvoiceAvailable =
                            dto.Inspection.SalesInvoiceAvailable,

                        DeliveryReceiptAvailable =
                            dto.Inspection.DeliveryReceiptAvailable,

                        CoaAvailable =
                            dto.Inspection.CoaAvailable,

                        VehicleClean =
                            dto.Inspection.VehicleClean,

                        VehicleDry =
                            dto.Inspection.VehicleDry,

                        VehicleOdorFree =
                            dto.Inspection.VehicleOdorFree,

                        VehicleResidueFree =
                            dto.Inspection.VehicleResidueFree,

                        MaterialClean =
                            dto.Inspection.MaterialClean,

                        MaterialCoveredOrSealed =
                            dto.Inspection.MaterialCoveredOrSealed,

                        Remarks =
                            dto.Inspection.Remarks,

                        CheckedBy = userId,

                        CheckedAt = now
                    };

                _db.IncomingReceivingInspections.Add(
                    inspection);


                await _db.SaveChangesAsync();


                // ============================================================
                // CREATE QA/QC INSPECTION
                // ============================================================

                if (
                    result == "ACCEPTED_FOR_QC" ||
                    result == "FOR_QC_ON_HOLD"
                )
                {
                    // Prevent accidental duplicate QC
                    var existingQc =
                        await _db.QcInspectionHeaders
                            .AnyAsync(x =>
                                x.IncomingReceivingId ==
                                    incoming.IncomingReceivingId);

                    if (!existingQc)
                    {
                        var qcNo =
                            await GenerateQcNoAsync();

                        var qcStatus =
                            result == "FOR_QC_ON_HOLD"
                                ? "ON_HOLD"
                                : "FOR_INSPECTION";


                        var qc =
                            new QcInspectionHeader
                            {
                                QcNo =
                                    qcNo,

                                IncomingReceivingId =
                                    incoming.IncomingReceivingId,

                                IncomingNo =
                                    incoming.IncomingNo,

                                // Final RR does not exist yet
                                RrId =
                                    null,

                                RrNo =
                                    null,

                                PoId =
                                    po.PoId,

                                PoNo =
                                    po.PoNo,

                                SupplierId =
                                    po.SupplierId,

                                InspectionDate =
                                    null,

                                InspectorId =
                                    null,

                                Status =
                                    qcStatus,

                                Decision =
                                    null,

                                Remarks =
                                    result == "FOR_QC_ON_HOLD"
                                        ? $"Incoming delivery on hold. Missing documents: {missingDocuments}"
                                        : null,

                                CreatedBy =
                                    userId,

                                CreatedAt =
                                    now
                            };


                        // --------------------------------------------------------
                        // CREATE QC LINES FROM INCOMING RECEIVING LINES
                        // --------------------------------------------------------

                        var incomingLines =
                            await _db.IncomingReceivingLines
                                .Where(x =>
                                    x.IncomingReceivingId ==
                                        incoming.IncomingReceivingId)
                                .ToListAsync();


                        foreach (var incomingLine in incomingLines)
                        {
                            qc.Lines.Add(
                                new QcInspectionLine
                                {
                                    IncomingReceivingLineId =
                                        incomingLine.IncomingReceivingLineId,

                                    // Final RR does not exist yet
                                    RrLineId =
                                        null,

                                    PoLineId =
                                        incomingLine.PoLineId,

                                    MaterialId =
                                        incomingLine.MaterialId,

                                    ReceivedQty =
                                        incomingLine.DeliveredQty,

                                    AcceptedQty =
                                        0,

                                    RejectedQty =
                                        0,

                                    Remarks =
                                        incomingLine.Remarks,

                                    Status =
                                        qcStatus == "ON_HOLD"
                                            ? "ON_HOLD"
                                            : "PENDING",

                                    CreatedAt =
                                        now
                                }
                            );
                        }


                        _db.QcInspectionHeaders.Add(qc);


                        await AddTrackingAsync(
                            po.PoId,
                            schedule.ScheduleId,
                            incoming.IncomingReceivingId,

                            qcStatus == "ON_HOLD"
                                ? "QA_QC_ON_HOLD"
                                : "FORWARDED_TO_QA_QC",

                            qcStatus,

                            qcStatus == "ON_HOLD"
                                ? "Incoming delivery forwarded to QA/QC and placed on hold."
                                : "Incoming delivery forwarded to QA/QC for inspection.",

                            userId,
                            userRole,
                            now,
                            missingDocuments
                        );
                    }
                }



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
        // DETERMINE RESULT
        // ============================================================
        private static string DetermineReceivingStatus(
            IncomingReceivingInspectionDto inspection,
            List<IncomingReceivingLineDto> lines)
        {
            /*
             * SERIOUS RECEIVING FAILURES
             *
             * These should NOT proceed to QA/QC.
             */

            if (!inspection.PoMatched)
                return "NOT_ACCEPTED";

            if (!inspection.DeliveryScheduled)
                return "NOT_ACCEPTED";

            if (!inspection.ApprovedSupplier)
                return "NOT_ACCEPTED";

            if (!inspection.VehicleClean ||
                !inspection.VehicleDry ||
                !inspection.VehicleOdorFree ||
                !inspection.VehicleResidueFree)
            {
                return "NOT_ACCEPTED";
            }

            if (!inspection.MaterialClean ||
                !inspection.MaterialCoveredOrSealed)
            {
                return "NOT_ACCEPTED";
            }


            // Check individual materials
            var hasPhysicalProblem = lines.Any(x =>
                !x.PackagingOk ||
                !x.ContaminationOk ||
                !x.LabelingOk);

            if (hasPhysicalProblem)
                return "NOT_ACCEPTED";


            /*
             * DOCUMENT PROBLEM
             *
             * Physical delivery can proceed,
             * but QA/QC receives it ON HOLD.
             */

            var documentsComplete =
                inspection.SalesInvoiceAvailable &&
                inspection.DeliveryReceiptAvailable &&
                inspection.CoaAvailable;

            if (!documentsComplete)
                return "FOR_QC_ON_HOLD";


            /*
             * EVERYTHING OK
             */

            return "ACCEPTED_FOR_QC";
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
                "ACCEPTED_FOR_QC" =>
                    "Warehouse inspection completed. " +
                    "Delivery accepted for QA/QC.",

                "FOR_QC_ON_HOLD" =>
                    "Warehouse inspection completed. " +
                    "Delivery forwarded to QA/QC ON HOLD " +
                    "due to incomplete documents.",

                "NOT_ACCEPTED" =>
                    "Warehouse inspection completed. " +
                    "Delivery was not accepted.",

                _ =>
                    "Warehouse inspection completed."
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
                    MaterialName = x.material_name
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



        private async Task<string> GenerateQcNoAsync()
        {
            var year = DateTime.Now.Year;

            var prefix = $"QC-{year}-";

            var lastNo = await _db.QcInspectionHeaders
                .Where(x => x.QcNo.StartsWith(prefix))
                .OrderByDescending(x => x.QcId)
                .Select(x => x.QcNo)
                .FirstOrDefaultAsync();

            var nextNo = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var numberPart =
                    lastNo.Replace(prefix, "");

                if (int.TryParse(
                    numberPart,
                    out var lastNumber))
                {
                    nextNo =
                        lastNumber + 1;
                }
            }

            return $"{prefix}{nextNo:0000}";
        }
    }
}