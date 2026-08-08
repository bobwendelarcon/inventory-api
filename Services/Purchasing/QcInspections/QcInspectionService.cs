using inventory_api.Data;
using inventory_api.DTOs.Purchasing.QcInspections;
using inventory_api.Models.Manufacturing.Materials;
using inventory_api.Models.Purchasing.QcInspections;
using inventory_api.Models.Purchasing.ReceivingReports;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.QcInspections
{
    public class QcInspectionService
    {
        private readonly AppDbContext _context;

        public QcInspectionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<QcInspectionListDto>> GetAllAsync()
        {
            return await _context.QcInspectionHeaders
                .OrderByDescending(x => x.QcId)
                .Select(x => new QcInspectionListDto
                {
                    QcId = x.QcId,
                    QcNo = x.QcNo,
                    RrId = x.RrId,
                    RrNo = x.RrNo,
                    PoNo = x.PoNo,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    InspectionDate = x.InspectionDate,
                    InspectorId = x.InspectorId,
                    Status = x.Status,
                    Decision = x.Decision,
                    CreatedAt = x.CreatedAt
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
                    RrId = x.RrId,
                    RrNo = x.RrNo,
                    PoId = x.PoId,
                    PoNo = x.PoNo,
                    SupplierId = x.SupplierId,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    InspectionDate = x.InspectionDate,
                    InspectorId = x.InspectorId,
                    Status = x.Status,
                    Decision = x.Decision,
                    Remarks = x.Remarks,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CommittedBy = x.CommittedBy,
                    CommittedAt = x.CommittedAt,

                    Lines = x.Lines
    .OrderBy(l => l.QcLineId)
    .Select(l => new QcInspectionLineDetailsDto
    {
        QcLineId = l.QcLineId,
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

        IsLotTracked = _context.Materials
    .Where(m => m.material_id == l.MaterialId)
    .Select(m => m.is_lot_tracked)
    .FirstOrDefault(),

        ReceivedQty = l.ReceivedQty,
        AcceptedQty = l.AcceptedQty,
        RejectedQty = l.RejectedQty,
        Remarks = l.Remarks,
        Status = l.Status,

        Lots = l.Lots
            .OrderBy(lot => lot.QcLineLotId)
            .Select(lot => new QcInspectionLineLotDetailsDto
            {
                QcLineLotId = lot.QcLineLotId,
                LotNo = lot.LotNo,
                ManufacturingDate = lot.ManufacturingDate,
                ExpirationDate = lot.ExpirationDate,
                ReceivedQty = lot.ReceivedQty,
                AcceptedQty = lot.AcceptedQty,
                RejectedQty = lot.RejectedQty,
                RejectionReason = lot.RejectionReason,
                Remarks = lot.Remarks,
                Status = lot.Status
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
                    var qc = await _context.QcInspectionHeaders
     .Include(x => x.Lines)
         .ThenInclude(x => x.Lots)
     .FirstOrDefaultAsync(x => x.QcId == qcId);

                if (qc == null)
                    throw new Exception("QC inspection not found.");

                if (qc.Status != "FOR_INSPECTION")
                    throw new Exception(
                        "Only QC inspections with FOR_INSPECTION status can be processed."
                    );

                if (dto.Lines == null || !dto.Lines.Any())
                    throw new Exception("QC inspection must have at least one line.");

                var submittedQcLineIds = dto.Lines
    .Select(x => x.QcLineId)
    .ToHashSet();

                var missingQcLines = qc.Lines
                    .Where(x => !submittedQcLineIds.Contains(x.QcLineId))
                    .Select(x => x.QcLineId)
                    .ToList();

                if (missingQcLines.Any())
                {
                    throw new InvalidOperationException(
                        $"All QC lines must be inspected. Missing QC line IDs: " +
                        string.Join(", ", missingQcLines));
                }

                var duplicateQcLineId = dto.Lines
    .GroupBy(x => x.QcLineId)
    .FirstOrDefault(x => x.Count() > 1);

                if (duplicateQcLineId != null)
                {
                    throw new InvalidOperationException(
                        $"QC line {duplicateQcLineId.Key} was submitted more than once.");
                }



                var rr = await _context.ReceivingReportHeaders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.RrId == qc.RrId);

                if (rr == null)
                    throw new Exception("Receiving Report not found.");

                if (rr.Status != "FOR_QC")
                    throw new Exception(
                        "Only Receiving Reports submitted for QC can be inspected."
                    );

                var po = await _context.PurchaseOrderHeaders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.PoId == qc.PoId);

                if (po == null)
                    throw new Exception("Purchase Order not found.");

                var now = DateTime.Now;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new Exception("Inspector ID is required.");
                }

                var inspectorId = userId.Trim();

                var materialIds = qc.Lines
    .Select(x => x.MaterialId)
    .Distinct()
    .ToList();

                var materialTracking = await _context.Materials
                    .Where(x => materialIds.Contains(x.material_id))
                    .Select(x => new
                    {
                        x.material_id,
                        x.material_name,
                        x.is_lot_tracked
                    })
                    .ToDictionaryAsync(
                        x => x.material_id,
                        x => x
                    );

                foreach (var lineDto in dto.Lines)
                {
                    var qcLine = qc.Lines.FirstOrDefault(
                        x => x.QcLineId == lineDto.QcLineId
                    );

                    if (qcLine == null)
                    {
                        throw new Exception(
                            $"QC line {lineDto.QcLineId} was not found."
                        );
                    }


                    if (!materialTracking.TryGetValue(
        qcLine.MaterialId,
        out var material))
                    {
                        throw new InvalidOperationException(
                            $"Material ID {qcLine.MaterialId} was not found.");
                    }

                    ValidateLots(
                        qcLine,
                        lineDto.Lots,
                        material.is_lot_tracked,
                        material.material_name
                    );
                    SyncQcLineLots(
     qcLine,
     lineDto,
     inspectorId,
     now
 );


                    var rrLine = rr.Lines.FirstOrDefault(
                        x => x.RrLineId == qcLine.RrLineId
                    );

                    if (rrLine == null)
                    {
                        throw new Exception(
                            $"Receiving Report line {qcLine.RrLineId} was not found."
                        );
                    }

                    var poLine = po.Lines.FirstOrDefault(
                        x => x.PoLineId == qcLine.PoLineId
                    );

                    if (poLine == null)
                    {
                        throw new Exception(
                            $"Purchase Order line {qcLine.PoLineId} was not found."
                        );
                    }

                    qcLine.AcceptedQty = lineDto.Lots.Sum(x => x.AcceptedQty);
                    qcLine.RejectedQty = lineDto.Lots.Sum(x => x.RejectedQty);
                    qcLine.Remarks = lineDto.Remarks;
                    qcLine.Status = GetInspectionLineStatus(
     qcLine.ReceivedQty,
     qcLine.AcceptedQty,
     qcLine.RejectedQty
 );

                    qcLine.UpdatedAt = now;

                    rrLine.AcceptedQty = qcLine.AcceptedQty;
                    rrLine.RejectedQty = qcLine.RejectedQty;
                    rrLine.Status = qcLine.Status;
                    rrLine.UpdatedAt = now;

                    // Physical received quantity was already posted during RR creation.
                    // Rejected quantity must be returned to the PO balance.


                    // Accepted quantity will be posted to inventory next.
                    if (qcLine.AcceptedQty > 0)
                    {
                        await AddAcceptedQtyToInventoryAsync(
                            qc,
                            rr,
                            qcLine,
                            rrLine,
                            inspectorId,
                            now);
                    }
                }

                var totalReceived = qc.Lines.Sum(x => x.ReceivedQty);
                var totalAccepted = qc.Lines.Sum(x => x.AcceptedQty);
                var totalRejected = qc.Lines.Sum(x => x.RejectedQty);

                qc.Decision = GetHeaderDecision(
                    totalReceived,
                    totalAccepted,
                    totalRejected
                );

                qc.Status = "COMMITTED";
                qc.CommittedBy = inspectorId;
                qc.CommittedAt = now;
                qc.InspectionDate = dto.InspectionDate ?? now;
                qc.InspectorId = inspectorId;
                qc.Remarks = dto.Remarks;
               
                qc.UpdatedAt = now;
               

                rr.Status = "COMMITTED";
                rr.CommittedBy = inspectorId;
                rr.CommittedAt = now;
                rr.QcBy = inspectorId;
                rr.QcAt = now;
              
                rr.UpdatedAt = now;

                var allPoLinesClosed = po.Lines.All(x => x.BalanceQty <= 0);
                var hasReceivedQty = po.Lines.Any(x => x.ReceivedQty > 0);

                po.Status = allPoLinesClosed
                    ? "FULLY_RECEIVED"
                    : hasReceivedQty
                        ? "PARTIALLY_RECEIVED"
                        : "APPROVED";

                po.UpdatedAt = now;
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

            if (qcLine.Lots == null || qcLine.Lots.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No QC lot information was found for material ID {qcLine.MaterialId}.");
            }

            foreach (var qcLot in qcLine.Lots.Where(x => x.AcceptedQty > 0))
            {
                var lotNo = qcLot.LotNo?.Trim();

                if (string.IsNullOrWhiteSpace(lotNo))
                {
                    throw new InvalidOperationException(
                        $"Lot reference is missing for material ID {qcLine.MaterialId}.");
                }

                /*
                 * Idempotency protection:
                 * Do not post the same QC material lot transaction twice.
                 */
                var transactionExistsLocally =
      _context.MaterialInventoryTransactions.Local.Any(x =>
          x.material_id == qcLine.MaterialId &&
          x.branch_id == branchId &&
          x.lot_no == lotNo &&
          x.transaction_type == "PURCHASE_RECEIPT" &&
          x.reference_type == "QC" &&
          x.reference_id == qc.QcId);

                var transactionExistsInDatabase =
                    await _context.MaterialInventoryTransactions.AnyAsync(x =>
                        x.material_id == qcLine.MaterialId &&
                        x.branch_id == branchId &&
                        x.lot_no == lotNo &&
                        x.transaction_type == "PURCHASE_RECEIPT" &&
                        x.reference_type == "QC" &&
                        x.reference_id == qc.QcId);

                if (transactionExistsLocally || transactionExistsInDatabase)
                {
                    throw new InvalidOperationException(
                        $"QC {qc.QcNo}, material ID {qcLine.MaterialId}, " +
                        $"lot {lotNo} has already been committed to inventory.");
                }



                var inventoryLot =
     _context.MaterialLotNumbers.Local.FirstOrDefault(x =>
         x.material_id == qcLine.MaterialId &&
         x.branch_id == branchId &&
         x.lot_no == lotNo);

                inventoryLot ??=
                    await _context.MaterialLotNumbers.FirstOrDefaultAsync(x =>
                        x.material_id == qcLine.MaterialId &&
                        x.branch_id == branchId &&
                        x.lot_no == lotNo);

                if (inventoryLot == null)
                {
                    inventoryLot = new MaterialLotNumber
                    {
                        material_id = qcLine.MaterialId,
                        branch_id = branchId,
                        lot_no = lotNo,

                        manufacturing_date = qcLot.ManufacturingDate,
                        expiration_date = qcLot.ExpirationDate,

                        quantity = qcLot.AcceptedQty,
                        uom = rrLine.Uom,

                        supplier_id = qc.SupplierId,

                        remarks = $"Accepted through QC {qc.QcNo}; RR {rr.RrNo}.",
                        is_active = true,

                        created_at = now,
                        updated_at = null
                    };

                    await _context.MaterialLotNumbers.AddAsync(inventoryLot);
                }
                else
                {
                    inventoryLot.quantity += qcLot.AcceptedQty;
                    inventoryLot.is_active = true;
                    inventoryLot.updated_at = now;

                    /*
                     * Preserve existing dates unless they are currently empty.
                     */
                    inventoryLot.manufacturing_date ??= qcLot.ManufacturingDate;
                    inventoryLot.expiration_date ??= qcLot.ExpirationDate;
                    inventoryLot.supplier_id ??= qc.SupplierId;
                    if (string.IsNullOrWhiteSpace(inventoryLot.uom))
                    {
                        inventoryLot.uom = rrLine.Uom.Trim();
                    }
                }

                var inventoryTransaction = new MaterialInventoryTransaction
                {
                    material_id = qcLine.MaterialId,
                    branch_id = branchId,
                    lot_no = lotNo,

                    transaction_type = "PURCHASE_RECEIPT",
                    quantity = qcLot.AcceptedQty,
                    uom = rrLine.Uom,

                    reference_type = "QC",
                    reference_id = qc.QcId,
                    reference_no = qc.QcNo,

                    remarks =
                        $"Accepted inventory from RR {rr.RrNo}, PO {qc.PoNo}, " +
                        $"lot {lotNo}.",

                    encoded_by = userId,
                    transaction_date = now,
                    created_at = now
                };

                await _context.MaterialInventoryTransactions.AddAsync(
                    inventoryTransaction);
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
      
    }
}