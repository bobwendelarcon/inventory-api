using inventory_api.Data;
using inventory_api.DTOs.Purchasing.PurchaseOrders;
using inventory_api.DTOs.Purchasing.ReceivingReports;
using inventory_api.Models.Purchasing.PurchaseOrders;
using inventory_api.Models.Purchasing.QcInspections;
using inventory_api.Models.Purchasing.ReceivingReports;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.ReceivingReports
{
    public class ReceivingReportService
    {
        private readonly AppDbContext _context;

        public ReceivingReportService(AppDbContext context)
        {
            _context = context;
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
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                if (dto.ScheduleId <= 0)
                    throw new Exception("Delivery schedule is required.");

                if (dto.DeliveryDate == default)
                    throw new Exception("Actual delivery date is required.");

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
            return await _context.ReceivingReportHeaders
                .OrderByDescending(x => x.RrId)
                .Select(x => new ReceivingReportListDto
                {
                    RrId = x.RrId,
                    RrNo = x.RrNo,
                    PoNo = x.PoNo,
                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",
                    SiDrNo = x.SiDrNo,
                    DeliveryDate = x.DeliveryDate,
                    Status = x.Status,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ReceivingReportDetailsDto?> GetByIdAsync(int rrId)
        {
            return await _context.ReceivingReportHeaders
                .Where(x => x.RrId == rrId)
                .Select(x => new ReceivingReportDetailsDto
                {
                    RrId = x.RrId,
                    RrNo = x.RrNo,
                    PoId = x.PoId,
                    PoNo = x.PoNo,
                    SupplierId = x.SupplierId,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    SiDrNo = x.SiDrNo,
                    DeliveryDate = x.DeliveryDate,
                    Remarks = x.Remarks,
                    Status = x.Status,

                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,

                    QcBy = x.QcBy,
                    QcAt = x.QcAt,
                    CommittedBy = x.CommittedBy,
                    CommittedAt = x.CommittedAt,

                    Lines = x.Lines.Select(l => new ReceivingReportLineDetailsDto
                    {
                        RrLineId = l.RrLineId,
                        PoLineId = l.PoLineId,
                        MaterialId = l.MaterialId,

                        MaterialCode = _context.Materials
                            .Where(m => m.material_id == l.MaterialId)
                            .Select(m => m.material_code)
                            .FirstOrDefault() ?? "",

                        MaterialName = _context.Materials
                            .Where(m => m.material_id == l.MaterialId)
                            .Select(m => m.material_name)
                            .FirstOrDefault() ?? "",

                        PoQty = l.PoQty,
                        PreviouslyReceivedQty = l.PreviouslyReceivedQty,
                        BalanceQty = l.BalanceQty,
                        ReceiveQty = l.ReceiveQty,
                        AcceptedQty = l.AcceptedQty,

                        IsOverReceived = l.ReceiveQty > l.BalanceQty,

                        OverReceivedQty =
    l.ReceiveQty > l.BalanceQty
        ? l.ReceiveQty - l.BalanceQty
        : 0m,

                        RejectedQty = l.RejectedQty,
                        Uom = l.Uom,
                        Remarks = l.Remarks,
                        Status = l.Status
                    }).ToList()
                })
                .FirstOrDefaultAsync();
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
        schedule.Status == "RECEIVED" ||
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

                    Status = schedule.Status,

                    TotalAmount =

                        _context.PurchaseOrderHeaders

                        .Where(po => po.PoId == schedule.PoId)

                        .Select(po => po.TotalAmount)

                        .FirstOrDefault(),

                    MaterialCount = schedule.Lines.Count(),

                    TotalPoQty = schedule.Lines.Sum(x => x.ScheduledQty),

                    TotalReceivedQty = schedule.Lines.Sum(x => x.ReceivedQty),

                    TotalBalanceQty = schedule.Lines.Sum(x => x.BalanceQty)
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
                .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId);

            if (schedule == null)
                return null;

            var po = schedule.PurchaseOrder;

            if (po == null)
                throw new Exception(
                    "Purchase Order for this delivery schedule was not found."
                );

            var supplierName = await _context.Suppliers
                .Where(x => x.SupplierId == po.SupplierId)
                .Select(x => x.SupplierName)
                .FirstOrDefaultAsync() ?? "";

            var rr = await _context.ReceivingReportHeaders
                .Where(x => x.ScheduleId == schedule.ScheduleId)
                .OrderByDescending(x => x.RrId)
                .Select(x => new
                {
                    x.RrId,
                    x.RrNo,
                    x.Status
                })
                .FirstOrDefaultAsync();

            var qc = rr == null
                ? null
                : await _context.QcInspectionHeaders
                    .Where(x => x.RrId == rr.RrId)
                    .OrderByDescending(x => x.QcId)
                    .Select(x => new
                    {
                        x.QcNo,
                        x.Status,
                        x.Decision
                    })
                    .FirstOrDefaultAsync();

            var materialIds = schedule.Lines
                .Join(
                    po.Lines,
                    scheduleLine => scheduleLine.PoLineId,
                    poLine => poLine.PoLineId,
                    (scheduleLine, poLine) => poLine.MaterialId
                )
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
                .OrderBy(x => x.ScheduleLineId)
                .Select(scheduleLine =>
                {
                    var poLine = po.Lines.First(
                        x => x.PoLineId == scheduleLine.PoLineId
                    );

                    materials.TryGetValue(
                        poLine.MaterialId,
                        out var material
                    );

                    return new DeliveryScheduleLineDetailsDto
                    {
                        ScheduleLineId = scheduleLine.ScheduleLineId,

                        PoLineId = poLine.PoLineId,
                        MaterialId = poLine.MaterialId,

                        MaterialCode = material?.MaterialCode ?? "",
                        MaterialName = material?.MaterialName ?? "",

                        PoQty = poLine.PoQty,

                        ScheduledQty = scheduleLine.ScheduledQty,
                        ReceivedQty = scheduleLine.ReceivedQty,
                        RemainingQty = scheduleLine.BalanceQty,

                        Uom = poLine.Uom,

                        Status = scheduleLine.Status
                    };
                })
                .ToList();

            var hasRemaining =
                lines.Any(x => x.RemainingQty > 0);

            var hasExistingChildSchedule =
                await _context.PurchaseOrderDeliverySchedules
                    .AnyAsync(x =>
                        x.RescheduledFromScheduleId == schedule.ScheduleId &&
                        x.Status != "CANCELLED");

            return new DeliveryScheduleDetailsDto
            {
                ScheduleId = schedule.ScheduleId,
                ScheduleNo = schedule.ScheduleNo,

                PoId = po.PoId,
                PoNo = po.PoNo,
                PrintedPoNo = po.PrintedPoNo,

                ScheduledDate = schedule.ScheduledDate,
                ScheduleStatus = schedule.Status,

                SupplierId = po.SupplierId,
                SupplierName = supplierName,

                RescheduledFromScheduleId =
                    schedule.RescheduledFromScheduleId,

                Remarks = schedule.Remarks,

                TotalScheduledQty =
                    lines.Sum(x => x.ScheduledQty),

                TotalReceivedQty =
                    lines.Sum(x => x.ReceivedQty),

                TotalRemainingQty =
                    lines.Sum(x => x.RemainingQty),

                RrId = rr?.RrId,
                RrNo = rr?.RrNo,
                RrStatus = rr?.Status,

                QcNo = qc?.QcNo,
                QcStatus = qc?.Status,
                QcDecision = qc?.Decision,

                CanReschedule =
                    schedule.Status == "PARTIALLY_RECEIVED" &&
                    hasRemaining &&
                    !hasExistingChildSchedule,

                Lines = lines
            };
        }

        public async Task<List<int>> RescheduleRemainingAsync(
      int sourceScheduleId,
      RescheduleRemainingDeliveryDto dto,
      string userId)
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
        }


    }
}