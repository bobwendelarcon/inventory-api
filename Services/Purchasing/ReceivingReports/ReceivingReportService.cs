using inventory_api.Data;
using inventory_api.DTOs.Purchasing.PurchaseOrders;
using inventory_api.DTOs.Purchasing.ReceivingReports;
using inventory_api.Models.Manufacturing.Materials;
using inventory_api.Models.Purchasing.PurchaseOrders;
using inventory_api.Models.Purchasing.QcInspections;
using inventory_api.Models.Purchasing.ReceivingReports;
using inventory_api.Services.Purchasing.SupplierEvaluations;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.ReceivingReports
{
    public class ReceivingReportService
    {
        private readonly AppDbContext _context;

        private readonly SupplierEvaluationGenerationService
            _supplierEvaluationGenerationService;

        public ReceivingReportService(
            AppDbContext context,
            SupplierEvaluationGenerationService
                supplierEvaluationGenerationService)
        {
            _context = context;

            _supplierEvaluationGenerationService =
                supplierEvaluationGenerationService;
        }

        public async Task<string> GenerateRrNoAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"RR-{year}-";

            var lastNo = await _context.ReceivingReportHeaders
                .Where(x => x.RrNo.StartsWith(prefix))
                .OrderByDescending(x => x.RrId)
                .Select(x => x.RrNo)
                .FirstOrDefaultAsync();

            var nextNo = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var numberPart = lastNo.Replace(prefix, "");

                if (int.TryParse(numberPart, out var lastNumber))
                    nextNo = lastNumber + 1;
            }

            return $"{prefix}{nextNo:0000}";
        }

        public async Task<object?> GetCreateOptionsAsync(int scheduleId)
        {
            var schedule = await _context.PurchaseOrderDeliverySchedules
                .Include(x => x.PurchaseOrder)
                .Include(x => x.Lines)
                    .ThenInclude(x => x.PurchaseOrderLine)
                .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId);

            if (schedule == null)
                return null;

            var po = schedule.PurchaseOrder;

            if (po == null)
                throw new Exception("Purchase Order for this schedule was not found.");





            if (po.Status != "APPROVED" &&
                po.Status != "PARTIALLY_RECEIVED")
            {
                throw new Exception(
                    "Only approved or partially received PO can create RR."
                );
            }

            /*
             * One truck arrival = one RR.
             *
             * Once an RR has already been created for this schedule,
             * the same schedule cannot create another RR.
             */
            var hasScheduleRr = await _context.ReceivingReportHeaders
                .AnyAsync(x => x.ScheduleId == scheduleId);

            if (hasScheduleRr)
            {
                throw new Exception(
                    "A Receiving Report has already been created for this delivery schedule."
                );
            }

            /*
             * Temporary rule:
             * only one active RR per PO at a time.
             */
            var hasActivePoRr = await _context.ReceivingReportHeaders
                .AnyAsync(x =>
                    x.PoId == po.PoId &&
                    (
                       x.Status == "FOR_QC"
                    ));

            if (hasActivePoRr)
            {
                throw new Exception(
                    "This Purchase Order already has a Receiving Report awaiting QA/QC."
                );
            }

            if (schedule.Status != "OPEN")
            {
                throw new Exception(
                    $"Schedule #{schedule.ScheduleNo} is already {schedule.Status.Replace("_", " ")}."
                );
            }

            var supplierName = await _context.Suppliers
                .Where(x => x.SupplierId == po.SupplierId)
                .Select(x => x.SupplierName)
                .FirstOrDefaultAsync();

            var materialIds = schedule.Lines
                .Where(x => x.PurchaseOrderLine != null)
                .Select(x => x.PurchaseOrderLine!.MaterialId)
                .Distinct()
                .ToList();

            var materials = await _context.Materials
                .Where(x => materialIds.Contains(x.material_id))
                .Select(x => new
                {
                    MaterialId = x.material_id,
                    MaterialCode = x.material_code,
                    MaterialName = x.material_name
                })
                .ToDictionaryAsync(x => x.MaterialId);

            var lines = schedule.Lines
                .Where(x =>
                    x.BalanceQty > 0 &&
                    x.Status != "RECEIVED" &&
                    x.Status != "CANCELLED" &&
                    x.PurchaseOrderLine != null)
                .Select(x =>
                {
                    var poLine = x.PurchaseOrderLine!;

                    materials.TryGetValue(
                        poLine.MaterialId,
                        out var material
                    );

                    return new
                    {
                        x.ScheduleLineId,
                        x.ScheduleId,

                        poLine.PoLineId,
                        poLine.MaterialId,

                        MaterialCode =
                            material?.MaterialCode ?? "",

                        MaterialName =
                            material?.MaterialName ?? "",

                        // Entire ordered quantity
                        PoQty = poLine.PoQty,

                        // Quantity planned specifically for this schedule
                        ScheduledQty = x.ScheduledQty,

                        // Already physically received for the whole PO line
                        PreviouslyReceivedQty = poLine.ReceivedQty,

                        // Remaining quantity under this specific schedule
                        BalanceQty = x.BalanceQty,

                        // Remaining quantity for the whole PO line
                        PoBalanceQty = poLine.BalanceQty,

                        poLine.Uom
                    };
                })
                .ToList();

            return new
            {
                schedule.ScheduleId,
                schedule.ScheduleNo,
                schedule.ScheduledDate,
                ScheduleStatus = schedule.Status,

                po.PoId,
                po.PoNo,
                po.PrintedPoNo,
                po.SupplierId,

                SupplierName = supplierName ?? "",

                PoStatus = po.Status,

                Lines = lines
            };
        }

        public async Task<int> CreateAsync(
     CreateReceivingReportDto dto,
     string userId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    if (dto.ScheduleId <= 0)
                        throw new Exception("Delivery schedule is required.");

                    if (dto.DeliveryDate == default)
                        throw new Exception("Actual delivery date is required.");

                    if (string.IsNullOrWhiteSpace(dto.BranchId))
                    {
                        throw new InvalidOperationException(
                            "Receiving branch is required.");
                    }

                    if (dto.Lines == null || !dto.Lines.Any())
                        throw new Exception("RR must have at least one line.");

                    var validLines = dto.Lines
                        .Where(x => x.ReceiveQty > 0)
                        .ToList();

                    if (!validLines.Any())
                        throw new Exception("Receive quantity is required.");

                    var schedule = await _context.PurchaseOrderDeliverySchedules
                        .Include(x => x.PurchaseOrder)
                            .ThenInclude(x => x!.Lines)
                        .Include(x => x.Lines)
                        .FirstOrDefaultAsync(x =>
                            x.ScheduleId == dto.ScheduleId);

                    if (schedule == null)
                        throw new Exception("Delivery schedule not found.");

                    var po = schedule.PurchaseOrder;

                    if (po == null)
                        throw new Exception("Purchase Order not found.");

                    if (po.Status != "APPROVED" &&
                        po.Status != "PARTIALLY_RECEIVED")
                    {
                        throw new Exception(
                            "Only approved or partially received PO can create RR."
                        );
                    }

                    if (schedule.Status != "OPEN")
                    {
                        throw new Exception(
                            "Only an open delivery schedule can create a Receiving Report."
                        );
                    }

                    /*
                     * One actual truck arrival creates only one RR
                     * for the selected schedule.
                     */
                    var hasScheduleRr = await _context.ReceivingReportHeaders
                        .AnyAsync(x => x.ScheduleId == dto.ScheduleId);

                    if (hasScheduleRr)
                    {
                        throw new Exception(
                            "A Receiving Report already exists for this delivery schedule."
                        );
                    }

                    /*
                     * Temporary business rule:
                     * only one DRAFT or FOR_QC RR for the whole PO.
                     */
                    var hasActivePoRr = await _context.ReceivingReportHeaders
                        .AnyAsync(x =>
                            x.PoId == po.PoId &&
                            (
                               x.Status == "FOR_QC"
                            ));

                    if (hasActivePoRr)
                    {
                        throw new Exception(
                            "This Purchase Order already has an active Receiving Report."
                        );
                    }

                    var duplicatePoLineIds = validLines
                        .GroupBy(x => x.PoLineId)
                        .Where(x => x.Count() > 1)
                        .Select(x => x.Key)
                        .ToList();

                    if (duplicatePoLineIds.Any())
                    {
                        throw new Exception(
                            "Duplicate receiving lines were submitted."
                        );
                    }

                    var rrNo = await GenerateRrNoAsync();
                    var now = DateTime.Now;

                    var rr = new ReceivingReportHeader
                    {
                        RrNo = rrNo,

                        PoId = po.PoId,
                        ScheduleId = schedule.ScheduleId,

                        PoNo = po.PoNo,
                        SupplierId = po.SupplierId,

                        BranchId = dto.BranchId.Trim(),

                        SiDrNo = dto.SiDrNo,
                        DeliveryDate = dto.DeliveryDate.Date,

                        Remarks = dto.Remarks,

                        Status = "FOR_QC",

                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    foreach (var lineDto in validLines)
                    {
                        var scheduleLine = schedule.Lines
                            .FirstOrDefault(x =>
                                x.PoLineId == lineDto.PoLineId);

                        if (scheduleLine == null)
                        {
                            throw new Exception(
                                "One or more selected materials do not belong to this delivery schedule."
                            );
                        }

                        var poLine = po.Lines
                            .FirstOrDefault(x =>
                                x.PoLineId == lineDto.PoLineId);

                        if (poLine == null)
                            throw new Exception("Invalid PO line selected.");

                        if (scheduleLine.BalanceQty <= 0)
                        {
                            throw new Exception(
                                $"Schedule line for material ID {poLine.MaterialId} has no remaining balance."
                            );
                        }

                        if (poLine.BalanceQty <= 0)
                        {
                            throw new Exception(
                                $"PO line for material ID {poLine.MaterialId} has no remaining balance."
                            );
                        }

                        var exceedsSchedule =
                            lineDto.ReceiveQty > scheduleLine.BalanceQty;

                        var exceedsPo =
                            lineDto.ReceiveQty > poLine.BalanceQty;

                        if ((exceedsSchedule || exceedsPo) &&
                            string.IsNullOrWhiteSpace(lineDto.Remarks))
                        {
                            throw new Exception(
                                $"Remarks are required for over-receiving material ID {poLine.MaterialId}."
                            );
                        }

                        var previousPoReceivedQty = poLine.ReceivedQty;
                        var previousPoBalanceQty = poLine.BalanceQty;

                        rr.Lines.Add(new ReceivingReportLine
                        {
                            PoLineId = poLine.PoLineId,
                            MaterialId = poLine.MaterialId,

                            PoQty = poLine.PoQty,

                            PreviouslyReceivedQty =
                                previousPoReceivedQty,

                            BalanceQty =
                                previousPoBalanceQty,

                            ReceiveQty =
                                lineDto.ReceiveQty,

                            AcceptedQty = 0,
                            RejectedQty = 0,

                            Uom = poLine.Uom,

                            Remarks = lineDto.Remarks,

                            Status = "PENDING",

                            CreatedAt = now
                        });

                        /*
                         * Update physical receiving totals for the PO line.
                         */
                        poLine.ReceivedQty += lineDto.ReceiveQty;

                        poLine.BalanceQty = Math.Max(
                            0m,
                            poLine.PoQty - poLine.ReceivedQty
                        );

                        poLine.Status =
                            poLine.BalanceQty <= 0
                                ? "CLOSED"
                                : "PARTIAL";

                        poLine.UpdatedAt = now;

                        /*
                         * Update the selected delivery schedule line.
                         */
                        scheduleLine.ReceivedQty += lineDto.ReceiveQty;

                        scheduleLine.BalanceQty = Math.Max(
                            0m,
                            scheduleLine.ScheduledQty -
                            scheduleLine.ReceivedQty
                        );

                        scheduleLine.Status =
                            scheduleLine.BalanceQty <= 0
                                ? "RECEIVED"
                                : "PARTIAL";

                        scheduleLine.UpdatedAt = now;
                    }

                    /*
                     * Lines that were scheduled but received as zero
                     * remain OPEN with their full balance.
                     *
                     * Since one truck arrival creates one RR,
                     * those remaining quantities will later be
                     * rescheduled by Purchasing.
                     */
                    var scheduleHasRemaining =
                        schedule.Lines.Any(x => x.BalanceQty > 0);

                    var scheduleHasReceived =
                        schedule.Lines.Any(x => x.ReceivedQty > 0);

                    schedule.Status =
                        !scheduleHasRemaining
                            ? "RECEIVED"
                            : scheduleHasReceived
                                ? "PARTIALLY_RECEIVED"
                                : "OPEN";

                    schedule.UpdatedBy = userId;
                    schedule.UpdatedAt = now;

                    var allPoLinesClosed =
                        po.Lines.All(x => x.BalanceQty <= 0);

                    var anyPoQuantityReceived =
                        po.Lines.Any(x => x.ReceivedQty > 0);

                    po.Status =
                        allPoLinesClosed
                            ? "FULLY_RECEIVED"
                            : anyPoQuantityReceived
                                ? "PARTIALLY_RECEIVED"
                                : "APPROVED";

                    po.UpdatedAt = now;

                    _context.ReceivingReportHeaders.Add(rr);

                    await _context.SaveChangesAsync();


                    var qcNo = await GenerateQcNoAsync();

                    var qc = new QcInspectionHeader
                    {
                        QcNo = qcNo,

                        RrId = rr.RrId,
                        RrNo = rr.RrNo,

                        PoId = rr.PoId,
                        PoNo = rr.PoNo,

                        SupplierId = rr.SupplierId,

                        Status = "FOR_INSPECTION",

                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    foreach (var rrLine in rr.Lines)
                    {
                        qc.Lines.Add(new QcInspectionLine
                        {
                            RrLineId = rrLine.RrLineId,
                            PoLineId = rrLine.PoLineId,
                            MaterialId = rrLine.MaterialId,

                            ReceivedQty = rrLine.ReceiveQty,

                            AcceptedQty = 0,
                            RejectedQty = 0,

                            Status = "PENDING",

                            CreatedAt = now
                        });
                    }

                    _context.QcInspectionHeaders.Add(qc);

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return rr.RrId;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        public async Task SubmitForQcAsync(int rrId)
        {
            var rr = await _context.ReceivingReportHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.RrId == rrId);

            if (rr == null)
                throw new Exception("RR not found.");

            if (rr.Status != "DRAFT")
                throw new Exception("Only draft RR can be submitted for QC.");

            var existingQc = await _context.QcInspectionHeaders
                .AnyAsync(x => x.RrId == rr.RrId);

            if (existingQc)
                throw new Exception("QC inspection already exists for this RR.");

            var qcNo = await GenerateQcNoAsync();

            var qc = new QcInspectionHeader
            {
                QcNo = qcNo,
                RrId = rr.RrId,
                RrNo = rr.RrNo,
                PoId = rr.PoId,
                PoNo = rr.PoNo,
                SupplierId = rr.SupplierId,
                Status = "FOR_INSPECTION",
                CreatedBy = rr.CreatedBy,
                CreatedAt = DateTime.Now
            };

            foreach (var rrLine in rr.Lines)
            {
                qc.Lines.Add(new QcInspectionLine
                {
                    RrLineId = rrLine.RrLineId,
                    PoLineId = rrLine.PoLineId,
                    MaterialId = rrLine.MaterialId,
                    ReceivedQty = rrLine.ReceiveQty,
                    AcceptedQty = 0,
                    RejectedQty = 0,
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                });
            }

            rr.Status = "FOR_QC";
            rr.UpdatedAt = DateTime.Now;

            _context.QcInspectionHeaders.Add(qc);

            await _context.SaveChangesAsync();
        }



        public async Task<List<ReceivingReportListDto>> GetAllAsync()
        {
            var rrList =
                await _context.ReceivingReportHeaders
                    .AsNoTracking()
                    .OrderByDescending(x => x.RrId)
                    .ToListAsync();

            var result =
                new List<ReceivingReportListDto>();

            foreach (var rr in rrList)
            {
                var supplierName =
                    await _context.Suppliers
                        .Where(x =>
                            x.SupplierId == rr.SupplierId)
                        .Select(x =>
                            x.SupplierName)
                        .FirstOrDefaultAsync()
                    ?? "";

                var incoming =
                    rr.ScheduleId.HasValue
                        ? await _context.IncomingReceivings
                            .AsNoTracking()
                            .Where(x =>
                                x.ScheduleId ==
                                rr.ScheduleId.Value)
                            .OrderByDescending(x =>
                                x.IncomingReceivingId)
                            .FirstOrDefaultAsync()
                        : null;

                var qc =
                    incoming != null
                        ? await _context.QcInspectionHeaders
                            .AsNoTracking()
                            .Where(x =>
                                x.IncomingReceivingId ==
                                incoming.IncomingReceivingId)
                            .OrderByDescending(x =>
                                x.QcId)
                            .FirstOrDefaultAsync()
                        : null;

                var processing =
                    incoming != null
                        ? await _context.RmwProcessingHeaders
                            .AsNoTracking()
                            .Where(x =>
                                x.IncomingReceivingId ==
                                incoming.IncomingReceivingId)
                            .OrderByDescending(x =>
                                x.ProcessingId)
                            .FirstOrDefaultAsync()
                        : null;

                result.Add(
                    new ReceivingReportListDto
                    {
                        RrId =
                            rr.RrId,

                        RrNo =
                            rr.RrNo,

                        ScheduleId =
                            rr.ScheduleId,

                        IncomingReceivingId =
                            incoming?.IncomingReceivingId,

                        IncomingNo =
                            incoming?.IncomingNo,

                        QcId =
                            qc?.QcId,

                        QcNo =
                            qc?.QcNo,

                        ProcessingId =
                            processing?.ProcessingId,

                        ProcessingNo =
                            processing?.ProcessingNo,

                        PoNo =
                            rr.PoNo,

                        SupplierName =
                            supplierName,

                        SiDrNo =
                            rr.SiDrNo,

                        DeliveryDate =
                            rr.DeliveryDate,

                        Status =
                            rr.Status,

                        CreatedBy =
    rr.CreatedBy,

                        CreatedAt =
    rr.CreatedAt,

                        CommittedBy =
    rr.CommittedBy,

                        CommittedAt =
    rr.CommittedAt
                    }
                );
            }

            return result;
        }

        public async Task<ReceivingReportDetailsDto?>
     GetByIdAsync(int rrId)
        {
            var rr =
                await _context.ReceivingReportHeaders
                    .AsNoTracking()
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.RrId == rrId);

            if (rr == null)
                return null;


            var supplierName =
                await _context.Suppliers
                    .Where(x =>
                        x.SupplierId == rr.SupplierId)
                    .Select(x =>
                        x.SupplierName)
                    .FirstOrDefaultAsync()
                ?? "";


            var incoming =
                rr.ScheduleId.HasValue
                    ? await _context.IncomingReceivings
                        .AsNoTracking()
                        .Where(x =>
                            x.ScheduleId ==
                            rr.ScheduleId.Value)
                        .OrderByDescending(x =>
                            x.IncomingReceivingId)
                        .FirstOrDefaultAsync()
                    : null;


            var qc =
                incoming != null
                    ? await _context.QcInspectionHeaders
                        .AsNoTracking()
                        .Where(x =>
                            x.IncomingReceivingId ==
                            incoming.IncomingReceivingId)
                        .OrderByDescending(x =>
                            x.QcId)
                        .FirstOrDefaultAsync()
                    : null;


            var processing =
                incoming != null
                    ? await _context.RmwProcessingHeaders
                        .AsNoTracking()
                        .Where(x =>
                            x.IncomingReceivingId ==
                            incoming.IncomingReceivingId)
                        .OrderByDescending(x =>
                            x.ProcessingId)
                        .FirstOrDefaultAsync()
                    : null;

            var createdByName =
    await GetUserNameAsync(rr.CreatedBy);

            var qcByUserId =
                qc?.InspectorId;

            var qcByName =
                await GetUserNameAsync(qcByUserId);

            var committedByName =
                await GetUserNameAsync(rr.CommittedBy);


            var materialIds =
                rr.Lines
                    .Select(x => x.MaterialId)
                    .Distinct()
                    .ToList();


            var materials =
                await _context.Materials
                    .Where(x =>
                        materialIds.Contains(
                            x.material_id))
                    .Select(x => new
                    {
                        MaterialId =
                            x.material_id,

                        MaterialCode =
                            x.material_code,

                        MaterialName =
                            x.material_name
                    })
                    .ToDictionaryAsync(x =>
                        x.MaterialId);


            var lines =
                rr.Lines
                    .OrderBy(x => x.RrLineId)
                    .Select(line =>
                    {
                        materials.TryGetValue(
                            line.MaterialId,
                            out var material
                        );

                        return new ReceivingReportLineDetailsDto
                        {
                            RrLineId =
                                line.RrLineId,

                            PoLineId =
                                line.PoLineId,

                            MaterialId =
                                line.MaterialId,

                            MaterialCode =
                                material?.MaterialCode ?? "",

                            MaterialName =
                                material?.MaterialName ?? "",

                            PoQty =
                                line.PoQty,

                            PreviouslyReceivedQty =
                                line.PreviouslyReceivedQty,

                            BalanceQty =
                                line.BalanceQty,

                            ReceiveQty =
                                line.ReceiveQty,

                            AcceptedQty =
                                line.AcceptedQty,

                            RejectedQty =
                                line.RejectedQty,

                            IsOverReceived =
                                line.ReceiveQty >
                                line.BalanceQty,

                            OverReceivedQty =
                                line.ReceiveQty >
                                line.BalanceQty
                                    ? line.ReceiveQty -
                                      line.BalanceQty
                                    : 0m,

                            Uom =
                                line.Uom,

                            Remarks =
                                line.Remarks,

                            Status =
                                line.Status
                        };
                    })
                    .ToList();


            return new ReceivingReportDetailsDto
            {
                RrId =
                    rr.RrId,

                RrNo =
                    rr.RrNo,

                ScheduleId =
                    rr.ScheduleId,

                IncomingReceivingId =
                    incoming?.IncomingReceivingId,

                IncomingNo =
                    incoming?.IncomingNo,

                QcId =
                    qc?.QcId,

                QcNo =
                    qc?.QcNo,

                ProcessingId =
                    processing?.ProcessingId,

                ProcessingNo =
                    processing?.ProcessingNo,

                PoId =
                    rr.PoId,

                PoNo =
                    rr.PoNo,

                SupplierId =
                    rr.SupplierId,

                SupplierName =
                    supplierName,

                BranchId =
                    rr.BranchId,

                SiDrNo =
                    rr.SiDrNo,

                DeliveryDate =
                    rr.DeliveryDate,

                Remarks =
                    rr.Remarks,

                Status =
                    rr.Status,

                CreatedBy =
    rr.CreatedBy,

                CreatedByName =
    createdByName,

                CreatedAt =
    rr.CreatedAt,

                QcBy =
    qcByUserId,

                QcByName =
    qcByName,

                QcAt =
    qc?.UpdatedAt
    ?? qc?.InspectionDate,

                CommittedBy =
    rr.CommittedBy,

                CommittedByName =
    committedByName,

                CommittedAt =
    rr.CommittedAt,



                Lines =
                    lines
            };
        }

        private async Task<string> GenerateQcNoAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"QC-{year}-";

            var lastNo = await _context.QcInspectionHeaders
                .Where(x => x.QcNo.StartsWith(prefix))
                .OrderByDescending(x => x.QcId)
                .Select(x => x.QcNo)
                .FirstOrDefaultAsync();

            var nextNo = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var numberPart = lastNo.Replace(prefix, "");

                if (int.TryParse(numberPart, out var lastNumber))
                    nextNo = lastNumber + 1;
            }

            return $"{prefix}{nextNo:0000}";
        }

        public async Task<List<ReceivingCalendarDto>> GetReceivingCalendarAsync(
      DateTime startDate,
      DateTime endDate)
        {
            var data =
                await _context.PurchaseOrderDeliverySchedules

               .Where(schedule =>
    schedule.ScheduledDate >= startDate &&
    schedule.ScheduledDate < endDate &&
    (
        schedule.Status == "OPEN" ||
        schedule.Status == "PARTIALLY_RECEIVED" ||
        schedule.Status == "FOR_QC" ||
        schedule.Status == "RECEIVED" ||
        schedule.Status == "COMPLETED" ||
        schedule.Status == "RESCHEDULED" ||
        schedule.Status == "CANCELLED"
    )
)

                .OrderBy(x => x.ScheduledDate)
                .ThenBy(x => x.PoId)

                .Select(schedule => new ReceivingCalendarDto
                {
                    ScheduleId = schedule.ScheduleId,

                    ScheduleNo = schedule.ScheduleNo,

                    PoId = schedule.PoId,

                    PoNo = _context.PurchaseOrderHeaders
                        .Where(po => po.PoId == schedule.PoId)
                        .Select(po => po.PoNo)
                        .FirstOrDefault() ?? "",

                    PrintedPoNo = _context.PurchaseOrderHeaders
                        .Where(po => po.PoId == schedule.PoId)
                        .Select(po => po.PrintedPoNo)
                        .FirstOrDefault(),

                    DeliveryDate = schedule.ScheduledDate,

                    SupplierId = _context.PurchaseOrderHeaders
                        .Where(po => po.PoId == schedule.PoId)
                        .Select(po => po.SupplierId)
                        .FirstOrDefault(),

                    SupplierName =
                        _context.PurchaseOrderHeaders
                        .Where(po => po.PoId == schedule.PoId)
                        .Join(
                            _context.Suppliers,
                            po => po.SupplierId,
                            s => s.SupplierId,
                            (po, s) => s.SupplierName
                        )
                        .FirstOrDefault() ?? "",

                    // =====================================================
                    // CALENDAR DISPLAY STATUS
                    // =====================================================
                    //                Status =
                    //_context.IncomingReceivings
                    //    .Where(ir =>
                    //        ir.ScheduleId == schedule.ScheduleId)
                    //    .OrderByDescending(ir =>
                    //        ir.IncomingReceivingId)
                    //    .Select(ir =>
                    //        ir.ReceivingStatus == "ACCEPTED_FOR_QC"
                    //            ? "WAITING_FOR_QC"

                    //        : ir.ReceivingStatus == "FOR_QC_ON_HOLD"
                    //            ? "WAITING_FOR_QC"

                    //        : ir.ReceivingStatus == "NOT_ACCEPTED"
                    //            ? "NOT_ACCEPTED"

                    //        : schedule.Status
                    //    )
                    //    .FirstOrDefault()
                    //?? schedule.Status,

                    Status =
    // =====================================================
    // 1. FINAL RR COMPLETED
    // =====================================================
    _context.ReceivingReportHeaders
        .Any(rr =>
            rr.ScheduleId == schedule.ScheduleId &&
            rr.Status == "COMMITTED")
        ? "COMPLETED"

    // =====================================================
    // 2. RMW PROCESSING
    // Once RMW exists, it takes priority over QC status.
    // =====================================================
    : _context.RmwProcessingHeaders
        .Where(rmw =>
            rmw.IncomingReceivingId ==
                _context.IncomingReceivings
                    .Where(ir =>
                        ir.ScheduleId == schedule.ScheduleId)
                    .OrderByDescending(ir =>
                        ir.IncomingReceivingId)
                    .Select(ir =>
                        (int?)ir.IncomingReceivingId)
                    .FirstOrDefault())
        .OrderByDescending(rmw =>
            rmw.ProcessingId)
        .Select(rmw =>
            rmw.Status == "READY_FOR_FINAL_RR"
                ? "READY_FOR_FINAL_RR"
                : "RMW_PROCESSING")
        .FirstOrDefault()

    // =====================================================
    // 3. QA/QC COMPLETED
    // =====================================================
    ?? _context.QcInspectionHeaders
        .Where(qc =>
            qc.IncomingReceivingId ==
                _context.IncomingReceivings
                    .Where(ir =>
                        ir.ScheduleId == schedule.ScheduleId)
                    .OrderByDescending(ir =>
                        ir.IncomingReceivingId)
                    .Select(ir =>
                        (int?)ir.IncomingReceivingId)
                    .FirstOrDefault() &&
            qc.Status == "INSPECTED")
        .Select(qc =>
            "QC_COMPLETED")
        .FirstOrDefault()

    // =====================================================
    // 4. INCOMING RECEIVING STATUS
    // =====================================================
    ?? _context.IncomingReceivings
        .Where(ir =>
            ir.ScheduleId == schedule.ScheduleId)
        .OrderByDescending(ir =>
            ir.IncomingReceivingId)
        .Select(ir =>
            ir.ReceivingStatus == "ACCEPTED_FOR_QC"
                ? "WAITING_FOR_QC"

            : ir.ReceivingStatus == "FOR_QC_ON_HOLD"
                ? "WAITING_FOR_QC"

            : ir.ReceivingStatus == "NOT_ACCEPTED"
                ? "NOT_ACCEPTED"

            : schedule.Status)
        .FirstOrDefault()

    // =====================================================
    // 5. FALLBACK
    // =====================================================
    ?? schedule.Status,







                    TotalAmount =
                        _context.PurchaseOrderHeaders
                            .Where(po => po.PoId == schedule.PoId)
                            .Select(po => po.TotalAmount)
                            .FirstOrDefault(),

                    MaterialCount =
                        schedule.Lines.Count(),

                    TotalPoQty =
                        schedule.Lines.Sum(x =>
                            x.ScheduledQty),

                    TotalReceivedQty =
                        schedule.Lines.Sum(x =>
                            x.ReceivedQty),

                    TotalBalanceQty =
                        schedule.Lines.Sum(x =>
                            x.BalanceQty)
                })

                .ToListAsync();

            return data;
        }

        public async Task<DeliveryScheduleDetailsDto?> GetScheduleDetailsAsync(
     int scheduleId)
        {
            var schedule = await _context.PurchaseOrderDeliverySchedules
                .Include(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Lines)
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x =>
                    x.ScheduleId == scheduleId);

            if (schedule == null)
                return null;

            var po = schedule.PurchaseOrder;

            if (po == null)
            {
                throw new Exception(
                    "Purchase Order for this delivery schedule was not found.");
            }


            // ============================================================
            // SUPPLIER
            // ============================================================

            var supplierName = await _context.Suppliers
                .Where(x => x.SupplierId == po.SupplierId)
                .Select(x => x.SupplierName)
                .FirstOrDefaultAsync() ?? "";


            // ============================================================
            // LATEST INCOMING RECEIVING
            // ============================================================

            var incoming = await _context.IncomingReceivings
                .Where(x =>
                    x.ScheduleId == schedule.ScheduleId)
                .OrderByDescending(x =>
                    x.IncomingReceivingId)
                .Select(x => new
                {
                    x.IncomingReceivingId,
                    x.IncomingNo,
                    x.ReceivingStatus,
                    x.DeliveryDate
                })
                .FirstOrDefaultAsync();


            // ============================================================
            // PHYSICAL DELIVERED QTY FROM INCOMING RECEIVING
            //
            // NOT_ACCEPTED is not counted.
            // ============================================================

            var deliveredByPoLine =
                await _context.IncomingReceivingLines
                    .Where(x =>
                        x.IncomingReceiving != null &&
                        x.IncomingReceiving.ScheduleId ==
                            schedule.ScheduleId &&
                        x.IncomingReceiving.ReceivingStatus !=
                            "NOT_ACCEPTED")
                    .GroupBy(x => x.PoLineId)
                    .Select(g => new
                    {
                        PoLineId = g.Key,
                        DeliveredQty =
                            g.Sum(x => x.DeliveredQty)
                    })
                    .ToDictionaryAsync(
                        x => x.PoLineId,
                        x => x.DeliveredQty);


            // ============================================================
            // FINAL RR
            //
            // This remains separate from Incoming Receiving.
            // ============================================================

            var rr = await _context.ReceivingReportHeaders
                .Where(x =>
                    x.ScheduleId == schedule.ScheduleId)
                .OrderByDescending(x => x.RrId)
                .Select(x => new
                {
                    x.RrId,
                    x.RrNo,
                    x.Status
                })
                .FirstOrDefaultAsync();


            // ============================================================
            // QA/QC
            //
            // Current old QC is still linked to RR.
            // Keep it until we refactor QA/QC next.
            // ============================================================

            var qc =
      incoming == null
          ? null
          : await _context.QcInspectionHeaders
              .Where(x =>
                  x.IncomingReceivingId ==
                      incoming.IncomingReceivingId)
              .OrderByDescending(x => x.QcId)
              .Select(x => new
              {
                  x.QcId,
                  x.QcNo,
                  x.Status,
                  x.Decision
              })
              .FirstOrDefaultAsync();

            // ============================================================
            // MATERIAL MASTER
            // ============================================================

            var materialIds = schedule.Lines
                .Join(
                    po.Lines,
                    scheduleLine =>
                        scheduleLine.PoLineId,
                    poLine =>
                        poLine.PoLineId,
                    (scheduleLine, poLine) =>
                        poLine.MaterialId
                )
                .Distinct()
                .ToList();

            var materials =
                await _context.Materials
                    .Where(x =>
                        materialIds.Contains(
                            x.material_id))
                    .Select(x => new
                    {
                        MaterialId =
                            x.material_id,

                        MaterialCode =
                            x.material_code,

                        MaterialName =
                            x.material_name
                    })
                    .ToDictionaryAsync(
                        x => x.MaterialId);


            // ============================================================
            // FINAL RR QTY PER PO LINE
            //
            // For now ReceiveQty represents the RR quantity.
            // Later Final RR logic will be refined.
            // ============================================================

            var finalReceivedByPoLine =
                await _context.ReceivingReportLines
                    .Where(x =>
                        x.Header != null &&
                        x.Header.ScheduleId ==
                            schedule.ScheduleId)
                    .GroupBy(x => x.PoLineId)
                    .Select(g => new
                    {
                        PoLineId = g.Key,

                        FinalReceivedQty =
                            g.Sum(x => x.ReceiveQty)
                    })
                    .ToDictionaryAsync(
                        x => x.PoLineId,
                        x => x.FinalReceivedQty);


            // ============================================================
            // BUILD LINES
            // ============================================================

            var lines = schedule.Lines
                .OrderBy(x =>
                    x.ScheduleLineId)
                .Select(scheduleLine =>
                {
                    var poLine =
                        po.Lines.First(
                            x =>
                                x.PoLineId ==
                                scheduleLine.PoLineId
                        );

                    materials.TryGetValue(
                        poLine.MaterialId,
                        out var material);


                    var deliveredQty =
                        deliveredByPoLine.TryGetValue(
                            poLine.PoLineId,
                            out var delivered)
                                ? delivered
                                : 0m;


                    var finalReceivedQty =
                        finalReceivedByPoLine.TryGetValue(
                            poLine.PoLineId,
                            out var finalReceived)
                                ? finalReceived
                                : 0m;


                    var remainingDeliveryQty =
                        Math.Max(
                            0m,
                            scheduleLine.ScheduledQty -
                            deliveredQty);


                    var lineStatus =
                        deliveredQty <= 0
                            ? "OPEN"

                            : remainingDeliveryQty > 0
                                ? "PARTIALLY_DELIVERED"

                                : "DELIVERED";


                    return new DeliveryScheduleLineDetailsDto
                    {
                        ScheduleLineId =
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


                        // Physical supplier delivery
                        DeliveredQty =
                            deliveredQty,


                        // Final RR qty
                        FinalReceivedQty =
                            finalReceivedQty,


                        // Keep for old UI compatibility temporarily
                        ReceivedQty =
    deliveredQty,


                        // Remaining physical delivery
                        RemainingQty =
                            remainingDeliveryQty,

                        Uom =
                            poLine.Uom,

                        Status =
                            lineStatus
                    };
                })
                .ToList();


            // ============================================================
            // WORKFLOW STATUS
            // ============================================================

            string workflowStatus;

            if (incoming == null)
            {
                workflowStatus =
                    "SCHEDULED_RECEIVING";
            }
            else
            {
                workflowStatus =
       incoming.ReceivingStatus switch
       {
           "ACCEPTED_FOR_QC" =>
               "WAITING_FOR_QC",

           "FOR_QC_ON_HOLD" =>
               "QA_QC_ON_HOLD",

           "NOT_ACCEPTED" =>
               "NOT_ACCEPTED",

           _ =>
               incoming.ReceivingStatus
       };
            }


            // ============================================================
            // SCHEDULE DELIVERY STATUS
            // ============================================================

            var totalScheduled =
                lines.Sum(x =>
                    x.ScheduledQty);

            var totalDelivered =
                lines.Sum(x =>
                    x.DeliveredQty);

            var totalFinalReceived =
                lines.Sum(x =>
                    x.FinalReceivedQty);

            var totalRemaining =
                lines.Sum(x =>
                    x.RemainingQty);


            string calculatedScheduleStatus;

            if (totalDelivered <= 0)
            {
                calculatedScheduleStatus =
                    schedule.Status;
            }
            else if (totalRemaining > 0)
            {
                calculatedScheduleStatus =
                    "PARTIALLY_DELIVERED";
            }
            else
            {
                calculatedScheduleStatus =
                    "DELIVERED";
            }


            // ============================================================
            // RESCHEDULE
            // ============================================================

            var hasRemaining =
                totalRemaining > 0;

            var hasExistingChildSchedule =
                await _context
                    .PurchaseOrderDeliverySchedules
                    .AnyAsync(x =>
                        x.RescheduledFromScheduleId ==
                            schedule.ScheduleId &&
                        x.Status != "CANCELLED");


            // ============================================================
            // RESPONSE
            // ============================================================

            return new DeliveryScheduleDetailsDto
            {
                ScheduleId =
                    schedule.ScheduleId,

                ScheduleNo =
                    schedule.ScheduleNo,

                PoId =
                    po.PoId,

                PoNo =
                    po.PoNo,

                PrintedPoNo =
                    po.PrintedPoNo,

                ScheduledDate =
                    schedule.ScheduledDate,


                // Delivery status
                ScheduleStatus =
                    calculatedScheduleStatus,


                // Current business workflow
                WorkflowStatus =
                    workflowStatus,


                SupplierId =
                    po.SupplierId,

                SupplierName =
                    supplierName,

                RescheduledFromScheduleId =
                    schedule.RescheduledFromScheduleId,

                Remarks =
                    schedule.Remarks,


                // Incoming Receiving
                IncomingReceivingId =
                    incoming?.IncomingReceivingId,

                IncomingNo =
                    incoming?.IncomingNo,

                IncomingStatus =
                    incoming?.ReceivingStatus,


                // Totals
                TotalScheduledQty =
                    totalScheduled,

                TotalDeliveredQty =
    totalDelivered,

                TotalFinalReceivedQty =
    totalFinalReceived,

                // UI "Received" = physically delivered quantity
                TotalReceivedQty =
    totalDelivered,

                TotalRemainingQty =
    totalRemaining,

                // Final RR
                RrId =
                    rr?.RrId,

                RrNo =
                    rr?.RrNo,

                RrStatus =
                    rr?.Status,


                // QA/QC
                QcNo =
                    qc?.QcNo,

                QcStatus =
                    qc?.Status,

                QcDecision =
                    qc?.Decision,


                CanReschedule =
                    hasRemaining &&
                    !hasExistingChildSchedule,

                Lines =
                    lines
            };
        }

        public async Task<List<int>> RescheduleRemainingAsync(
       int sourceScheduleId,
       RescheduleRemainingDeliveryDto dto,
       string userId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    if (dto.Schedules == null || !dto.Schedules.Any())
                {
                    throw new Exception(
                        "At least one new delivery schedule is required."
                    );
                }

                var sourceSchedule =
                    await _context.PurchaseOrderDeliverySchedules
                        .Include(x => x.PurchaseOrder)
                            .ThenInclude(x => x!.Lines)
                        .Include(x => x.Lines)
                        .FirstOrDefaultAsync(x =>
                            x.ScheduleId == sourceScheduleId);

                if (sourceSchedule == null)
                {
                    throw new Exception(
                        "Source delivery schedule was not found."
                    );
                }

                var po = sourceSchedule.PurchaseOrder;

                if (po == null)
                    throw new Exception("Purchase Order was not found.");

                if (sourceSchedule.Status != "PARTIALLY_RECEIVED")
                {
                    throw new Exception(
                        "Only a partially received schedule can be rescheduled."
                    );
                }

                var existingReplacement =
                    await _context.PurchaseOrderDeliverySchedules
                        .AnyAsync(x =>
                            x.RescheduledFromScheduleId ==
                                sourceSchedule.ScheduleId &&
                            x.Status != "CANCELLED");

                if (existingReplacement)
                {
                    throw new Exception(
                        "The remaining quantity from this schedule has already been rescheduled."
                    );
                }

                var remainingLines = sourceSchedule.Lines
                    .Where(x => x.BalanceQty > 0)
                    .ToList();

                if (!remainingLines.Any())
                {
                    throw new Exception(
                        "This delivery schedule has no remaining quantity."
                    );
                }

                foreach (var requestedSchedule in dto.Schedules)
                {
                    if (requestedSchedule.ScheduledDate == default)
                    {
                        throw new Exception(
                            "Each new schedule must have a scheduled date."
                        );
                    }

                    if (requestedSchedule.Lines == null ||
                        !requestedSchedule.Lines.Any(x => x.RescheduleQty > 0))
                    {
                        throw new Exception(
                            "Each new schedule must contain at least one quantity."
                        );
                    }

                    var duplicateLineIds = requestedSchedule.Lines
                        .GroupBy(x => x.ScheduleLineId)
                        .Where(x => x.Count() > 1)
                        .Select(x => x.Key)
                        .ToList();

                    if (duplicateLineIds.Any())
                    {
                        throw new Exception(
                            "A schedule contains duplicate material lines."
                        );
                    }
                }

                var requestedTotals = dto.Schedules
                    .SelectMany(x => x.Lines)
                    .Where(x => x.RescheduleQty > 0)
                    .GroupBy(x => x.ScheduleLineId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Sum(y => y.RescheduleQty)
                    );

                foreach (var sourceLine in remainingLines)
                {
                    requestedTotals.TryGetValue(
                        sourceLine.ScheduleLineId,
                        out var requestedQty
                    );

                    if (requestedQty <= 0)
                    {
                        throw new Exception(
                            "All remaining schedule quantities must be assigned to a new delivery schedule."
                        );
                    }

                    if (requestedQty != sourceLine.BalanceQty)
                    {
                        throw new Exception(
                            $"The total rescheduled quantity must equal the remaining quantity for schedule line {sourceLine.ScheduleLineId}."
                        );
                    }
                }

                var invalidLineIds = requestedTotals.Keys
                    .Where(id =>
                        remainingLines.All(x =>
                            x.ScheduleLineId != id))
                    .ToList();

                if (invalidLineIds.Any())
                {
                    throw new Exception(
                        "One or more submitted lines do not belong to the source delivery schedule."
                    );
                }

                var nextScheduleNo =
                    await _context.PurchaseOrderDeliverySchedules
                        .Where(x => x.PoId == po.PoId)
                        .Select(x => (int?)x.ScheduleNo)
                        .MaxAsync() ?? 0;

                var now = DateTime.Now;

                var newSchedules =
                    new List<PurchaseOrderDeliverySchedule>();

                foreach (var requestedSchedule in dto.Schedules
                             .OrderBy(x => x.ScheduledDate))
                {
                    nextScheduleNo++;

                    var newSchedule =
                        new PurchaseOrderDeliverySchedule
                        {
                            PoId = po.PoId,

                            ScheduleNo = nextScheduleNo,

                            ScheduledDate =
                                requestedSchedule.ScheduledDate.Date,

                            Status = "OPEN",

                            RescheduledFromScheduleId =
                                sourceSchedule.ScheduleId,

                            Remarks =
                                !string.IsNullOrWhiteSpace(
                                    requestedSchedule.Remarks)
                                    ? requestedSchedule.Remarks
                                    : !string.IsNullOrWhiteSpace(dto.Reason)
                                        ? dto.Reason
                                        : $"Remaining delivery from Schedule #{sourceSchedule.ScheduleNo}.",

                            CreatedBy = userId,
                            CreatedAt = now
                        };

                    foreach (var requestedLine in requestedSchedule.Lines
                                 .Where(x => x.RescheduleQty > 0))
                    {
                        var sourceLine = remainingLines
                            .First(x =>
                                x.ScheduleLineId ==
                                requestedLine.ScheduleLineId);

                        newSchedule.Lines.Add(
                            new PurchaseOrderDeliveryScheduleLine
                            {
                                PoLineId = sourceLine.PoLineId,

                                ScheduledQty =
                                    requestedLine.RescheduleQty,

                                ReceivedQty = 0,

                                BalanceQty =
                                    requestedLine.RescheduleQty,

                                Status = "OPEN",

                                Remarks =
                                    $"Rescheduled from Schedule #{sourceSchedule.ScheduleNo}.",

                                CreatedAt = now
                            }
                        );
                    }

                    newSchedules.Add(newSchedule);
                }

                foreach (var sourceLine in remainingLines)
                {
                    sourceLine.BalanceQty = 0;
                    sourceLine.Status = "RESCHEDULED";
                    sourceLine.UpdatedAt = now;
                }

                sourceSchedule.Status = "RESCHEDULED";
                sourceSchedule.UpdatedBy = userId;
                sourceSchedule.UpdatedAt = now;

                _context.PurchaseOrderDeliverySchedules
                    .AddRange(newSchedules);

                await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return newSchedules
                        .Select(x => x.ScheduleId)
                        .ToList();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }


        public async Task<object> GetPendingFinalRrAsync()
        {
            var headers =
                await _context.RmwProcessingHeaders
                    .Include(x => x.Lines)
                    .Where(x =>
                        x.Status == "READY_FOR_FINAL_RR")
                    .OrderByDescending(x =>
                        x.CompletedAt ?? x.CreatedAt)
                    .ToListAsync();

            var result = new List<object>();

            foreach (var x in headers)
            {
                var incoming =
                    x.IncomingReceivingId.HasValue
                        ? await _context.IncomingReceivings
                            .FirstOrDefaultAsync(ir =>
                                ir.IncomingReceivingId ==
                                x.IncomingReceivingId.Value)
                        : null;

                var po =
                    await _context.PurchaseOrderHeaders
                        .FirstOrDefaultAsync(p =>
                            p.PoId == x.PoId);

                var supplierName =
                    await _context.Suppliers
                        .Where(s =>
                            s.SupplierId == x.SupplierId)
                        .Select(s =>
                            s.SupplierName)
                        .FirstOrDefaultAsync()
                    ?? "";

                var qc =
                    await _context.QcInspectionHeaders
                        .FirstOrDefaultAsync(q =>
                            q.QcId == x.QcId);

                result.Add(new
                {
                    x.ProcessingId,
                    x.ProcessingNo,

                    x.IncomingReceivingId,

                    IncomingNo =
                        incoming?.IncomingNo ?? "",

                    x.PoId,

                    PoNo =
                        po?.PoNo ?? "",

                    PrintedPoNo =
                        po?.PrintedPoNo,

                    x.SupplierId,

                    SupplierName =
                        supplierName,

                    DeliveryDate =
                        incoming?.DeliveryDate,

                    SiDrNo =
                        incoming?.SiDrNo,

                    x.QcId,

                    QcNo =
                        qc?.QcNo ?? "",

                    QcDecision =
                        qc?.Decision,

                    MaterialCount =
                        x.Lines.Count,

                    TotalQaAcceptedQty =
                        x.Lines.Sum(line =>
                            line.QaAcceptedQty),

                    TotalActualQty =
                        x.Lines.Sum(line =>
                            line.ActualQty ?? 0m),

                    x.Status,
                    x.CompletedAt
                });
            }

            return result;
        }

        public async Task<object?> GetFinalRrDetailsAsync(
       int processingId)
        {
            var processing =
                await _context.RmwProcessingHeaders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingId == processingId);

            if (processing == null)
                return null;

            if (processing.Status != "READY_FOR_FINAL_RR")
            {
                throw new InvalidOperationException(
                    "This raw material processing record is not ready for Final RR."
                );
            }


            var incoming =
                processing.IncomingReceivingId.HasValue
                    ? await _context.IncomingReceivings
                        .FirstOrDefaultAsync(x =>
                            x.IncomingReceivingId ==
                            processing.IncomingReceivingId.Value)
                    : null;

            if (incoming == null)
            {
                throw new InvalidOperationException(
                    "Incoming Receiving record was not found."
                );
            }


            var po =
                await _context.PurchaseOrderHeaders
                    .FirstOrDefaultAsync(x =>
                        x.PoId == processing.PoId);

            if (po == null)
            {
                throw new InvalidOperationException(
                    "Purchase Order was not found."
                );
            }

           


            var supplierName =
                await _context.Suppliers
                    .Where(x =>
                        x.SupplierId == processing.SupplierId)
                    .Select(x => x.SupplierName)
                    .FirstOrDefaultAsync()
                ?? "";


            var qc =
                await _context.QcInspectionHeaders
                    .FirstOrDefaultAsync(x =>
                        x.QcId == processing.QcId);


            var materialIds =
                processing.Lines
                    .Select(x => x.MaterialId)
                    .Distinct()
                    .ToList();


            var materials =
                await _context.Materials
                    .Where(x =>
                        materialIds.Contains(x.material_id))
                    .Select(x => new
                    {
                        MaterialId =
                            x.material_id,

                        MaterialCode =
                            x.material_code,

                        MaterialName =
                            x.material_name
                    })
                    .ToDictionaryAsync(x =>
                        x.MaterialId);


            var lines =
                processing.Lines
                    .OrderBy(x => x.ProcessingLineId)
                    .Select(x =>
                    {
                        materials.TryGetValue(
                            x.MaterialId,
                            out var material
                        );

                        return new
                        {
                            x.ProcessingLineId,
                            x.QuarantineLineId,
                            x.QcLineId,
                            x.QcLineLotId,
                            x.IncomingReceivingLineId,
                            x.PoLineId,
                            x.MaterialId,

                            MaterialCode =
                                material?.MaterialCode ?? "",

                            MaterialName =
                                material?.MaterialName ?? "",

                            x.LotNo,
                            x.ManufacturingDate,
                            x.ExpirationDate,

                            // Quantity accepted by QA/QC
                            QaAcceptedQty =
                                x.QaAcceptedQty,

                            // Actual quantity verified by RMW
                            ActualQty =
                                x.ActualQty ?? 0m,

                            // Difference between QA qty and actual qty
                            VarianceQty =
                                x.VarianceQty ?? 0m,

                            Uom =
                                x.Uom ?? "",

                            ProcessingStatus =
                                x.Status,

                            x.Remarks
                        };
                    })
                    .ToList();


            return new
            {
                processing.ProcessingId,
                processing.ProcessingNo,
                processing.Status,

                processing.QuarantineId,

                IncomingReceivingId =
                    incoming.IncomingReceivingId,

                incoming.IncomingNo,

                po.PoId,
                po.PoNo,
                po.PrintedPoNo,

                SupplierId =
                    processing.SupplierId,

                SupplierName =
                    supplierName,

                incoming.SiDrNo,
                incoming.DeliveryDate,
                incoming.BranchId,

                QcId =
                    qc?.QcId,

                QcNo =
                    qc?.QcNo,

                QcStatus =
                    qc?.Status,

                QcDecision =
                    qc?.Decision,

                TotalQaAcceptedQty =
                    lines.Sum(x =>
                        x.QaAcceptedQty),

                TotalActualQty =
                    lines.Sum(x =>
                        x.ActualQty),

                TotalVarianceQty =
                    lines.Sum(x =>
                        x.VarianceQty),

                Lines =
                    lines
            };
        }


        public async Task<CompleteFinalRrResultDto>
    CompleteFinalRrAsync(
        int processingId,
        CompleteFinalRrDto dto,
        string userId)
        {
            var strategy =
                _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    var now = DateTime.Now;

                    // =========================================================
                    // 1. LOAD RMW PROCESSING
                    // =========================================================
                    var processing =
                        await _context.RmwProcessingHeaders
                            .Include(x => x.Lines)
                            .FirstOrDefaultAsync(x =>
                                x.ProcessingId == processingId);

                    if (processing == null)
                    {
                        throw new InvalidOperationException(
                            "Raw Material Processing record was not found."
                        );
                    }

                    if (processing.Status != "READY_FOR_FINAL_RR")
                    {
                        throw new InvalidOperationException(
                            $"Processing record is not ready for Final RR. " +
                            $"Current status: {processing.Status}"
                        );
                    }

                    if (!processing.Lines.Any())
                    {
                        throw new InvalidOperationException(
                            "Raw Material Processing has no material lines."
                        );
                    }

                    if (processing.Lines.Any(x =>
                            x.Status != "STICKER_COMPLETED"))
                    {
                        throw new InvalidOperationException(
                            "All material lines must complete sticker / identification first."
                        );
                    }

                    if (processing.Lines.Any(x =>
                            !x.ActualQty.HasValue ||
                            x.ActualQty.Value <= 0))
                    {
                        throw new InvalidOperationException(
                            "All material lines must have a valid actual quantity."
                        );
                    }


                    // =========================================================
                    // 2. LOAD INCOMING RECEIVING
                    // =========================================================
                    if (!processing.IncomingReceivingId.HasValue)
                    {
                        throw new InvalidOperationException(
                            "Incoming Receiving reference is missing."
                        );
                    }

                    var incoming =
                        await _context.IncomingReceivings
                            .FirstOrDefaultAsync(x =>
                                x.IncomingReceivingId ==
                                processing.IncomingReceivingId.Value);

                    if (incoming == null)
                    {
                        throw new InvalidOperationException(
                            "Incoming Receiving record was not found."
                        );
                    }


                    // =========================================================
                    // 3. LOAD PURCHASE ORDER
                    // =========================================================
                    var po =
                        await _context.PurchaseOrderHeaders
                            .Include(x => x.Lines)
                            .FirstOrDefaultAsync(x =>
                                x.PoId == processing.PoId);

                    if (po == null)
                    {
                        throw new InvalidOperationException(
                            "Purchase Order was not found."
                        );
                    }


                    // =========================================================
                    // 4. LOAD QA/QC INSPECTION
                    // =========================================================
                    var qc =
                        await _context.QcInspectionHeaders
                            .Include(x => x.Lines)
                            .FirstOrDefaultAsync(x =>
                                x.QcId == processing.QcId);

                    if (qc == null)
                    {
                        throw new InvalidOperationException(
                            "QA/QC inspection record was not found."
                        );
                    }


                    // =========================================================
                    // 5. DUPLICATE FINAL RR PROTECTION
                    // =========================================================


                    // =========================================================
                    // 4. DUPLICATE FINAL RR PROTECTION
                    // =========================================================
                    var existingFinalRr =
                        await _context.ReceivingReportHeaders
                            .AnyAsync(x =>
                                x.ScheduleId == incoming.ScheduleId &&
                                x.Status == "COMMITTED");

                    if (existingFinalRr)
                    {
                        throw new InvalidOperationException(
                            "A completed Final Receiving Report already exists " +
                            "for this delivery schedule."
                        );
                    }


                    // =========================================================
                    // 5. GENERATE RR
                    // =========================================================
                    var rrNo =
                        await GenerateRrNoAsync();

                    var rr =
                        new ReceivingReportHeader
                        {
                            RrNo = rrNo,

                            PoId = po.PoId,

                            ScheduleId =
                                incoming.ScheduleId,

                            PoNo =
                                po.PoNo,

                            SupplierId =
                                processing.SupplierId,

                            BranchId =
                                incoming.BranchId ?? "",

                            SiDrNo =
                                incoming.SiDrNo,

                            DeliveryDate =
                                incoming.DeliveryDate,

                            Remarks =
                                dto.Remarks,

                            Status =
                                "COMMITTED",

                            CreatedBy =
                                userId,

                            CreatedAt =
                                now,

                            CommittedBy =
                                userId,

                            CommittedAt =
                                now
                        };


                    // =========================================================
                    // 6. CREATE FINAL RR LINES
                    // =========================================================
                    foreach (var rmwLine in processing.Lines)
                    {
                        var poLine =
                            po.Lines.FirstOrDefault(x =>
                                x.PoLineId ==
                                rmwLine.PoLineId);

                        if (poLine == null)
                        {
                            throw new InvalidOperationException(
                                $"PO line {rmwLine.PoLineId} was not found."
                            );
                        }

                        var finalQty =
                            rmwLine.ActualQty!.Value;

                        var previousReceived =
                            poLine.ReceivedQty;

                        var previousBalance =
                            poLine.BalanceQty;

                        rr.Lines.Add(
                            new ReceivingReportLine
                            {
                                PoLineId =
                                    poLine.PoLineId,

                                MaterialId =
                                    rmwLine.MaterialId,

                                PoQty =
                                    poLine.PoQty,

                                PreviouslyReceivedQty =
                                    previousReceived,

                                BalanceQty =
                                    previousBalance,

                                ReceiveQty =
                                    finalQty,

                                // Final RR has already passed QA/QC
                                AcceptedQty =
                                    finalQty,

                                RejectedQty =
                                    0,

                                Uom =
                                    rmwLine.Uom
                                    ?? poLine.Uom,

                                Remarks =
                                    rmwLine.Remarks,

                                Status =
                                    "COMMITTED",

                                CreatedAt =
                                    now
                            }
                        );
                    }


                    _context.ReceivingReportHeaders.Add(rr);

                    // Need RR ID before inventory transaction references it.
                    await _context.SaveChangesAsync();


                    // =========================================================
                    // 7. POST TO RAW MATERIAL INVENTORY
                    // =========================================================
                    foreach (var rmwLine in processing.Lines)
                    {
                        var finalQty =
                            rmwLine.ActualQty!.Value;

                        var material =
                            await _context.Materials
                                .FirstOrDefaultAsync(x =>
                                    x.material_id ==
                                    rmwLine.MaterialId);

                        if (material == null)
                        {
                            throw new InvalidOperationException(
                                $"Material ID {rmwLine.MaterialId} was not found."
                            );
                        }


                        // -----------------------------------------------------
                        // Stable inventory lot number
                        // -----------------------------------------------------
                        string inventoryLotNo;

                        if (material.is_lot_tracked)
                        {
                            inventoryLotNo =
                                rmwLine.LotNo?
                                    .Trim()
                                    .ToUpperInvariant()
                                ?? "";

                            if (string.IsNullOrWhiteSpace(
                                inventoryLotNo))
                            {
                                throw new InvalidOperationException(
                                    $"Lot number is required for " +
                                    $"{material.material_name}."
                                );
                            }
                        }
                        else
                        {
                            // IMPORTANT:
                            // One stable balance row for non-lot material.
                            inventoryLotNo =
                                $"NON-LOT-MAT-{rmwLine.MaterialId}";
                        }


                        // =====================================================
                        // 8. DUPLICATE INVENTORY POSTING PROTECTION
                        // =====================================================
                        var alreadyPosted =
                            await _context
                                .MaterialInventoryTransactions
                                .AnyAsync(x =>
                                    x.material_id ==
                                        rmwLine.MaterialId &&

                                    x.branch_id ==
                                        rr.BranchId &&

                                    x.lot_no ==
                                        inventoryLotNo &&

                                    x.transaction_type ==
                                        "PURCHASE_RECEIPT" &&

                                    x.reference_type ==
                                        "FINAL_RR" &&

                                    x.reference_id ==
                                        rr.RrId);

                        if (alreadyPosted)
                        {
                            throw new InvalidOperationException(
                                $"Material {material.material_name} " +
                                $"has already been committed for {rr.RrNo}."
                            );
                        }


                        // =====================================================
                        // 9. FIND OR CREATE INVENTORY BALANCE
                        // =====================================================
                        var inventoryLot =
                            await _context.MaterialLotNumbers
                                .FirstOrDefaultAsync(x =>
                                    x.material_id ==
                                        rmwLine.MaterialId &&

                                    x.branch_id ==
                                        rr.BranchId &&

                                    x.lot_no ==
                                        inventoryLotNo);

                        if (inventoryLot == null)
                        {
                            inventoryLot =
                                new MaterialLotNumber
                                {
                                    material_id =
                                        rmwLine.MaterialId,

                                    branch_id =
                                        rr.BranchId,

                                    lot_no =
                                        inventoryLotNo,

                                    manufacturing_date =
                                        material.is_lot_tracked
                                            ? rmwLine.ManufacturingDate
                                            : null,

                                    expiration_date =
                                        material.is_lot_tracked
                                            ? rmwLine.ExpirationDate
                                            : null,

                                    quantity =
                                        finalQty,

                                    uom =
                                        rmwLine.Uom
                                        ?? material.uom,

                                    supplier_id =
                                        material.is_lot_tracked
                                            ? processing.SupplierId
                                            : null,

                                    remarks =
                                        $"Final inventory receipt through {rr.RrNo}.",

                                    is_active =
                                        true,

                                    created_at =
                                        now
                                };

                            _context.MaterialLotNumbers.Add(
                                inventoryLot
                            );
                        }
                        else
                        {
                            inventoryLot.quantity +=
                                finalQty;

                            inventoryLot.is_active =
                                true;

                            inventoryLot.updated_at =
                                now;

                            if (material.is_lot_tracked)
                            {
                                inventoryLot
                                    .manufacturing_date ??=
                                        rmwLine.ManufacturingDate;

                                inventoryLot
                                    .expiration_date ??=
                                        rmwLine.ExpirationDate;

                                inventoryLot
                                    .supplier_id ??=
                                        processing.SupplierId;
                            }

                            if (string.IsNullOrWhiteSpace(
                                inventoryLot.uom))
                            {
                                inventoryLot.uom =
                                    rmwLine.Uom
                                    ?? material.uom;
                            }
                        }


                        // =====================================================
                        // 10. INVENTORY TRANSACTION HISTORY
                        // =====================================================
                        var inventoryTransaction =
                            new MaterialInventoryTransaction
                            {
                                material_id =
                                    rmwLine.MaterialId,

                                branch_id =
                                    rr.BranchId,

                                lot_no =
                                    inventoryLotNo,

                                transaction_type =
                                    "PURCHASE_RECEIPT",

                                quantity =
                                    finalQty,

                                uom =
                                    rmwLine.Uom
                                    ?? material.uom,

                                supplier_id =
                                    processing.SupplierId,

                                reference_type =
                                    "FINAL_RR",

                                reference_id =
                                    rr.RrId,

                                reference_no =
                                    rr.RrNo,

                                remarks =
                                    material.is_lot_tracked
                                        ? $"Final received inventory from " +
                                          $"{rr.RrNo}, PO {po.PoNo}, " +
                                          $"lot {inventoryLotNo}."
                                        : $"Final received inventory from " +
                                          $"{rr.RrNo}, PO {po.PoNo}.",

                                encoded_by =
                                    userId,

                                transaction_date =
                                    now,

                                created_at =
                                    now
                            };

                        _context.MaterialInventoryTransactions
                            .Add(inventoryTransaction);
                    }


                    // =========================================================
                    // 11. UPDATE PO FINAL RECEIVED QUANTITIES
                    // =========================================================
                    foreach (var rmwLine in processing.Lines)
                    {
                        var poLine =
                            po.Lines.First(x =>
                                x.PoLineId ==
                                rmwLine.PoLineId);

                        var finalQty =
                            rmwLine.ActualQty!.Value;

                        poLine.ReceivedQty +=
                            finalQty;

                        poLine.BalanceQty =
                            Math.Max(
                                0m,
                                poLine.PoQty -
                                poLine.ReceivedQty
                            );

                        poLine.Status =
                            poLine.BalanceQty <= 0
                                ? "CLOSED"
                                : "PARTIAL";

                        poLine.UpdatedAt =
                            now;
                    }


                    var allPoLinesClosed =
                        po.Lines.All(x =>
                            x.BalanceQty <= 0);

                    var anyReceived =
                        po.Lines.Any(x =>
                            x.ReceivedQty > 0);

                    po.Status =
                        allPoLinesClosed
                            ? "FULLY_RECEIVED"
                            : anyReceived
                                ? "PARTIALLY_RECEIVED"
                                : "APPROVED";

                    po.UpdatedAt =
                        now;


                    // =========================================================
                    // 12. COMPLETE RMW PROCESSING
                    // =========================================================
                    processing.Status =
                        "COMPLETED";

                    processing.CompletedBy =
                        userId;

                    processing.CompletedAt =
                        now;

                    processing.UpdatedBy =
                        userId;

                    processing.UpdatedAt =
     now;


                    // =========================================================
                    // 13. UPDATE SUPPLIER PERFORMANCE EVALUATION
                    // =========================================================
                    await _supplierEvaluationGenerationService
                        .UpdateFromCommittedQcAsync(
                            qc,
                            rr,
                            po,
                            userId,
                            now
                        );


                    // =========================================================
                    // 14. SAVE EVERYTHING
                    // =========================================================
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();


                    return new CompleteFinalRrResultDto
                    {
                        RrId =
                            rr.RrId,

                        RrNo =
                            rr.RrNo
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }


        private async Task<string> GetUserNameAsync(
    string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return "";

            var name =
                await _context.Users
                    .AsNoTracking()
                    .Where(x =>
                        x.user_id == userId)
                    .Select(x =>
                        x.full_name)
                    .FirstOrDefaultAsync();

            return !string.IsNullOrWhiteSpace(name)
                ? name
                : userId;
        }

    }
}