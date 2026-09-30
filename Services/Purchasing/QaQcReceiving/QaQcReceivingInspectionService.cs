using inventory_api.Data;
using inventory_api.DTOs.Purchasing.QaQcReceiving;
using inventory_api.Models.Purchasing.QaQcReceiving;
using inventory_api.Models.Purchasing.Quarantine;
using inventory_api.Services.Purchasing.SupplierEvaluations;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.QaQcReceiving
{
    public class QaQcReceivingInspectionService
    {
        private readonly AppDbContext _db;
        private readonly SupplierEvaluationGenerationService
            _supplierEvaluationGenerationService;

        public QaQcReceivingInspectionService(
            AppDbContext db,
            SupplierEvaluationGenerationService supplierEvaluationGenerationService)
        {
            _db = db;
            _supplierEvaluationGenerationService =
                supplierEvaluationGenerationService;
        }


        // ============================================================
        // GENERATE QA/QC RECEIVING INSPECTION NUMBER
        // QAR-2026-0001
        // ============================================================
        public async Task<string> GenerateInspectionNoAsync()
        {
            var year = DateTime.Now.Year;

            var prefix = $"QAR-{year}-";

            var lastNo =
                await _db.QaQcReceivingInspections
                    .Where(x =>
                        x.InspectionNo.StartsWith(prefix))
                    .OrderByDescending(x =>
                        x.QaReceivingId)
                    .Select(x =>
                        x.InspectionNo)
                    .FirstOrDefaultAsync();

            var nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var numberPart =
                    lastNo.Replace(prefix, "");

                if (int.TryParse(
                    numberPart,
                    out var parsed))
                {
                    nextNumber =
                        parsed + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }


        // ============================================================
        // GET RMW RECEIVING DETAILS FOR QA/QC
        // ============================================================
        public async Task<QaQcReceivingInspectionDetailsDto?>
            GetIncomingDetailsAsync(
                int incomingReceivingId)
        {
            var incoming =
                await _db.IncomingReceivings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.IncomingReceivingId ==
                        incomingReceivingId);

            if (incoming == null)
                return null;


            // ========================================================
            // ONLY VALID RMW RECEIPTS CAN ENTER THIS STAGE
            // ========================================================

            if (incoming.ReceivingStatus !=
                "FOR_QA_QC_RECEIVING_INSPECTION")
            {
                throw new Exception(
                    $"Incoming receiving {incoming.IncomingNo} " +
                    $"cannot be inspected by QA/QC. " +
                    $"Current status: {incoming.ReceivingStatus}");
            }


            // ========================================================
            // PREVENT DUPLICATE QA/QC RECEIVING INSPECTION
            // ========================================================

            var existingInspection =
                await _db.QaQcReceivingInspections
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.IncomingReceivingId ==
                        incomingReceivingId);

            if (existingInspection != null)
            {
                throw new Exception(
                    $"Incoming receiving {incoming.IncomingNo} " +
                    $"already has QA/QC receiving inspection " +
                    $"{existingInspection.InspectionNo}.");
            }


            // ========================================================
            // PURCHASE ORDER
            // ========================================================

            var po =
                await _db.PurchaseOrderHeaders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.PoId == incoming.PoId);

            if (po == null)
                throw new Exception(
                    "Purchase Order not found.");


            // ========================================================
            // SUPPLIER
            // ========================================================

            var supplier =
                await _db.Suppliers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.SupplierId ==
                        incoming.SupplierId);

            if (supplier == null)
                throw new Exception(
                    "Supplier not found.");


            // ========================================================
            // RMW RECEIVING LINES
            // ========================================================

            var incomingLines =
                await _db.IncomingReceivingLines
                    .AsNoTracking()
                    .Where(x =>
                        x.IncomingReceivingId ==
                        incomingReceivingId)
                    .OrderBy(x =>
                        x.IncomingReceivingLineId)
                    .ToListAsync();

            if (incomingLines.Count == 0)
            {
                throw new Exception(
                    "No received materials were found.");
            }


            // ========================================================
            // MATERIALS
            // ========================================================

            var materialIds =
                incomingLines
                    .Select(x => x.MaterialId)
                    .Distinct()
                    .ToList();

            var materials =
      await _db.Materials
          .AsNoTracking()
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
                  x.material_name,

              IsLotTracked =
                  x.is_lot_tracked
          })
          .ToDictionaryAsync(
              x => x.MaterialId);

            // ========================================================
            // RMW LOTS
            // ========================================================

            var incomingLineIds =
                incomingLines
                    .Select(x =>
                        x.IncomingReceivingLineId)
                    .ToList();

            var lots =
                await _db.IncomingReceivingLineLots
                    .AsNoTracking()
                    .Where(x =>
                        incomingLineIds.Contains(
                            x.IncomingReceivingLineId))
                    .OrderBy(x =>
                        x.IncomingReceivingLineLotId)
                    .ToListAsync();


            // ========================================================
            // MANUFACTURERS
            // ========================================================

            var manufacturerIds =
                lots
                    .Where(x =>
                        x.ManufacturerId.HasValue)
                    .Select(x =>
                        x.ManufacturerId!.Value)
                    .Distinct()
                    .ToList();

            var manufacturers =
                await _db.Manufacturers
                    .AsNoTracking()
                    .Where(x =>
                        manufacturerIds.Contains(
                            x.ManufacturerId))
                    .Select(x => new
                    {
                        x.ManufacturerId,
                        x.ManufacturerName
                    })
                    .ToDictionaryAsync(
                        x => x.ManufacturerId);


            // ========================================================
            // BUILD RESPONSE
            // ========================================================

            var result =
                new QaQcReceivingInspectionDetailsDto
                {
                    IncomingReceivingId =
                        incoming.IncomingReceivingId,

                    IncomingNo =
                        incoming.IncomingNo,

                    PoId =
                        incoming.PoId,

                    PoNo =
                        po.PoNo ?? "",

                    SupplierId =
                        incoming.SupplierId,

                    SupplierName =
                        supplier.SupplierName ?? "",

                    DeliveryDate =
                        incoming.DeliveryDate,

                    ReceivingStatus =
                        incoming.ReceivingStatus,

                    // RMW receiver
                    ReceivedBy =
                        incoming.CreatedBy,

                    VerifiedBy =
                        incoming.VerifiedBy
                };


            // ========================================================
            // BUILD MATERIAL LINES
            // ========================================================

            foreach (var incomingLine in incomingLines)
            {
                materials.TryGetValue(
                    incomingLine.MaterialId,
                    out var material);

                var lineDto =
    new QaQcReceivingMaterialDto
    {
        IncomingReceivingLineId =
            incomingLine.IncomingReceivingLineId,

        PoLineId =
            incomingLine.PoLineId,

        MaterialId =
            incomingLine.MaterialId,

        MaterialCode =
            material?.MaterialCode ?? "",

        MaterialName =
            material?.MaterialName ?? "",

        IsLotTracked =
            material?.IsLotTracked ?? false,

        DeliveredQty =
            incomingLine.DeliveredQty,

        Uom =
            incomingLine.Uom,

        TareWeight =
            incomingLine.TareWeight
    };


                // ====================================================
                // LOTS FROM RMW MATERIAL RECEIVING FORM
                // ====================================================

                var lineLots =
                    lots
                        .Where(x =>
                            x.IncomingReceivingLineId ==
                            incomingLine
                                .IncomingReceivingLineId)
                        .ToList();

                foreach (var lot in lineLots)
                {
                    string? manufacturerName = null;

                    if (lot.ManufacturerId.HasValue &&
                        manufacturers.TryGetValue(
                            lot.ManufacturerId.Value,
                            out var manufacturer))
                    {
                        manufacturerName =
                            manufacturer.ManufacturerName;
                    }


                    lineDto.Lots.Add(
                        new QaQcReceivingLotDto
                        {
                            IncomingReceivingLineLotId =
                                lot.IncomingReceivingLineLotId,

                            ManufacturerId =
                                lot.ManufacturerId,

                            ManufacturerName =
                                manufacturerName,

                            LotNo =
                                lot.LotNo,

                            ManufacturingDate =
                                lot.ManufacturingDate,

                            ExpirationDate =
                                lot.ExpirationDate,

                            ItemCount =
                                lot.ItemCount,

                            Weight =
                                lot.Weight,

                            Remarks =
                                lot.Remarks
                        });
                }


                result.Lines.Add(lineDto);
            }


            return result;
        }

        public async Task<int> SaveInspectionAsync(
    SaveQaQcReceivingInspectionDto dto,
    string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "QA/QC inspector user ID is required.");
            }

            if (dto.IncomingReceivingId <= 0)
            {
                throw new InvalidOperationException(
                    "Incoming Receiving ID is required.");
            }

            if (dto.Lines == null || !dto.Lines.Any())
            {
                throw new InvalidOperationException(
                    "At least one inspection line is required.");
            }

            var strategy =
                _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _db.Database.BeginTransactionAsync();

                try
                {
                    var now = DateTime.Now;
                    var cleanUserId = userId.Trim();

                    // ============================================================
                    // LOAD INCOMING RECEIVING
                    // ============================================================

                    var incoming =
                        await _db.IncomingReceivings
                            .Include(x => x.Lines)
                            .FirstOrDefaultAsync(x =>
                                x.IncomingReceivingId ==
                                dto.IncomingReceivingId);

                    if (incoming == null)
                    {
                        throw new InvalidOperationException(
                            "Incoming Receiving record not found.");
                    }

                    if (incoming.ReceivingStatus !=
                        "FOR_QA_QC_RECEIVING_INSPECTION")
                    {
                        throw new InvalidOperationException(
                            $"Incoming receiving {incoming.IncomingNo} " +
                            $"cannot be processed by QA/QC. " +
                            $"Current status: {incoming.ReceivingStatus}");
                    }

                    // ============================================================
                    // PREVENT DUPLICATE INSPECTION
                    // ============================================================

                    var existingInspection =
                        await _db.QaQcReceivingInspections
                            .AnyAsync(x =>
                                x.IncomingReceivingId ==
                                incoming.IncomingReceivingId);

                    if (existingInspection)
                    {
                        throw new InvalidOperationException(
                            $"Incoming receiving {incoming.IncomingNo} " +
                            "already has a QA/QC Receiving Inspection.");
                    }

                    // ============================================================
                    // LOAD RECEIVING DETAIL / LOT ROWS
                    // ============================================================

                    var incomingLineIds =
                        incoming.Lines
                            .Select(x => x.IncomingReceivingLineId)
                            .ToList();

                    var receivingLots =
                        await _db.IncomingReceivingLineLots
                            .Where(x =>
                                incomingLineIds.Contains(
                                    x.IncomingReceivingLineId))
                            .ToListAsync();

                    // ============================================================
                    // VALIDATE SUBMITTED ROWS
                    // ============================================================

                    var duplicateRows =
                        dto.Lines
                            .GroupBy(x => new
                            {
                                x.IncomingReceivingLineId,
                                x.IncomingReceivingLineLotId
                            })
                            .FirstOrDefault(x => x.Count() > 1);

                    if (duplicateRows != null)
                    {
                        throw new InvalidOperationException(
                            "The same receiving detail was submitted more than once.");
                    }

                    foreach (var lineDto in dto.Lines)
                    {
                        var incomingLine =
                            incoming.Lines.FirstOrDefault(x =>
                                x.IncomingReceivingLineId ==
                                lineDto.IncomingReceivingLineId);

                        if (incomingLine == null)
                        {
                            throw new InvalidOperationException(
                                $"Incoming Receiving line " +
                                $"{lineDto.IncomingReceivingLineId} was not found.");
                        }

                        if (lineDto.IncomingReceivingLineLotId.HasValue)
                        {
                            var receivingLot =
                                receivingLots.FirstOrDefault(x =>
                                    x.IncomingReceivingLineLotId ==
                                    lineDto.IncomingReceivingLineLotId.Value &&
                                    x.IncomingReceivingLineId ==
                                    lineDto.IncomingReceivingLineId);

                            if (receivingLot == null)
                            {
                                throw new InvalidOperationException(
                                    $"Receiving detail ID " +
                                    $"{lineDto.IncomingReceivingLineLotId.Value} " +
                                    "does not belong to the selected receiving line.");
                            }
                        }
                    }

                    // ============================================================
                    // GENERATE INSPECTION NUMBER
                    // ============================================================

                    var inspectionNo =
                        await GenerateInspectionNoAsync();

                    // ============================================================
                    // CREATE QA/QC RECEIVING HEADER
                    // ============================================================

                    var inspection =
                        new QaQcReceivingInspection
                        {
                            InspectionNo =
                                inspectionNo,

                            IncomingReceivingId =
                                incoming.IncomingReceivingId,

                            PoId =
                                incoming.PoId,

                            SupplierId =
                                incoming.SupplierId,

                            InspectionDate =
                                dto.InspectionDate == default
                                    ? now
                                    : dto.InspectionDate,

                            Status =
                                "INSPECTED",

                            Remarks =
                                string.IsNullOrWhiteSpace(dto.Remarks)
                                    ? null
                                    : dto.Remarks.Trim(),

                            InspectedBy =
                                cleanUserId,

                            ReceivedBy =
                                string.IsNullOrWhiteSpace(dto.ReceivedBy)
                                    ? null
                                    : dto.ReceivedBy.Trim(),

                            NotedBy =
                                string.IsNullOrWhiteSpace(dto.NotedBy)
                                    ? null
                                    : dto.NotedBy.Trim(),

                            CreatedBy =
                                cleanUserId,

                            CreatedAt =
                                now
                        };

                    // ============================================================
                    // CREATE INSPECTION LINES
                    // ============================================================

                    foreach (var lineDto in dto.Lines)
                    {
                        var incomingLine =
                            incoming.Lines.First(x =>
                                x.IncomingReceivingLineId ==
                                lineDto.IncomingReceivingLineId);

                        var lineStatus =
                            DetermineQaReceivingLineStatus(lineDto);

                        inspection.Lines.Add(
                            new QaQcReceivingInspectionLine
                            {
                                IncomingReceivingLineId =
                                    lineDto.IncomingReceivingLineId,

                                IncomingReceivingLineLotId =
                                    lineDto.IncomingReceivingLineLotId,

                                MaterialId =
                                    incomingLine.MaterialId,

                                PackagingClean =
                                    lineDto.PackagingClean,

                                ReadableLabel =
                                    lineDto.ReadableLabel,

                                ProperlySealed =
                                    lineDto.ProperlySealed,

                                NoDeterioration =
                                    lineDto.NoDeterioration,

                                NoForeignMatter =
                                    lineDto.NoForeignMatter,

                                NoInfestation =
                                    lineDto.NoInfestation,

                                AppearanceNotApplicable =
                                    lineDto.AppearanceNotApplicable,

                                PowderNoLump =
                                    lineDto.PowderNoLump,

                                PowderGoodFlowability =
                                    lineDto.PowderGoodFlowability,

                                LiquidNoSolidification =
                                    lineDto.LiquidNoSolidification,

                                LiquidNoPrecipitation =
                                    lineDto.LiquidNoPrecipitation,

                                ColorResult =
                                    NormalizeInspectionResult(
                                        lineDto.ColorResult),

                                ActualColor =
                                    CleanNullable(lineDto.ActualColor),

                                OdorResult =
                                    NormalizeInspectionResult(
                                        lineDto.OdorResult),

                                ActualOdor =
                                    CleanNullable(lineDto.ActualOdor),

                                AssayResultStatus =
                                    NormalizeInspectionResult(
                                        lineDto.AssayResultStatus),

                                CoaAssayResult =
                                    CleanNullable(lineDto.CoaAssayResult),

                                BnpiAssaySpecification =
                                    CleanNullable(
                                        lineDto.BnpiAssaySpecification),

                                SolubilityResult =
                                    NormalizeInspectionResult(
                                        lineDto.SolubilityResult),

                                SolubilityTest =
                                    CleanNullable(lineDto.SolubilityTest),

                                Status =
                                    lineStatus,

                                Remarks =
                                    CleanNullable(lineDto.Remarks),

                                CreatedAt =
                                    now
                            });
                    }

                    // ============================================================
                    // DETERMINE OVERALL RESULT
                    // ============================================================

                    var hasFailedLine =
                        inspection.Lines.Any(x =>
                            x.Status == "FAILED");

                    if (hasFailedLine)
                    {
                        inspection.Status =
                            "FAILED";

                        incoming.ReceivingStatus =
                            "QA_QC_RECEIVING_FAILED";

                        incoming.UpdatedBy =
                            cleanUserId;

                        incoming.UpdatedAt =
                            now;

                        _db.QaQcReceivingInspections.Add(
                            inspection);

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return inspection.QaReceivingId;
                    }

                    // ============================================================
                    // PASSED - SAVE INSPECTION FIRST
                    // ============================================================

                    inspection.Status =
                        "PASSED";

                    _db.QaQcReceivingInspections.Add(
                        inspection);

                    await _db.SaveChangesAsync();

                    // ============================================================
                    // CREATE QUARANTINE HEADER
                    // ============================================================

                    var quarantineExists =
                        await _db.QuarantineHeaders
                            .AnyAsync(x =>
                                x.IncomingReceivingId ==
                                incoming.IncomingReceivingId);

                    if (quarantineExists)
                    {
                        throw new InvalidOperationException(
                            "A quarantine record already exists for this receiving.");
                    }

                    var quarantineNo =
                        await GenerateQuarantineNoAsync();

                    var quarantine =
                        new QuarantineHeader
                        {
                            QuarantineNo =
                                quarantineNo,

                            IncomingReceivingId =
                                incoming.IncomingReceivingId,

                            QaReceivingId =
                                inspection.QaReceivingId,

                            QcId =
                                null,

                            PoId =
                                incoming.PoId,

                            SupplierId =
                                incoming.SupplierId,

                            Status =
                                "QUARANTINED",

                            Decision =
                                null,

                            Remarks =
                                null,

                            CreatedBy =
                                cleanUserId,

                            CreatedAt =
                                now
                        };

                    // ============================================================
                    // CREATE QUARANTINE LINES
                    // ============================================================

                    foreach (var inspectionLine in inspection.Lines)
                    {
                        var incomingLine =
                            incoming.Lines.First(x =>
                                x.IncomingReceivingLineId ==
                                inspectionLine.IncomingReceivingLineId);

                        var receivingLot =
                            inspectionLine.IncomingReceivingLineLotId.HasValue
                                ? receivingLots.FirstOrDefault(x =>
                                    x.IncomingReceivingLineLotId ==
                                    inspectionLine.IncomingReceivingLineLotId.Value)
                                : null;

                        quarantine.Lines.Add(
                            new QuarantineLine
                            {
                                QaReceivingLineId =
                                    inspectionLine.QaReceivingLineId,

                                IncomingReceivingLineId =
                                    inspectionLine.IncomingReceivingLineId,

                                IncomingReceivingLineLotId =
                                    inspectionLine
                                        .IncomingReceivingLineLotId,

                                QcLineId =
                                    null,

                                QcLineLotId =
                                    null,

                                PoLineId =
                                    incomingLine.PoLineId,

                                MaterialId =
                                    incomingLine.MaterialId,

                                LotNo =
                                    receivingLot?.LotNo,

                                ManufacturingDate =
                                    receivingLot?.ManufacturingDate,

                                ExpirationDate =
                                    receivingLot?.ExpirationDate,

                                QcReceivedQty =
                                    receivingLot?.Weight ??
                                    incomingLine.DeliveredQty,

                                QcAcceptedQty =
                                    0,

                                QcRejectedQty =
                                    0,

                                Status =
                                    "QUARANTINED",

                                Remarks =
                                    receivingLot?.Remarks,

                                CreatedAt =
                                    now
                            });
                    }

                    _db.QuarantineHeaders.Add(
                        quarantine);

                    // ============================================================
                    // UPDATE INCOMING RECEIVING
                    // ============================================================

                    incoming.ReceivingStatus =
      "QUARANTINED";

                    incoming.UpdatedBy =
                        cleanUserId;

                    incoming.UpdatedAt =
                        now;

                    // ============================================================
                    // UPDATE SUPPLIER EVALUATION WORKFLOW
                    // QA/QC RECEIVING PASSED -> QUARANTINED
                    // ============================================================

                    await _supplierEvaluationGenerationService
                        .UpdateWorkflowStageAsync(
                            incoming.PoId,
                            "QUARANTINED",
                            "QA_QC_RECEIVING_PASSED",
                            cleanUserId,
                            now,
                            $"QA/QC Receiving Inspection {inspection.InspectionNo} passed. " +
                            $"Material placed in quarantine {quarantine.QuarantineNo}."
                        );

                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return inspection.QaReceivingId;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<object> GetPendingAsync()
        {
            var incoming =
                await _db.IncomingReceivings
                    .AsNoTracking()
                    .Where(x =>
                        x.ReceivingStatus ==
                        "FOR_QA_QC_RECEIVING_INSPECTION")
                    .OrderBy(x => x.DeliveryDate)
                    .ThenBy(x => x.IncomingReceivingId)
                    .ToListAsync();

            var poIds =
                incoming
                    .Select(x => x.PoId)
                    .Distinct()
                    .ToList();

            var supplierIds =
                incoming
                    .Select(x => x.SupplierId)
                    .Distinct()
                    .ToList();

            var pos =
                await _db.PurchaseOrderHeaders
                    .AsNoTracking()
                    .Where(x =>
                        poIds.Contains(x.PoId))
                    .Select(x => new
                    {
                        x.PoId,
                        x.PoNo
                    })
                    .ToDictionaryAsync(
                        x => x.PoId);

            var suppliers =
                await _db.Suppliers
                    .AsNoTracking()
                    .Where(x =>
                        supplierIds.Contains(
                            x.SupplierId))
                    .Select(x => new
                    {
                        x.SupplierId,
                        x.SupplierName
                    })
                    .ToDictionaryAsync(
                        x => x.SupplierId);

            var result =
                incoming.Select(x =>
                {
                    pos.TryGetValue(
                        x.PoId,
                        out var po);

                    suppliers.TryGetValue(
                        x.SupplierId,
                        out var supplier);

                    return new
                    {
                        incomingReceivingId =
                            x.IncomingReceivingId,

                        incomingNo =
                            x.IncomingNo,

                        poId =
                            x.PoId,

                        poNo =
                            po?.PoNo ?? "",

                        supplierId =
                            x.SupplierId,

                        supplierName =
                            supplier?.SupplierName ?? "",

                        deliveryDate =
                            x.DeliveryDate,

                        receivedBy =
                            x.CreatedBy,

                        verifiedBy =
                            x.VerifiedBy,

                        status =
                            x.ReceivingStatus
                    };
                })
                .ToList();

            return result;
        }
        public async Task<object> GetHistoryAsync()
        {
            var inspections =
                await _db.QaQcReceivingInspections
                    .AsNoTracking()
                    .OrderByDescending(x => x.InspectionDate)
                    .ThenByDescending(x => x.QaReceivingId)
                    .ToListAsync();

            if (inspections.Count == 0)
            {
                return new List<object>();
            }

            var incomingIds =
                inspections
                    .Select(x => x.IncomingReceivingId)
                    .Distinct()
                    .ToList();

            var poIds =
                inspections
                    .Select(x => x.PoId)
                    .Distinct()
                    .ToList();

            var supplierIds =
                inspections
                    .Select(x => x.SupplierId)
                    .Distinct()
                    .ToList();


            var incoming =
                await _db.IncomingReceivings
                    .AsNoTracking()
                    .Where(x =>
                        incomingIds.Contains(
                            x.IncomingReceivingId))
                    .ToDictionaryAsync(
                        x => x.IncomingReceivingId);


            var pos =
                await _db.PurchaseOrderHeaders
                    .AsNoTracking()
                    .Where(x =>
                        poIds.Contains(x.PoId))
                    .Select(x => new
                    {
                        x.PoId,
                        x.PoNo
                    })
                    .ToDictionaryAsync(
                        x => x.PoId);


            var suppliers =
                await _db.Suppliers
                    .AsNoTracking()
                    .Where(x =>
                        supplierIds.Contains(
                            x.SupplierId))
                    .Select(x => new
                    {
                        x.SupplierId,
                        x.SupplierName
                    })
                    .ToDictionaryAsync(
                        x => x.SupplierId);


            var result =
                inspections.Select(x =>
                {
                    incoming.TryGetValue(
                        x.IncomingReceivingId,
                        out var incomingRecord);

                    pos.TryGetValue(
                        x.PoId,
                        out var po);

                    suppliers.TryGetValue(
                        x.SupplierId,
                        out var supplier);

                    return new
                    {
                        qaReceivingId =
                            x.QaReceivingId,

                        inspectionNo =
                            x.InspectionNo,

                        incomingReceivingId =
                            x.IncomingReceivingId,

                        incomingNo =
                            incomingRecord?.IncomingNo ?? "",

                        poId =
                            x.PoId,

                        poNo =
                            po?.PoNo ?? "",

                        supplierId =
                            x.SupplierId,

                        supplierName =
                            supplier?.SupplierName ?? "",

                        deliveryDate =
                            incomingRecord?.DeliveryDate,

                        inspectionDate =
                            x.InspectionDate,

                        inspectedBy =
                            x.InspectedBy,

                        receivedBy =
                            x.ReceivedBy,

                        notedBy =
                            x.NotedBy,

                        status =
                            x.Status,

                        currentReceivingStatus =
                            incomingRecord?.ReceivingStatus ?? "",

                        remarks =
                            x.Remarks
                    };
                })
                .ToList();

            return result;
        }

        private static string DetermineQaReceivingLineStatus(
    SaveQaQcReceivingInspectionLineDto line)
        {
            if (!line.PackagingClean ||
                !line.ReadableLabel ||
                !line.ProperlySealed ||
                !line.NoDeterioration)
            {
                return "FAILED";
            }

            if (!line.AppearanceNotApplicable)
            {
                if (!line.NoForeignMatter ||
                    !line.NoInfestation)
                {
                    return "FAILED";
                }

                if (line.PowderNoLump.HasValue &&
                    !line.PowderNoLump.Value)
                {
                    return "FAILED";
                }

                if (line.PowderGoodFlowability.HasValue &&
                    !line.PowderGoodFlowability.Value)
                {
                    return "FAILED";
                }

                if (line.LiquidNoSolidification.HasValue &&
                    !line.LiquidNoSolidification.Value)
                {
                    return "FAILED";
                }

                if (line.LiquidNoPrecipitation.HasValue &&
                    !line.LiquidNoPrecipitation.Value)
                {
                    return "FAILED";
                }
            }

            var controlledResults =
                new[]
                {
            line.ColorResult,
            line.OdorResult,
            line.AssayResultStatus,
            line.SolubilityResult
                };

            if (controlledResults.Any(x =>
                string.Equals(
                    NormalizeInspectionResult(x),
                    "NON_COMPLIANT",
                    StringComparison.OrdinalIgnoreCase)))
            {
                return "FAILED";
            }

            return "PASSED";
        }


        private static string? NormalizeInspectionResult(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var result =
                value.Trim()
                    .ToUpperInvariant()
                    .Replace(" ", "_")
                    .Replace("-", "_");

            if (result == "NA" ||
                result == "N/A")
            {
                return "N/A";
            }

            return result;
        }


        private static string? CleanNullable(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }


        private async Task<string> GenerateQuarantineNoAsync()
        {
            var year =
                DateTime.Now.Year;

            var prefix =
                $"QT-{year}-";

            var lastNo =
                await _db.QuarantineHeaders
                    .Where(x =>
                        x.QuarantineNo.StartsWith(prefix))
                    .OrderByDescending(x =>
                        x.QuarantineId)
                    .Select(x =>
                        x.QuarantineNo)
                    .FirstOrDefaultAsync();

            var nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var numberPart =
                    lastNo.Replace(prefix, "");

                if (int.TryParse(
                    numberPart,
                    out var parsed))
                {
                    nextNumber =
                        parsed + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }


        public async Task<object?> GetHistoryDetailsAsync(
    int qaReceivingId)
        {
            var inspection =
                await _db.QaQcReceivingInspections
                    .AsNoTracking()
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.QaReceivingId == qaReceivingId);

            if (inspection == null)
                return null;


            // ============================================================
            // INCOMING RECEIVING
            // ============================================================

            var incoming =
                await _db.IncomingReceivings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.IncomingReceivingId ==
                        inspection.IncomingReceivingId);


            // ============================================================
            // PURCHASE ORDER
            // ============================================================

            var po =
                await _db.PurchaseOrderHeaders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.PoId == inspection.PoId);


            // ============================================================
            // SUPPLIER
            // ============================================================

            var supplier =
                await _db.Suppliers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.SupplierId == inspection.SupplierId);


            // ============================================================
            // BUILD INSPECTION LINES
            // ============================================================

            var detailLines = new List<object>();


            foreach (var line in inspection.Lines
                .OrderBy(x => x.QaReceivingLineId))
            {
                // --------------------------------------------------------
                // RECEIVING LINE
                // --------------------------------------------------------

                var receivingLine =
                    await _db.IncomingReceivingLines
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.IncomingReceivingLineId ==
                            line.IncomingReceivingLineId);


                // --------------------------------------------------------
                // MATERIAL
                // --------------------------------------------------------

                var material =
                    await _db.Materials
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.material_id ==
                            line.MaterialId);


                // --------------------------------------------------------
                // LOT
                // --------------------------------------------------------

                var lot =
                    line.IncomingReceivingLineLotId.HasValue
                        ? await _db.IncomingReceivingLineLots
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.IncomingReceivingLineLotId ==
                                line.IncomingReceivingLineLotId.Value)
                        : null;


                // --------------------------------------------------------
                // MANUFACTURER
                // --------------------------------------------------------

                string? manufacturerName = null;

                if (lot?.ManufacturerId != null)
                {
                    manufacturerName =
                        await _db.Manufacturers
                            .AsNoTracking()
                            .Where(x =>
                                x.ManufacturerId ==
                                lot.ManufacturerId.Value)
                            .Select(x =>
                                x.ManufacturerName)
                            .FirstOrDefaultAsync();
                }


                detailLines.Add(new
                {
                    line.QaReceivingLineId,

                    line.IncomingReceivingLineId,
                    line.IncomingReceivingLineLotId,

                    line.MaterialId,

                    materialCode =
                        material?.material_code ?? "",

                    materialName =
                        material?.material_name ?? "",








                    // ====================================================
                    // RECEIVING INFORMATION
                    // ====================================================

                    scheduledQty =
                        receivingLine?.ScheduledQty ?? 0,

                    deliveredQty =
                        receivingLine?.DeliveredQty ?? 0,

                    tareWeight =
                        receivingLine?.TareWeight,

                    uom =
                        receivingLine?.Uom ?? "",


                    // ====================================================
                    // LOT INFORMATION
                    // ====================================================

                    manufacturerId =
    lot?.ManufacturerId,

                    manufacturerName =
    manufacturerName,

                    lotNo =
    lot?.LotNo,

                    manufacturingDate =
                        lot?.ManufacturingDate,

                    expirationDate =
                        lot?.ExpirationDate,

                    itemCount =
                        lot?.ItemCount,

                    weight =
                        lot?.Weight,

                    lotRemarks =
                        lot?.Remarks,


                    // ====================================================
                    // PACKAGING
                    // ====================================================

                    packagingClean =
                        line.PackagingClean,

                    readableLabel =
                        line.ReadableLabel,

                    properlySealed =
                        line.ProperlySealed,

                    noDeterioration =
                        line.NoDeterioration,


                    // ====================================================
                    // RAW MATERIAL APPEARANCE
                    // ====================================================

                    appearanceNotApplicable =
                        line.AppearanceNotApplicable,

                    noForeignMatter =
                        line.NoForeignMatter,

                    noInfestation =
                        line.NoInfestation,

                    powderNoLump =
                        line.PowderNoLump,

                    powderGoodFlowability =
                        line.PowderGoodFlowability,

                    liquidNoSolidification =
                        line.LiquidNoSolidification,

                    liquidNoPrecipitation =
                        line.LiquidNoPrecipitation,


                    // ====================================================
                    // COLOR
                    // ====================================================

                    colorResult =
                        line.ColorResult,

                    actualColor =
                        line.ActualColor,


                    // ====================================================
                    // ODOR
                    // ====================================================

                    odorResult =
                        line.OdorResult,

                    actualOdor =
                        line.ActualOdor,


                    // ====================================================
                    // ASSAY
                    // ====================================================

                    assayResultStatus =
                        line.AssayResultStatus,

                    coaAssayResult =
                        line.CoaAssayResult,

                    bnpiAssaySpecification =
                        line.BnpiAssaySpecification,


                    // ====================================================
                    // SOLUBILITY
                    // ====================================================

                    solubilityResult =
                        line.SolubilityResult,

                    solubilityTest =
                        line.SolubilityTest,


                    // ====================================================
                    // RESULT
                    // ====================================================

                    status =
                        line.Status,

                    remarks =
                        line.Remarks
                });
            }


            // ============================================================
            // FINAL RESPONSE
            // ============================================================

            return new
            {
                inspection.QaReceivingId,

                inspectionNo =
                    inspection.InspectionNo,

                inspectionDate =
                    inspection.InspectionDate,


                // ========================================================
                // INCOMING RECEIVING
                // ========================================================

                incomingReceivingId =
                    inspection.IncomingReceivingId,

                incomingNo =
                    incoming?.IncomingNo ?? "",

                deliveryDate =
                    incoming?.DeliveryDate,

                siDrNo =
                    incoming?.SiDrNo,

                currentReceivingStatus =
                    incoming?.ReceivingStatus ?? "",


                // ========================================================
                // PURCHASE ORDER
                // ========================================================

                poId =
                    inspection.PoId,

                poNo =
                    po?.PoNo ?? "",

                printedPoNo =
                    po?.PrintedPoNo ?? "",


                // ========================================================
                // SUPPLIER
                // ========================================================

                supplierId =
                    inspection.SupplierId,

                supplierName =
                    supplier?.SupplierName ?? "",


                // ========================================================
                // INSPECTION
                // ========================================================

                status =
                    inspection.Status,

                remarks =
                    inspection.Remarks,

                inspectedBy =
                    inspection.InspectedBy,

                receivedBy =
                    inspection.ReceivedBy,

                notedBy =
                    inspection.NotedBy,

                createdBy =
                    inspection.CreatedBy,

                createdAt =
                    inspection.CreatedAt,


                // ========================================================
                // MATERIAL INSPECTIONS
                // ========================================================

                lines =
                    detailLines
            };
        }




    }
}