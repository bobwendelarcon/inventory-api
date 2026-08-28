using inventory_api.Data;
using inventory_api.DTOs.Purchasing.QcInspections;
using inventory_api.DTOs.Purchasing.Quarantine;
using inventory_api.Models.Manufacturing.Materials;
using inventory_api.Models.Purchasing.QcInspections;
using inventory_api.Models.Purchasing.Quarantine;
using inventory_api.Models.Purchasing.RawMaterialProcessing;
using inventory_api.Models.Purchasing.ReceivingReports;
using inventory_api.Services.Purchasing.SupplierEvaluations;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.QcInspections
{
    public class QcInspectionService
    {
        private readonly AppDbContext _context;
        private readonly SupplierEvaluationGenerationService _supplierEvaluationGenerationService;

        public QcInspectionService(
            AppDbContext context,
            SupplierEvaluationGenerationService supplierEvaluationGenerationService)
        {
            _context = context;
            _supplierEvaluationGenerationService =
                supplierEvaluationGenerationService;
        }

        public async Task<List<QcInspectionListDto>> GetAllAsync()
        {
            return await _context.QcInspectionHeaders
     .OrderByDescending(x => x.QcId)
     .Select(x => new QcInspectionListDto
     {
         QcId = x.QcId,
         QcNo = x.QcNo,

         IncomingReceivingId =
             x.IncomingReceivingId,

         IncomingNo =
             x.IncomingNo,

         RrId =
             x.RrId,

         RrNo =
             x.RrNo,

         PoNo =
             x.PoNo,

         SupplierName =
             _context.Suppliers
                 .Where(s =>
                     s.SupplierId == x.SupplierId)
                 .Select(s =>
                     s.SupplierName)
                 .FirstOrDefault() ?? "",

         InspectionDate =
             x.InspectionDate,

         InspectorId =
             x.InspectorId,

         InspectorName =
             _context.Users
                 .Where(u =>
                     u.user_id == x.InspectorId)
                 .Select(u =>
                     u.full_name)
                 .FirstOrDefault()
             ?? x.InspectorId,

         Status =
             x.Status,

         Decision =
             x.Decision,

         CreatedAt =
             x.CreatedAt,


         // QUARANTINE
         QuarantineId =
             _context.QuarantineHeaders
                 .Where(q =>
                     q.QcId == x.QcId)
                 .Select(q =>
                     (int?)q.QuarantineId)
                 .FirstOrDefault(),

         QuarantineNo =
             _context.QuarantineHeaders
                 .Where(q =>
                     q.QcId == x.QcId)
                 .Select(q =>
                     q.QuarantineNo)
                 .FirstOrDefault(),

         QuarantineStatus =
             _context.QuarantineHeaders
                 .Where(q =>
                     q.QcId == x.QcId)
                 .Select(q =>
                     q.Status)
                 .FirstOrDefault()
     })
     .ToListAsync();
        }

        public async Task<QcInspectionDetailsDto?> GetByIdAsync(int qcId)
        {
            return await _context.QcInspectionHeaders
                .Where(x => x.QcId == qcId)
                .Select(x => new QcInspectionDetailsDto
                {
                    QcId = x.QcId,
                    QcNo = x.QcNo,

                    IncomingReceivingId =
                        x.IncomingReceivingId,

                    IncomingNo =
                        x.IncomingNo,

                    RrId =
                        x.RrId,

                    RrNo =
                        x.RrNo,

                    PoId =
                        x.PoId,

                    PoNo =
                        x.PoNo,

                    SupplierId =
                        x.SupplierId,

                    SupplierName =
                        _context.Suppliers
                            .Where(s =>
                                s.SupplierId == x.SupplierId)
                            .Select(s =>
                                s.SupplierName)
                            .FirstOrDefault() ?? "",

                    InspectionDate =
                        x.InspectionDate,

                    InspectorId =
                        x.InspectorId,

                    InspectorName =
                        _context.Users
                            .Where(u =>
                                u.user_id == x.InspectorId)
                            .Select(u =>
                                u.full_name)
                            .FirstOrDefault()
                        ?? x.InspectorId,

                    Status =
                        x.Status,

                    Decision =
                        x.Decision,

                    Remarks =
                        x.Remarks,

                    CreatedBy =
                        x.CreatedBy,

                    CreatedAt =
                        x.CreatedAt,

                    CommittedBy =
                        x.CommittedBy,

                    CommittedAt =
                        x.CommittedAt,


                    // ============================================================
                    // QUARANTINE / RELEASE INFORMATION
                    // ============================================================

                    QuarantineId =
                        _context.QuarantineHeaders
                            .Where(q =>
                                q.QcId == x.QcId)
                            .Select(q =>
                                (int?)q.QuarantineId)
                            .FirstOrDefault(),

                    QuarantineNo =
                        _context.QuarantineHeaders
                            .Where(q =>
                                q.QcId == x.QcId)
                            .Select(q =>
                                q.QuarantineNo)
                            .FirstOrDefault(),

                    QuarantineStatus =
                        _context.QuarantineHeaders
                            .Where(q =>
                                q.QcId == x.QcId)
                            .Select(q =>
                                q.Status)
                            .FirstOrDefault(),

                    QuarantineDecision =
                        _context.QuarantineHeaders
                            .Where(q =>
                                q.QcId == x.QcId)
                            .Select(q =>
                                q.Decision)
                            .FirstOrDefault(),

                    ReleasedBy =
                        _context.QuarantineHeaders
                            .Where(q =>
                                q.QcId == x.QcId)
                            .Select(q =>
                                q.ReleasedBy)
                            .FirstOrDefault(),

                    ReleasedAt =
                        _context.QuarantineHeaders
                            .Where(q =>
                                q.QcId == x.QcId)
                            .Select(q =>
                                q.ReleasedAt)
                            .FirstOrDefault(),


                    // ============================================================
                    // QC LINES
                    // ============================================================

                    Lines = x.Lines
                        .OrderBy(l =>
                            l.QcLineId)
                        .Select(l =>
                            new QcInspectionLineDetailsDto
                            {
                                QcLineId =
                                    l.QcLineId,

                                IncomingReceivingLineId =
                                    l.IncomingReceivingLineId,

                                RrLineId =
                                    l.RrLineId,

                                PoLineId =
                                    l.PoLineId,

                                MaterialId =
                                    l.MaterialId,

                                MaterialCode =
                                    _context.Materials
                                        .Where(m =>
                                            m.material_id ==
                                            l.MaterialId)
                                        .Select(m =>
                                            m.material_code)
                                        .FirstOrDefault() ?? "",

                                MaterialName =
                                    _context.Materials
                                        .Where(m =>
                                            m.material_id ==
                                            l.MaterialId)
                                        .Select(m =>
                                            m.material_name)
                                        .FirstOrDefault() ?? "",

                                IsLotTracked =
                                    _context.Materials
                                        .Where(m =>
                                            m.material_id ==
                                            l.MaterialId)
                                        .Select(m =>
                                            m.is_lot_tracked)
                                        .FirstOrDefault(),

                                ReceivedQty =
                                    l.ReceivedQty,

                                AcceptedQty =
                                    l.AcceptedQty,

                                RejectedQty =
                                    l.RejectedQty,

                                Remarks =
                                    l.Remarks,

                                Status =
                                    l.Status,

                                Lots = l.Lots
                                    .OrderBy(lot =>
                                        lot.QcLineLotId)
                                    .Select(lot =>
                                        new QcInspectionLineLotDetailsDto
                                        {
                                            QcLineLotId =
                                                lot.QcLineLotId,

                                            LotNo =
                                                lot.LotNo,

                                            ManufacturingDate =
                                                lot.ManufacturingDate,

                                            ExpirationDate =
                                                lot.ExpirationDate,

                                            ReceivedQty =
                                                lot.ReceivedQty,

                                            AcceptedQty =
                                                lot.AcceptedQty,

                                            RejectedQty =
                                                lot.RejectedQty,

                                            RejectionReason =
                                                lot.RejectionReason,

                                            Remarks =
                                                lot.Remarks,

                                            Status =
                                                lot.Status
                                        })
                                    .ToList()
                            })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task SaveInspectionAsync(
       int qcId,
       SaveQcInspectionDto dto,
       string userId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    // ============================================================
                    // LOAD QC
                    // ============================================================

                    var qc = await _context.QcInspectionHeaders
                        .Include(x => x.Lines)
                            .ThenInclude(x => x.Lots)
                        .FirstOrDefaultAsync(x => x.QcId == qcId);

                    if (qc == null)
                        throw new Exception("QC inspection not found.");

                    if (qc.Status != "FOR_INSPECTION")
                    {
                        throw new Exception(
                            "Only QC inspections with FOR_INSPECTION status can be processed.");
                    }

                    if (dto.Lines == null || !dto.Lines.Any())
                    {
                        throw new Exception(
                            "QC inspection must have at least one line.");
                    }

                    // ============================================================
                    // NEW WORKFLOW REQUIRES INCOMING RECEIVING
                    // ============================================================

                    if (!qc.IncomingReceivingId.HasValue)
                    {
                        throw new InvalidOperationException(
                            "QC inspection is not linked to an Incoming Receiving record.");
                    }

                    var incoming = await _context.IncomingReceivings
                        .Include(x => x.Lines)
                        .FirstOrDefaultAsync(x =>
                            x.IncomingReceivingId ==
                            qc.IncomingReceivingId.Value);

                    if (incoming == null)
                    {
                        throw new Exception(
                            "Incoming Receiving record was not found.");
                    }

                    // ============================================================
                    // VALIDATE ALL QC LINES WERE SUBMITTED
                    // ============================================================

                    var submittedQcLineIds = dto.Lines
                        .Select(x => x.QcLineId)
                        .ToHashSet();

                    var missingQcLines = qc.Lines
                        .Where(x =>
                            !submittedQcLineIds.Contains(x.QcLineId))
                        .Select(x => x.QcLineId)
                        .ToList();

                    if (missingQcLines.Any())
                    {
                        throw new InvalidOperationException(
                            "All QC lines must be inspected. Missing QC line IDs: " +
                            string.Join(", ", missingQcLines));
                    }

                    // ============================================================
                    // PREVENT DUPLICATE SUBMITTED QC LINES
                    // ============================================================

                    var duplicateQcLineId = dto.Lines
                        .GroupBy(x => x.QcLineId)
                        .FirstOrDefault(x => x.Count() > 1);

                    if (duplicateQcLineId != null)
                    {
                        throw new InvalidOperationException(
                            $"QC line {duplicateQcLineId.Key} was submitted more than once.");
                    }

                    // ============================================================
                    // INSPECTOR
                    // ============================================================

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        throw new Exception(
                            "Inspector ID is required.");
                    }

                    var inspectorId = userId.Trim();
                    var now = DateTime.Now;

                    // ============================================================
                    // LOAD MATERIAL TRACKING INFORMATION
                    // ============================================================

                    var materialIds = qc.Lines
                        .Select(x => x.MaterialId)
                        .Distinct()
                        .ToList();

                    var materialTracking = await _context.Materials
                        .Where(x =>
                            materialIds.Contains(x.material_id))
                        .Select(x => new
                        {
                            x.material_id,
                            x.material_name,
                            x.is_lot_tracked
                        })
                        .ToDictionaryAsync(
                            x => x.material_id,
                            x => x);

                    // ============================================================
                    // PROCESS EACH QC LINE
                    // ============================================================

                    foreach (var lineDto in dto.Lines)
                    {
                        var qcLine = qc.Lines
                            .FirstOrDefault(x =>
                                x.QcLineId == lineDto.QcLineId);

                        if (qcLine == null)
                        {
                            throw new Exception(
                                $"QC line {lineDto.QcLineId} was not found.");
                        }

                        // --------------------------------------------------------
                        // QC line must belong to an Incoming Receiving line
                        // --------------------------------------------------------

                        if (!qcLine.IncomingReceivingLineId.HasValue)
                        {
                            throw new InvalidOperationException(
                                $"QC line {qcLine.QcLineId} is not linked " +
                                "to an Incoming Receiving line.");
                        }

                        var incomingLine = incoming.Lines
                            .FirstOrDefault(x =>
                                x.IncomingReceivingLineId ==
                                qcLine.IncomingReceivingLineId.Value);

                        if (incomingLine == null)
                        {
                            throw new InvalidOperationException(
                                $"Incoming Receiving line " +
                                $"{qcLine.IncomingReceivingLineId.Value} " +
                                "was not found.");
                        }

                        // --------------------------------------------------------
                        // Material
                        // --------------------------------------------------------

                        if (!materialTracking.TryGetValue(
                            qcLine.MaterialId,
                            out var material))
                        {
                            throw new InvalidOperationException(
                                $"Material ID {qcLine.MaterialId} was not found.");
                        }

                        // --------------------------------------------------------
                        // Validate inspection quantities / lots
                        // --------------------------------------------------------

                        ValidateLots(
                            qcLine,
                            lineDto.Lots,
                            material.is_lot_tracked,
                            material.material_name);

                        // --------------------------------------------------------
                        // Save/update QC lots
                        // --------------------------------------------------------

                        SyncQcLineLots(
                            qcLine,
                            lineDto,
                            inspectorId,
                            now);

                        // --------------------------------------------------------
                        // Calculate QC result
                        // --------------------------------------------------------

                        qcLine.AcceptedQty =
                            lineDto.Lots.Sum(x => x.AcceptedQty);

                        qcLine.RejectedQty =
                            lineDto.Lots.Sum(x => x.RejectedQty);

                        qcLine.Remarks =
                            lineDto.Remarks;

                        qcLine.Status =
                            GetInspectionLineStatus(
                                qcLine.ReceivedQty,
                                qcLine.AcceptedQty,
                                qcLine.RejectedQty);

                        qcLine.UpdatedAt = now;

                        // --------------------------------------------------------
                        // IMPORTANT
                        //
                        // DO NOT:
                        // - update PO received quantity
                        // - update PO balance
                        // - create RR
                        // - post inventory
                        //
                        // Those happen AFTER QA/QC approval in the next stage.
                        // --------------------------------------------------------
                    }

                    // ============================================================
                    // CALCULATE HEADER RESULT
                    // ============================================================

                    var totalReceived =
                        qc.Lines.Sum(x => x.ReceivedQty);

                    var totalAccepted =
                        qc.Lines.Sum(x => x.AcceptedQty);

                    var totalRejected =
                        qc.Lines.Sum(x => x.RejectedQty);

                    qc.Decision =
                        GetHeaderDecision(
                            totalReceived,
                            totalAccepted,
                            totalRejected);

                    // ============================================================
                    // COMPLETE QC INSPECTION
                    // ============================================================

                    qc.Status = "INSPECTED";

                    qc.InspectionDate =
                        dto.InspectionDate ?? now;

                    qc.InspectorId =
                        inspectorId;

                    qc.Remarks =
                        dto.Remarks;

                    qc.UpdatedAt =
                        now;

                    // ============================================================
                    // UPDATE INCOMING RECEIVING STATUS
                    //
                    // Physical material remains in quarantine.
                    // Nothing enters inventory yet.
                    // ============================================================

                    incoming.ReceivingStatus =
                        "QC_INSPECTED";

                    // ============================================================
                    // SAVE
                    // ============================================================

                    await _context.SaveChangesAsync();


                    // ============================================================
                    // CREATE QUARANTINE RECORD
                    // ============================================================

                    var existingQuarantine =
                        await _context.QuarantineHeaders
                            .AnyAsync(x =>
                                x.QcId == qc.QcId);

                    if (!existingQuarantine)
                    {
                        var quarantineNo =
                            await GenerateQuarantineNoAsync();

                        var quarantine =
                            new QuarantineHeader
                            {
                                QuarantineNo = quarantineNo,

                                IncomingReceivingId =
                                    incoming.IncomingReceivingId,

                                QcId =
                                    qc.QcId,

                                PoId =
                                    qc.PoId,

                                SupplierId =
                                    qc.SupplierId,

                                Status =
                                    "WAITING_FOR_QA_RELEASE",

                                Decision =
                                    null,

                                Remarks =
                                    null,

                                CreatedBy =
                                    inspectorId,

                                CreatedAt =
                                    now
                            };

                        // --------------------------------------------------------
                        // CREATE QUARANTINE LINES PER QC LOT
                        // --------------------------------------------------------

                        foreach (var qcLine in qc.Lines)
                        {
                            foreach (var lot in qcLine.Lots)
                            {
                                // Only quantities accepted by QA/QC
                                // should enter quarantine/release workflow.
                                if (lot.AcceptedQty <= 0)
                                    continue;

                                quarantine.Lines.Add(
                                    new QuarantineLine
                                    {
                                        QcLineId =
                                            qcLine.QcLineId,

                                        QcLineLotId =
                                            lot.QcLineLotId,

                                        IncomingReceivingLineId =
                                            qcLine.IncomingReceivingLineId,

                                        PoLineId =
                                            qcLine.PoLineId,

                                        MaterialId =
                                            qcLine.MaterialId,

                                        LotNo =
                                            lot.LotNo,

                                        ManufacturingDate =
                                            lot.ManufacturingDate,

                                        ExpirationDate =
                                            lot.ExpirationDate,

                                        QcReceivedQty =
                                            lot.ReceivedQty,

                                        QcAcceptedQty =
                                            lot.AcceptedQty,

                                        QcRejectedQty =
                                            lot.RejectedQty,

                                        Status =
                                            "WAITING_FOR_QA_RELEASE",

                                        Remarks =
                                            lot.Remarks,

                                        CreatedAt =
                                            now
                                    }
                                );
                            }
                        }

                        _context.QuarantineHeaders.Add(
                            quarantine);
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }


        public async Task ReleaseQuarantineAsync(
      int quarantineId,
      string userId)
        {
            var strategy =
                _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    var quarantine =
                        await _context.QuarantineHeaders
                            .Include(x => x.Lines)
                            .FirstOrDefaultAsync(x =>
                                x.QuarantineId == quarantineId);

                    if (quarantine == null)
                    {
                        throw new InvalidOperationException(
                            "Quarantine record not found."
                        );
                    }

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        throw new InvalidOperationException(
                            "Released By user is required."
                        );
                    }

                    if (quarantine.Status == "RELEASED")
                    {
                        throw new InvalidOperationException(
                            "This material has already been released."
                        );
                    }

                    if (
                        quarantine.Status !=
                        "WAITING_FOR_QA_RELEASE"
                    )
                    {
                        throw new InvalidOperationException(
                            $"Material cannot be released. Current status: {quarantine.Status}"
                        );
                    }

                    var acceptedLines =
                        quarantine.Lines
                            .Where(x =>
                                x.QcAcceptedQty > 0)
                            .ToList();

                    if (!acceptedLines.Any())
                    {
                        throw new InvalidOperationException(
                            "No accepted quarantine lines are available for processing."
                        );
                    }

                    // ---------------------------------------------------------
                    // Prevent duplicate RMW processing
                    // ---------------------------------------------------------

                    var processingExists =
                        await _context.RmwProcessingHeaders
                            .AnyAsync(x =>
                                x.QuarantineId ==
                                quarantine.QuarantineId);

                    if (processingExists)
                    {
                        throw new InvalidOperationException(
                            "An RMW processing record already exists for this quarantine."
                        );
                    }

                    var now = DateTime.Now;
                    var cleanUserId = userId.Trim();

                    // ---------------------------------------------------------
                    // Generate RMP number
                    // ---------------------------------------------------------

                    var processingNo =
                        await GenerateRmwProcessingNoAsync();

                    // ---------------------------------------------------------
                    // Create RMW processing header
                    // ---------------------------------------------------------

                    var processing =
                        new RmwProcessingHeader
                        {
                            ProcessingNo =
                                processingNo,

                            QuarantineId =
                                quarantine.QuarantineId,

                            QcId =
                                quarantine.QcId,

                            IncomingReceivingId =
                                quarantine.IncomingReceivingId,

                            PoId =
                                quarantine.PoId,

                            SupplierId =
                                quarantine.SupplierId,

                            Status =
                                "READY_FOR_WEIGHING",

                            CreatedBy =
                                cleanUserId,

                            CreatedAt =
                                now
                        };

                    // ---------------------------------------------------------
                    // Create one processing line per accepted quarantine lot
                    // ---------------------------------------------------------

                    foreach (var quarantineLine in acceptedLines)
                    {
                        var uom =
                            await _context.PurchaseOrderLines
                                .Where(x =>
                                    x.PoLineId ==
                                    quarantineLine.PoLineId)
                                .Select(x => x.Uom)
                                .FirstOrDefaultAsync();

                        processing.Lines.Add(
                            new RmwProcessingLine
                            {
                                QuarantineLineId =
                                    quarantineLine.QuarantineLineId,

                                QcLineId =
                                    quarantineLine.QcLineId,

                                QcLineLotId =
                                    quarantineLine.QcLineLotId,

                                IncomingReceivingLineId =
                                    quarantineLine
                                        .IncomingReceivingLineId,

                                PoLineId =
                                    quarantineLine.PoLineId,

                                MaterialId =
                                    quarantineLine.MaterialId,

                                LotNo =
                                    quarantineLine.LotNo,

                                ManufacturingDate =
                                    quarantineLine
                                        .ManufacturingDate,

                                ExpirationDate =
                                    quarantineLine
                                        .ExpirationDate,

                                QaAcceptedQty =
                                    quarantineLine
                                        .QcAcceptedQty,

                                ActualQty =
                                    null,

                                VarianceQty =
                                    null,

                                Uom =
                                    uom,

                                Status =
                                    "READY_FOR_WEIGHING",

                                CreatedAt =
                                    now
                            }
                        );
                    }

                    _context.RmwProcessingHeaders.Add(
                        processing
                    );

                    // ---------------------------------------------------------
                    // Release quarantine
                    // ---------------------------------------------------------

                    quarantine.Status =
                        "RELEASED";

                    quarantine.Decision =
                        "RELEASED";

                    quarantine.ReleasedBy =
                        cleanUserId;

                    quarantine.ReleasedAt =
                        now;

                    quarantine.UpdatedBy =
                        cleanUserId;

                    quarantine.UpdatedAt =
                        now;

                    foreach (var line in acceptedLines)
                    {
                        line.Status =
                            "RELEASED";
                    }

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }


        private async Task<string>
    GenerateRmwProcessingNoAsync()
        {
            var year =
                DateTime.Now.Year;

            var prefix =
                $"RMP-{year}-";

            var lastNo =
                await _context.RmwProcessingHeaders
                    .Where(x =>
                        x.ProcessingNo
                            .StartsWith(prefix))
                    .OrderByDescending(x =>
                        x.ProcessingId)
                    .Select(x =>
                        x.ProcessingNo)
                    .FirstOrDefaultAsync();

            var nextNo = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var numberPart =
                    lastNo.Replace(
                        prefix,
                        ""
                    );

                if (
                    int.TryParse(
                        numberPart,
                        out var lastNumber
                    )
                )
                {
                    nextNo =
                        lastNumber + 1;
                }
            }

            return
                $"{prefix}{nextNo:0000}";
        }

        private async Task<string> GenerateQuarantineNoAsync()
        {
            var year =
                DateTime.Now.Year;

            var prefix =
                $"QT-{year}-";

            var lastNo =
                await _context.QuarantineHeaders
                    .Where(x =>
                        x.QuarantineNo.StartsWith(prefix))
                    .OrderByDescending(x =>
                        x.QuarantineId)
                    .Select(x =>
                        x.QuarantineNo)
                    .FirstOrDefaultAsync();

            var nextNo = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var numberPart =
                    lastNo.Replace(prefix, "");

                if (
                    int.TryParse(
                        numberPart,
                        out var lastNumber)
                )
                {
                    nextNo =
                        lastNumber + 1;
                }
            }

            return
                $"{prefix}{nextNo:0000}";
        }


        private static void ValidateLots(
      QcInspectionLine qcLine,
      IReadOnlyCollection<SaveQcInspectionLineLotDto>? lots,
      bool isLotTracked,
      string materialName)
        {
            if (lots == null || lots.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Inspection quantities are required for {materialName}.");
            }

            /*
             * Lot number rules only apply to lot-tracked materials.
             */
            if (isLotTracked)
            {
                var normalizedLotNumbers = lots
                    .Select(x => x.LotNo?.Trim().ToUpperInvariant())
                    .ToList();

                if (normalizedLotNumbers.Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidOperationException(
                        $"Lot number is required for {materialName}.");
                }

                if (normalizedLotNumbers.Distinct().Count() !=
                    normalizedLotNumbers.Count)
                {
                    throw new InvalidOperationException(
                        $"Duplicate lot numbers are not allowed for {materialName}.");
                }
            }

            foreach (var lot in lots)
            {
                var displayReference =
                    isLotTracked
                        ? lot.LotNo?.Trim()
                        : materialName;

                if (lot.ReceivedQty <= 0)
                {
                    throw new InvalidOperationException(
                        $"Received quantity must be greater than zero for {displayReference}.");
                }

                if (lot.AcceptedQty < 0 ||
                    lot.RejectedQty < 0)
                {
                    throw new InvalidOperationException(
                        $"Accepted and rejected quantities cannot be negative for {displayReference}.");
                }

                if (lot.AcceptedQty +
                    lot.RejectedQty !=
                    lot.ReceivedQty)
                {
                    throw new InvalidOperationException(
                        $"Accepted plus rejected quantity must equal received quantity for {displayReference}.");
                }

                if (isLotTracked &&
                    lot.ManufacturingDate.HasValue &&
                    lot.ExpirationDate.HasValue &&
                    lot.ExpirationDate.Value.Date <
                    lot.ManufacturingDate.Value.Date)
                {
                    throw new InvalidOperationException(
                        $"Expiration date cannot be earlier than manufacturing date for lot {lot.LotNo}.");
                }

                if (lot.RejectedQty > 0 &&
                    string.IsNullOrWhiteSpace(
                        lot.RejectionReason))
                {
                    throw new InvalidOperationException(
                        $"Rejection reason is required for {displayReference}.");
                }
            }

            var totalReceived =
                lots.Sum(x => x.ReceivedQty);

            if (totalReceived != qcLine.ReceivedQty)
            {
                throw new InvalidOperationException(
                    $"Total inspected quantity for {materialName} must equal " +
                    $"{qcLine.ReceivedQty}. Entered total: {totalReceived}.");
            }
        }


        private void SyncQcLineLots(
     QcInspectionLine qcLine,
     SaveQcInspectionLineDto lineDto,
     string userId,
     DateTime now)
        {
            var submittedLotIds = lineDto.Lots
                .Where(x => x.QcLineLotId.HasValue)
                .Select(x => x.QcLineLotId!.Value)
                .ToHashSet();

            // Remove lots deleted from the frontend.
            var lotsToRemove = qcLine.Lots
     .Where(x =>
         x.QcLineLotId > 0 &&
         !submittedLotIds.Contains(x.QcLineLotId))
     .ToList();

            if (lotsToRemove.Any())
            {
                _context.QcInspectionLineLots.RemoveRange(lotsToRemove);
            }





            foreach (var lotDto in lineDto.Lots)
            {
                QcInspectionLineLot lot;

                if (lotDto.QcLineLotId.HasValue)
                {
                    lot = qcLine.Lots.FirstOrDefault(
                        x => x.QcLineLotId == lotDto.QcLineLotId.Value
                    ) ?? throw new InvalidOperationException(
                        $"QC lot ID {lotDto.QcLineLotId.Value} was not found " +
                        $"under QC line {qcLine.QcLineId}."
                    );

                    lot.UpdatedAt = now;

                    // Change this if your UpdatedBy property is int.
                    lot.UpdatedBy = userId;
                }
                else
                {
                    lot = new QcInspectionLineLot
                    {
                        QcLineId = qcLine.QcLineId,
                        CreatedAt = now,

                        // Change this if your CreatedBy property is int.
                        CreatedBy = userId
                    };

                    qcLine.Lots.Add(lot);
                }

                lot.LotNo =
     string.IsNullOrWhiteSpace(lotDto.LotNo)
         ? $"NOLOT-MAT-{qcLine.MaterialId}-QCL-{qcLine.QcLineId}"
         : lotDto.LotNo.Trim().ToUpperInvariant();


                lot.ManufacturingDate = lotDto.ManufacturingDate?.Date;
                lot.ExpirationDate = lotDto.ExpirationDate?.Date;

                lot.ReceivedQty = lotDto.ReceivedQty;
                lot.AcceptedQty = lotDto.AcceptedQty;
                lot.RejectedQty = lotDto.RejectedQty;

                lot.RejectionReason =
                    string.IsNullOrWhiteSpace(lotDto.RejectionReason)
                        ? null
                        : lotDto.RejectionReason.Trim();

                lot.Remarks =
                    string.IsNullOrWhiteSpace(lotDto.Remarks)
                        ? null
                        : lotDto.Remarks.Trim();

                lot.Status = GetLotStatus(
                    lotDto.ReceivedQty,
                    lotDto.AcceptedQty,
                    lotDto.RejectedQty
                );
            }
        }


        private async Task AddAcceptedQtyToInventoryAsync(
       QcInspectionHeader qc,
       ReceivingReportHeader rr,
       QcInspectionLine qcLine,
       ReceivingReportLine rrLine,
       string userId,
       DateTime now)
        {
            if (string.IsNullOrWhiteSpace(rr.BranchId))
            {
                throw new InvalidOperationException(
                    $"Receiving Report {rr.RrNo} does not have a branch.");
            }

            var branchId = rr.BranchId.Trim();

            if (string.IsNullOrWhiteSpace(rrLine.Uom))
            {
                throw new InvalidOperationException(
                    $"UOM is missing for material ID {qcLine.MaterialId}.");
            }

            if (qcLine.Lots == null ||
                qcLine.Lots.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No QC lot information was found for material ID {qcLine.MaterialId}.");
            }

            // ---------------------------------------------------------
            // Determine whether this material is lot tracked.
            // ---------------------------------------------------------
            var material = await _context.Materials
                .AsNoTracking()
                .Where(x =>
                    x.material_id == qcLine.MaterialId)
                .Select(x => new
                {
                    x.material_id,
                    x.material_name,
                    x.is_lot_tracked
                })
                .FirstOrDefaultAsync();

            if (material == null)
            {
                throw new InvalidOperationException(
                    $"Material ID {qcLine.MaterialId} was not found.");
            }

            foreach (var qcLot in qcLine.Lots
                .Where(x => x.AcceptedQty > 0))
            {
                // -----------------------------------------------------
                // IMPORTANT:
                //
                // Lot-tracked:
                //     use actual lot number.
                //
                // Non-lot-tracked:
                //     always use ONE stable internal inventory key.
                //
                // Do NOT use QcLineId here because every QC would
                // create another inventory balance row.
                // -----------------------------------------------------

                string inventoryLotNo;

                if (material.is_lot_tracked)
                {
                    inventoryLotNo =
                        qcLot.LotNo?.Trim().ToUpperInvariant()
                        ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(inventoryLotNo))
                    {
                        throw new InvalidOperationException(
                            $"Lot number is required for lot-tracked material " +
                            $"{material.material_name}.");
                    }
                }
                else
                {
                    inventoryLotNo =
                        $"NON-LOT-MAT-{qcLine.MaterialId}";
                }

                // -----------------------------------------------------
                // Idempotency protection.
                //
                // Prevent same QC receipt from being posted twice.
                // -----------------------------------------------------
                var transactionExistsLocally =
                    _context.MaterialInventoryTransactions.Local.Any(x =>
                        x.material_id == qcLine.MaterialId &&
                        x.branch_id == branchId &&
                        x.lot_no == inventoryLotNo &&
                        x.transaction_type == "PURCHASE_RECEIPT" &&
                        x.reference_type == "QC" &&
                        x.reference_id == qc.QcId);

                var transactionExistsInDatabase =
                    await _context.MaterialInventoryTransactions
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.material_id == qcLine.MaterialId &&
                            x.branch_id == branchId &&
                            x.lot_no == inventoryLotNo &&
                            x.transaction_type == "PURCHASE_RECEIPT" &&
                            x.reference_type == "QC" &&
                            x.reference_id == qc.QcId);

                if (transactionExistsLocally ||
                    transactionExistsInDatabase)
                {
                    throw new InvalidOperationException(
                        $"QC {qc.QcNo}, material ID {qcLine.MaterialId} " +
                        $"has already been committed to inventory.");
                }

                // -----------------------------------------------------
                // Find existing inventory balance.
                //
                // NON-LOT material:
                // Same Material + Branch + NON-LOT key
                //
                // LOT material:
                // Same Material + Branch + actual Lot No.
                // -----------------------------------------------------
                var inventoryLot =
                    _context.MaterialLotNumbers.Local
                        .FirstOrDefault(x =>
                            x.material_id == qcLine.MaterialId &&
                            x.branch_id == branchId &&
                            x.lot_no == inventoryLotNo);

                inventoryLot ??=
                    await _context.MaterialLotNumbers
                        .FirstOrDefaultAsync(x =>
                            x.material_id == qcLine.MaterialId &&
                            x.branch_id == branchId &&
                            x.lot_no == inventoryLotNo);

                if (inventoryLot == null)
                {
                    inventoryLot = new MaterialLotNumber
                    {
                        material_id =
                            qcLine.MaterialId,

                        branch_id =
                            branchId,

                        lot_no =
                            inventoryLotNo,

                        // Dates only apply to actual tracked lots.
                        manufacturing_date =
                            material.is_lot_tracked
                                ? qcLot.ManufacturingDate
                                : null,

                        expiration_date =
                            material.is_lot_tracked
                                ? qcLot.ExpirationDate
                                : null,

                        quantity =
                            qcLot.AcceptedQty,

                        uom =
                            rrLine.Uom.Trim(),

                        supplier_id =
    material.is_lot_tracked
        ? qc.SupplierId
        : null,

                        remarks =
                            $"Accepted through QC {qc.QcNo}; RR {rr.RrNo}.",

                        is_active =
                            true,

                        created_at =
                            now,

                        updated_at =
                            null
                    };

                    await _context.MaterialLotNumbers
                        .AddAsync(inventoryLot);
                }
                else
                {
                    // -------------------------------------------------
                    // EXISTING INVENTORY:
                    // Add quantity instead of creating another row.
                    // -------------------------------------------------
                    inventoryLot.quantity +=
                        qcLot.AcceptedQty;

                    inventoryLot.is_active =
                        true;

                    inventoryLot.updated_at =
                        now;

                    if (material.is_lot_tracked)
                    {
                        inventoryLot.manufacturing_date ??=
                            qcLot.ManufacturingDate;

                        inventoryLot.expiration_date ??=
                            qcLot.ExpirationDate;
                    }

                    if (string.IsNullOrWhiteSpace(
                        inventoryLot.uom))
                    {
                        inventoryLot.uom =
                            rrLine.Uom.Trim();
                    }

                    /*
                     * Keep current supplier behavior.
                     *
                     * This means the inventory balance retains the
                     * supplier already associated with the balance.
                     *
                     * Purchase/QC transaction history still identifies
                     * each individual receipt.
                     */
                    if (material.is_lot_tracked)
                    {
                        inventoryLot.supplier_id ??=
                            qc.SupplierId;
                    }
                    else
                    {
                        inventoryLot.supplier_id = null;
                    }
                }

                // -----------------------------------------------------
                // ALWAYS create transaction history.
                //
                // Balance = one row for non-lot material.
                // Transactions = one record per QC receipt.
                // -----------------------------------------------------
                var inventoryTransaction =
     new MaterialInventoryTransaction
     {
         material_id =
             qcLine.MaterialId,

         branch_id =
             branchId,

         lot_no =
             inventoryLotNo,

         transaction_type =
             "PURCHASE_RECEIPT",

         quantity =
             qcLot.AcceptedQty,

         uom =
             rrLine.Uom.Trim(),

         supplier_id =
    qc.SupplierId,

         reference_type =
             "QC",

         reference_id =
             qc.QcId,

         reference_no =
             qc.QcNo,

         remarks =
             material.is_lot_tracked
                 ? $"Accepted inventory from RR {rr.RrNo}, " +
                   $"PO {qc.PoNo}, lot {inventoryLotNo}."
                 : $"Accepted inventory from RR {rr.RrNo}, " +
                   $"PO {qc.PoNo}. Non-lot-tracked material.",

         encoded_by =
             userId,

         transaction_date =
             now,

         created_at =
             now
     };

                await _context.MaterialInventoryTransactions
                    .AddAsync(inventoryTransaction);
            }
        }

        private static string GetLotStatus(
    decimal receivedQty,
    decimal acceptedQty,
    decimal rejectedQty)
        {
            if (acceptedQty == receivedQty)
                return "ACCEPTED";

            if (rejectedQty == receivedQty)
                return "REJECTED";

            return "PARTIALLY_ACCEPTED";
        }


        private static string GetInspectionLineStatus(
    decimal receivedQty,
    decimal acceptedQty,
    decimal rejectedQty)
        {
            if (acceptedQty == receivedQty)
                return "ACCEPTED";

            if (rejectedQty == receivedQty)
                return "REJECTED";

            return "PARTIALLY_ACCEPTED";
        }

        private static string GetHeaderDecision(
            decimal totalReceived,
            decimal totalAccepted,
            decimal totalRejected)
        {
            if (totalAccepted == totalReceived)
                return "ACCEPTED";

            if (totalRejected == totalReceived)
                return "REJECTED";

            return "PARTIALLY_ACCEPTED";
        }



        public async Task<List<RmwMaterialProcessingDto>>
    GetRmwMaterialProcessingAsync()
        {
            return await _context.QuarantineHeaders
                .OrderByDescending(x => x.QuarantineId)
                .Select(x => new RmwMaterialProcessingDto
                {
                    QuarantineId = x.QuarantineId,
                    QuarantineNo = x.QuarantineNo,

                    QcId = x.QcId,

                    QcNo = _context.QcInspectionHeaders
                        .Where(q => q.QcId == x.QcId)
                        .Select(q => q.QcNo)
                        .FirstOrDefault() ?? "",

                    IncomingReceivingId =
                        x.IncomingReceivingId,

                    IncomingNo = _context.IncomingReceivings
                        .Where(i =>
                            i.IncomingReceivingId ==
                            x.IncomingReceivingId)
                        .Select(i => i.IncomingNo)
                        .FirstOrDefault(),

                    SupplierName = _context.Suppliers
                        .Where(s =>
                            s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    Status = x.Status,
                    CreatedAt = x.CreatedAt,

                    Lines = x.Lines
                        .OrderBy(l => l.QuarantineLineId)
                        .Select(l =>
                            new RmwMaterialProcessingLineDto
                            {
                                QuarantineLineId =
                                    l.QuarantineLineId,

                                MaterialId =
                                    l.MaterialId,

                                MaterialCode =
                                    _context.Materials
                                        .Where(m =>
                                            m.material_id ==
                                            l.MaterialId)
                                        .Select(m =>
                                            m.material_code)
                                        .FirstOrDefault() ?? "",

                                MaterialName =
                                    _context.Materials
                                        .Where(m =>
                                            m.material_id ==
                                            l.MaterialId)
                                        .Select(m =>
                                            m.material_name)
                                        .FirstOrDefault() ?? "",

                                LotNo =
                                    l.LotNo,

                                ManufacturingDate =
                                    l.ManufacturingDate,

                                ExpirationDate =
                                    l.ExpirationDate,

                                AcceptedQty =
                                    l.QcAcceptedQty,

                                Status =
                                    l.Status
                            })
                        .ToList()
                })
                .ToListAsync();
        }

    }
}