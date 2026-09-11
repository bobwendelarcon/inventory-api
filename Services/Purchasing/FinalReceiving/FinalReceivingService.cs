using inventory_api.Data;
using inventory_api.DTOs.Purchasing.FinalReceiving;
using inventory_api.Models.Manufacturing.Materials;
using inventory_api.Models.Purchasing.FinalReceiving;
using inventory_api.Services.Purchasing.SupplierEvaluations;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.FinalReceiving
{
    public class FinalReceivingService
    {
        private readonly AppDbContext _context;

        private readonly SupplierEvaluationGenerationService
            _supplierEvaluationGenerationService;

        public FinalReceivingService(
            AppDbContext context,
            SupplierEvaluationGenerationService supplierEvaluationGenerationService)
        {
            _context = context;

            _supplierEvaluationGenerationService =
                supplierEvaluationGenerationService;
        }


        // ============================================================
        // CREATE FINAL RR
        // ============================================================

        public async Task<FinalReceivingHeader>
            CreateFinalReceivingAsync(
                int processingId,
                CreateFinalReceivingDto dto,
                string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "User ID is required."
                );
            }


            var processing =
                await _context.RmwProcessingHeaders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingId ==
                        processingId);

            if (processing == null)
            {
                throw new InvalidOperationException(
                    "RMW processing record not found."
                );
            }


            // --------------------------------------------------------
            // Prevent duplicate Final RR
            // --------------------------------------------------------

            var existing =
                await _context.FinalReceivingHeaders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingId ==
                        processingId);

            if (existing != null)
            {
                return existing;
            }


            // --------------------------------------------------------
            // Validate processing
            // --------------------------------------------------------

            if (!processing.Lines.Any())
            {
                throw new InvalidOperationException(
                    "Processing record does not contain any lines."
                );
            }


            var incompleteLines =
                processing.Lines
                    .Where(x =>
                        x.Status !=
                        "STICKER_COMPLETED")
                    .ToList();


            if (incompleteLines.Any())
            {
                throw new InvalidOperationException(
                    "All processing lots must complete weighing and sticker identification before Final RR."
                );
            }


            var missingActualQty =
                processing.Lines
                    .Where(x =>
                        !x.ActualQty.HasValue ||
                        x.ActualQty.Value <= 0)
                    .ToList();


            if (missingActualQty.Any())
            {
                throw new InvalidOperationException(
                    "All processing lots must contain a valid actual quantity."
                );
            }


            var now =
                DateTime.Now;


            var finalRrNo =
                await GenerateFinalRrNoAsync();


            // --------------------------------------------------------
            // Create Final RR header
            // --------------------------------------------------------
            if (!processing.QcId.HasValue)
            {
                throw new InvalidOperationException(
                    "Raw Material Evaluation has not yet been completed.");
            }


            var finalReceiving =
                new FinalReceivingHeader
                {
                    FinalRrNo =
                        finalRrNo,

                    ProcessingId =
                        processing.ProcessingId,

                    QuarantineId =
                        processing.QuarantineId,

                    QcId =
    processing.QcId.Value,

                    IncomingReceivingId =
                        processing.IncomingReceivingId,

                    PoId =
                        processing.PoId,

                    SupplierId =
                        processing.SupplierId,

                    Status =
                        "DRAFT",

                    Remarks =
                        string.IsNullOrWhiteSpace(
                            dto.Remarks)
                            ? null
                            : dto.Remarks.Trim(),

                    CreatedBy =
                        userId.Trim(),

                    CreatedAt =
                        now
                };


            // --------------------------------------------------------
            // Create Final RR lines
            // --------------------------------------------------------

            foreach (var processingLine
                in processing.Lines)
            {
                var actualQty =
                    processingLine.ActualQty!.Value;


                var varianceQty =
                    actualQty -
                    processingLine.QaAcceptedQty;


                finalReceiving.Lines.Add(
                    new FinalReceivingLine
                    {
                        ProcessingLineId =
                            processingLine
                                .ProcessingLineId,

                        QuarantineLineId =
                            processingLine
                                .QuarantineLineId,

                        MaterialId =
                            processingLine.MaterialId,

                        LotNo =
                            processingLine.LotNo,

                        ManufacturingDate =
                            processingLine
                                .ManufacturingDate,

                        ExpirationDate =
                            processingLine
                                .ExpirationDate,

                        QaAcceptedQty =
                            processingLine
                                .QaAcceptedQty,

                        ActualQty =
                            actualQty,

                        VarianceQty =
                            varianceQty,

                        Uom =
                            processingLine.Uom
                    }
                );
            }


            _context.FinalReceivingHeaders.Add(
                finalReceiving
            );


            // Processing has reached Final RR stage.
            processing.Status =
                "READY_FOR_FINAL_RR";

            processing.UpdatedBy =
                userId.Trim();

            processing.UpdatedAt =
                now;


            await _context.SaveChangesAsync();


            return finalReceiving;
        }


        // ============================================================
        // GENERATE FINAL RR NUMBER
        // ============================================================

        private async Task<string>
            GenerateFinalRrNoAsync()
        {
            var year =
                DateTime.Now.Year;


            var prefix =
                $"FRR-{year}-";


            var lastNo =
                await _context.FinalReceivingHeaders
                    .Where(x =>
                        x.FinalRrNo
                            .StartsWith(prefix))
                    .OrderByDescending(x =>
                        x.FinalRrId)
                    .Select(x =>
                        x.FinalRrNo)
                    .FirstOrDefaultAsync();


            var nextNo = 1;


            if (!string.IsNullOrWhiteSpace(
                lastNo))
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


        // ============================================================
        // COMMIT FINAL TO INVENTORY
        // ============================================================

        public async Task CommitFinalReceivingAsync(
    int processingId,
    CommitFinalReceivingDto dto,
    string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "User ID is required."
                );
            }

            var strategy =
                _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    // ============================================================
                    // LOAD PROCESSING
                    // ============================================================

                    var processing =
                        await _context.RmwProcessingHeaders
                            .Include(x => x.Lines)
                            .FirstOrDefaultAsync(x =>
                                x.ProcessingId == processingId);

                    if (processing == null)
                    {
                        throw new InvalidOperationException(
                            "RMW processing record not found."
                        );
                    }

                    if (
                        processing.Status ==
                        "INVENTORY_COMMITTED"
                    )
                    {
                        throw new InvalidOperationException(
                            "This processing record has already been committed to inventory."
                        );
                    }

                    if (
                        processing.Status !=
                        "READY_FOR_FINAL_RR"
                    )
                    {
                        throw new InvalidOperationException(
                            $"Processing is not ready for Final RR. Current status: {processing.Status}"
                        );
                    }

                    if (!processing.Lines.Any())
                    {
                        throw new InvalidOperationException(
                            "Processing record contains no lines."
                        );
                    }

                    if (
                        processing.Lines.Any(x =>
                            x.Status !=
                            "STICKER_COMPLETED")
                    )
                    {
                        throw new InvalidOperationException(
                            "All lots must complete sticker identification before inventory commit."
                        );
                    }

                    if (
                        processing.Lines.Any(x =>
                            !x.ActualQty.HasValue ||
                            x.ActualQty.Value <= 0)
                    )
                    {
                        throw new InvalidOperationException(
                            "All lots must have a valid actual quantity before inventory commit."
                        );
                    }

                    // ============================================================
                    // GET BRANCH FROM ORIGINAL INCOMING RECEIVING
                    // ============================================================

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
                            "Incoming Receiving record not found."
                        );
                    }

                    var branchId =
                        incoming.BranchId?.Trim();

                    if (string.IsNullOrWhiteSpace(branchId))
                    {
                        throw new InvalidOperationException(
                            "Receiving branch is missing."
                        );
                    }

                    var now =
                        DateTime.Now;

                    var cleanUserId =
                        userId.Trim();

                    // ============================================================
                    // GET / CREATE FINAL RR
                    // ============================================================

                    var finalReceiving =
                        await _context.FinalReceivingHeaders
                            .Include(x => x.Lines)
                            .FirstOrDefaultAsync(x =>
                                x.ProcessingId ==
                                processingId);

                    if (!processing.QcId.HasValue)
                    {
                        throw new InvalidOperationException(
                            "Raw Material Evaluation has not yet been completed.");
                    }

                    if (finalReceiving == null)
                    {
                        var finalRrNo =
                            await GenerateFinalRrNoAsync();

                        finalReceiving =
                            new Models.Purchasing.FinalReceiving.FinalReceivingHeader
                            {
                                FinalRrNo =
                                    finalRrNo,

                                ProcessingId =
                                    processing.ProcessingId,

                                QuarantineId =
                                    processing.QuarantineId,

                                QcId =
    processing.QcId.Value,

                                IncomingReceivingId =
                                    processing.IncomingReceivingId,

                                PoId =
                                    processing.PoId,

                                SupplierId =
                                    processing.SupplierId,

                                Status =
                                    "DRAFT",

                                Remarks =
                                    string.IsNullOrWhiteSpace(
                                        dto.Remarks)
                                        ? null
                                        : dto.Remarks.Trim(),

                                CreatedBy =
                                    cleanUserId,

                                CreatedAt =
                                    now
                            };

                        foreach (
                            var processingLine
                            in processing.Lines)
                        {
                            var actualQty =
                                processingLine
                                    .ActualQty!.Value;

                            finalReceiving.Lines.Add(
                                new Models.Purchasing.FinalReceiving.FinalReceivingLine
                                {
                                    ProcessingLineId =
                                        processingLine
                                            .ProcessingLineId,

                                    QuarantineLineId =
                                        processingLine
                                            .QuarantineLineId,

                                    MaterialId =
                                        processingLine.MaterialId,

                                    LotNo =
                                        processingLine.LotNo,

                                    ManufacturingDate =
                                        processingLine
                                            .ManufacturingDate,

                                    ExpirationDate =
                                        processingLine
                                            .ExpirationDate,

                                    QaAcceptedQty =
                                        processingLine
                                            .QaAcceptedQty,

                                    ActualQty =
                                        actualQty,

                                    VarianceQty =
                                        actualQty -
                                        processingLine
                                            .QaAcceptedQty,

                                    Uom =
                                        processingLine.Uom
                                }
                            );
                        }

                        _context
                            .FinalReceivingHeaders
                            .Add(finalReceiving);

                        // Save once so FinalRrId exists
                        await _context.SaveChangesAsync();
                    }

                    // ============================================================
                    // PREVENT DUPLICATE COMMIT
                    // ============================================================

                    if (
                        finalReceiving.Status ==
                        "COMMITTED"
                    )
                    {
                        throw new InvalidOperationException(
                            "Final Receiving Report has already been committed."
                        );
                    }

                    // ============================================================
                    // POST EACH FINAL RR LINE TO INVENTORY
                    // ============================================================

                    foreach (
                        var line
                        in finalReceiving.Lines)
                    {
                        var material =
                            await _context.Materials
                                .FirstOrDefaultAsync(x =>
                                    x.material_id ==
                                    line.MaterialId &&
                                    x.is_active &&
                                    !x.is_deleted);

                        if (material == null)
                        {
                            throw new InvalidOperationException(
                                $"Material ID {line.MaterialId} was not found."
                            );
                        }

                        var actualQty =
                            line.ActualQty;

                        if (actualQty <= 0)
                        {
                            throw new InvalidOperationException(
                                $"Actual quantity must be greater than zero for material {material.material_code}."
                            );
                        }

                        // ========================================================
                        // DETERMINE INVENTORY LOT
                        // ========================================================

                        string inventoryLotNo;

                        if (material.is_lot_tracked)
                        {
                            if (string.IsNullOrWhiteSpace(
                                line.LotNo))
                            {
                                throw new InvalidOperationException(
                                    $"Lot number is required for material {material.material_code}."
                                );
                            }

                            inventoryLotNo =
                                line.LotNo.Trim();
                        }
                        else
                        {
                            inventoryLotNo =
                                $"NON-LOT-MAT-{line.MaterialId}";
                        }

                        // ========================================================
                        // FIND EXISTING INVENTORY LOT
                        // ========================================================

                        var inventoryLot =
                            await _context.MaterialLotNumbers
                                .FirstOrDefaultAsync(x =>
                                    x.material_id ==
                                    line.MaterialId &&
                                    x.branch_id ==
                                    branchId &&
                                    x.lot_no ==
                                    inventoryLotNo);

                        // ========================================================
                        // CREATE NEW INVENTORY LOT
                        // ========================================================

                        if (inventoryLot == null)
                        {
                            inventoryLot =
                                new MaterialLotNumber
                                {
                                    material_id =
                                        line.MaterialId,

                                    branch_id =
                                        branchId,

                                    lot_no =
                                        inventoryLotNo,

                                    manufacturing_date =
                                        material.is_lot_tracked
                                            ? line.ManufacturingDate
                                            : null,

                                    expiration_date =
                                        material.is_lot_tracked
                                            ? line.ExpirationDate
                                            : null,

                                    quantity =
                                        actualQty,

                                    uom =
                                        string.IsNullOrWhiteSpace(
                                            line.Uom)
                                            ? material.uom
                                            : line.Uom!,

                                    supplier_id =
                                        material.is_lot_tracked
                                            ? processing.SupplierId
                                            : null,

                                    remarks =
                                        $"Received through {finalReceiving.FinalRrNo}.",

                                    is_active =
                                        true,

                                    created_at =
                                        now,

                                    updated_at =
                                        null
                                };

                            await _context
                                .MaterialLotNumbers
                                .AddAsync(inventoryLot);
                        }
                        else
                        {
                            // ====================================================
                            // UPDATE EXISTING LOT
                            // ====================================================

                            inventoryLot.quantity +=
                                actualQty;

                            inventoryLot.is_active =
                                true;

                            inventoryLot.updated_at =
                                now;

                            if (material.is_lot_tracked)
                            {
                                inventoryLot
                                    .manufacturing_date ??=
                                    line.ManufacturingDate;

                                inventoryLot
                                    .expiration_date ??=
                                    line.ExpirationDate;

                                inventoryLot
                                    .supplier_id ??=
                                    processing.SupplierId;
                            }

                            if (
                                string.IsNullOrWhiteSpace(
                                    inventoryLot.uom))
                            {
                                inventoryLot.uom =
                                    string.IsNullOrWhiteSpace(
                                        line.Uom)
                                        ? material.uom
                                        : line.Uom!;
                            }
                        }

                        // ========================================================
                        // INVENTORY TRANSACTION
                        // ========================================================

                        var inventoryTransaction =
                            new MaterialInventoryTransaction
                            {
                                material_id =
                                    line.MaterialId,

                                branch_id =
                                    branchId,

                                lot_no =
                                    inventoryLotNo,

                                transaction_type =
                                    "FINAL_RECEIVING",

                                quantity =
                                    actualQty,

                                uom =
                                    string.IsNullOrWhiteSpace(
                                        line.Uom)
                                        ? material.uom
                                        : line.Uom!,

                                supplier_id =
                                    processing.SupplierId,

                                reference_type =
                                    "FINAL_RR",

                                reference_id =
                                    finalReceiving.FinalRrId,

                                reference_no =
                                    finalReceiving.FinalRrNo,

                                remarks =
                                    string.IsNullOrWhiteSpace(
                                        dto.Remarks)
                                        ? "Final receiving inventory commit."
                                        : dto.Remarks.Trim(),

                                encoded_by =
                                    cleanUserId,

                                transaction_date =
                                    now,

                                created_at =
                                    now
                            };

                        await _context
                            .MaterialInventoryTransactions
                            .AddAsync(
                                inventoryTransaction
                            );
                    }

                    // ============================================================
                    // FINAL STATUS UPDATES
                    // ============================================================

                    finalReceiving.Status =
                        "COMMITTED";

                    finalReceiving.CommittedBy =
                        cleanUserId;

                    finalReceiving.CommittedAt =
                        now;

                    if (!string.IsNullOrWhiteSpace(
                        dto.Remarks))
                    {
                        finalReceiving.Remarks =
                            dto.Remarks.Trim();
                    }

                    processing.Status =
                        "INVENTORY_COMMITTED";

                    processing.CompletedBy =
                        cleanUserId;

                    processing.CompletedAt =
                        now;

                    processing.UpdatedBy =
                        cleanUserId;

                    processing.UpdatedAt =
                        now;

                    foreach (
                        var processingLine
                        in processing.Lines)
                    {
                        processingLine.Status =
                            "INVENTORY_COMMITTED";

                        processingLine.UpdatedAt =
                            now;
                    }

                    // ============================================================
                    // SUPPLIER EVALUATION:
                    // FINAL RR / INVENTORY COMMIT COMPLETED
                    // ============================================================

              

                    await _supplierEvaluationGenerationService
                        .UpdateFromFinalReceivingAsync(
                            processing.ProcessingId,
                            cleanUserId,
                            now);

                    await _context.SaveChangesAsync();

                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}