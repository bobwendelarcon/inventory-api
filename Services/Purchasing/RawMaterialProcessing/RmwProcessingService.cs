using inventory_api.Data;
using inventory_api.DTOs.Purchasing.RawMaterialProcessing;
using inventory_api.Services.Purchasing.SupplierEvaluations;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.RawMaterialProcessing
{
    public class RmwProcessingService
    {
        private readonly AppDbContext _context;
        private readonly SupplierEvaluationGenerationService
            _supplierEvaluationGenerationService;

        public RmwProcessingService(
            AppDbContext context,
            SupplierEvaluationGenerationService supplierEvaluationGenerationService)
        {
            _context = context;

            _supplierEvaluationGenerationService =
                supplierEvaluationGenerationService;
        }

        // ============================================================
        // START WEIGHING
        // ============================================================

        public async Task StartWeighingAsync(
            int processingLineId,
            string userId)
        {
            var line =
                await _context.RmwProcessingLines
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingLineId ==
                        processingLineId);

            if (line == null)
            {
                throw new InvalidOperationException(
                    "RMW processing line not found."
                );
            }

            if (
                line.Status !=
                "READY_FOR_WEIGHING"
            )
            {
                throw new InvalidOperationException(
                    $"Weighing cannot be started. Current status: {line.Status}"
                );
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "User ID is required."
                );
            }

            var now = DateTime.Now;

            line.Status =
                "WEIGHING_IN_PROGRESS";

            line.WeighingStartedBy =
                userId.Trim();

            line.WeighingStartedAt =
                now;

            line.UpdatedAt =
                now;

            await UpdateHeaderStatusAsync(
                line.ProcessingId,
                now
            );

            await _context.SaveChangesAsync();
        }


        // ============================================================
        // COMPLETE WEIGHING
        // ============================================================

        public async Task CompleteWeighingAsync(
       int processingLineId,
       CompleteWeighingDto dto,
       string userId)
        {
            var line =
                await _context.RmwProcessingLines
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingLineId == processingLineId);

            if (line == null)
            {
                throw new InvalidOperationException(
                    "RMW processing line not found.");
            }

            if (line.Status != "WEIGHING_IN_PROGRESS")
            {
                throw new InvalidOperationException(
                    $"Weighing cannot be completed. Current status: {line.Status}");
            }

            if (dto.ActualQty <= 0)
            {
                throw new InvalidOperationException(
                    "Actual quantity must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "User ID is required.");
            }

            var material =
                await _context.Materials
                    .AsNoTracking()
                    .Where(x =>
                        x.material_id == line.MaterialId)
                    .Select(x => new
                    {
                        x.requires_sticker
                    })
                    .FirstOrDefaultAsync();

            if (material == null)
            {
                throw new InvalidOperationException(
                    $"Material ID {line.MaterialId} was not found.");
            }

            var now = DateTime.Now;

            line.ActualQty = dto.ActualQty;

            line.VarianceQty =
                dto.ActualQty - line.QaAcceptedQty;

            line.WeighingCompletedBy =
                userId.Trim();

            line.WeighingCompletedAt =
                now;

            line.Remarks =
                string.IsNullOrWhiteSpace(dto.Remarks)
                    ? line.Remarks
                    : dto.Remarks.Trim();

            // Determine the NEXT required operation.
            line.Status =
                material.requires_sticker
                    ? "READY_FOR_STICKER"
                    : "READY_FOR_FINAL_RR";

            line.UpdatedAt = now;

            await UpdateHeaderStatusAsync(
                line.ProcessingId,
                now);

            await _context.SaveChangesAsync();
        }



        public async Task StartCountingAsync(
    int processingLineId,
    string userId)
        {
            var line =
                await _context.RmwProcessingLines
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingLineId == processingLineId);

            if (line == null)
            {
                throw new InvalidOperationException(
                    "RMW processing line not found.");
            }

            if (line.Status != "READY_FOR_COUNTING")
            {
                throw new InvalidOperationException(
                    $"Counting cannot be started. " +
                    $"Current status: {line.Status}");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "User ID is required.");
            }

            var now = DateTime.Now;

            line.Status =
                "COUNTING_IN_PROGRESS";

            line.CountingStartedBy =
     userId.Trim();

            line.CountingStartedAt =
                now;

            line.UpdatedAt =
                now;

            await UpdateHeaderStatusAsync(
                line.ProcessingId,
                now);

            await _context.SaveChangesAsync();
        }


        public async Task CompleteCountingAsync(
    int processingLineId,
    CompleteWeighingDto dto,
    string userId)
        {
            var line =
                await _context.RmwProcessingLines
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingLineId == processingLineId);

            if (line == null)
            {
                throw new InvalidOperationException(
                    "RMW processing line not found.");
            }

            if (line.Status != "COUNTING_IN_PROGRESS")
            {
                throw new InvalidOperationException(
                    $"Counting cannot be completed. " +
                    $"Current status: {line.Status}");
            }

            if (dto.ActualQty <= 0)
            {
                throw new InvalidOperationException(
                    "Actual counted quantity must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "User ID is required.");
            }

            // Get material configuration
            var material =
                await _context.Materials
                    .AsNoTracking()
                    .Where(x =>
                        x.material_id == line.MaterialId)
                    .Select(x => new
                    {
                        x.requires_sticker
                    })
                    .FirstOrDefaultAsync();

            if (material == null)
            {
                throw new InvalidOperationException(
                    $"Material ID {line.MaterialId} was not found.");
            }

            var now = DateTime.Now;

            // Actual counted quantity
            line.ActualQty =
                dto.ActualQty;

            // Difference between QA accepted qty
            // and actual counted qty
            line.VarianceQty =
                dto.ActualQty -
                line.QaAcceptedQty;

            line.CountingCompletedBy =
     userId.Trim();

            line.CountingCompletedAt =
                now;

            if (!string.IsNullOrWhiteSpace(dto.Remarks))
            {
                line.Remarks =
                    dto.Remarks.Trim();
            }

            // ========================================================
            // DETERMINE NEXT STEP
            // ========================================================

            if (material.requires_sticker)
            {
                line.Status =
                    "READY_FOR_STICKER";
            }
            else
            {
                line.Status =
                    "READY_FOR_FINAL_RR";
            }

            line.UpdatedAt =
                now;

            await UpdateHeaderStatusAsync(
                line.ProcessingId,
                now);

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // COMPLETE STICKER
        // ============================================================

        public async Task CompleteStickerAsync(
         int processingLineId,
         string userId)
        {
            var line =
                await _context.RmwProcessingLines
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingLineId == processingLineId);

            if (line == null)
            {
                throw new InvalidOperationException(
                    "RMW processing line not found.");
            }

            if (line.Status != "READY_FOR_STICKER")
            {
                throw new InvalidOperationException(
                    $"Sticker cannot be completed. Current status: {line.Status}");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "User ID is required.");
            }

            var now = DateTime.Now;

            line.Status =
                "READY_FOR_FINAL_RR";

            line.StickerCompletedBy =
                userId.Trim();

            line.StickerCompletedAt =
                now;

            line.UpdatedAt =
                now;

            await UpdateHeaderStatusAsync(
                line.ProcessingId,
                now);

            await _context.SaveChangesAsync();
        }


        // ============================================================
        // UPDATE HEADER STATUS BASED ON LOT/LINES
        // ============================================================

        private async Task UpdateHeaderStatusAsync(
       int processingId,
       DateTime now)
        {
            var header =
                await _context.RmwProcessingHeaders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingId == processingId);

            if (header == null)
            {
                throw new InvalidOperationException(
                    "RMW processing header not found.");
            }

            var statuses =
                header.Lines
                    .Select(x => x.Status)
                    .ToList();

            if (!statuses.Any())
            {
                throw new InvalidOperationException(
                    "RMW processing does not contain any lines.");
            }

            // ---------------------------------------------------------
            // ALL LINES FINISHED
            // ---------------------------------------------------------

            if (statuses.All(x =>
                x == "READY_FOR_FINAL_RR"))
            {
                var wasAlreadyCompleted =
                    header.Status == "READY_FOR_FINAL_RR";

                header.Status =
                    "READY_FOR_FINAL_RR";

                if (!header.CompletedAt.HasValue)
                {
                    header.CompletedAt = now;
                }

                //// Determine who performed the final RMW operation.
                //var actionBy =
                //    header.Lines
                //        .Where(x =>
                //            !string.IsNullOrWhiteSpace(
                //                x.StickerCompletedBy))
                //        .OrderByDescending(x =>
                //            x.StickerCompletedAt)
                //        .Select(x =>
                //            x.StickerCompletedBy)
                //        .FirstOrDefault();

                //actionBy ??=
                //    header.Lines
                //        .Where(x =>
                //            !string.IsNullOrWhiteSpace(
                //                x.WeighingCompletedBy))
                //        .OrderByDescending(x =>
                //            x.WeighingCompletedAt)
                //        .Select(x =>
                //            x.WeighingCompletedBy)
                //        .FirstOrDefault();

                //// NONE + no sticker may already be ready immediately
                //// after QA/QC release.
                //actionBy ??= header.CreatedBy;

                var actionBy =
    header.Lines
        .Where(x =>
            !string.IsNullOrWhiteSpace(
                x.StickerCompletedBy))
        .OrderByDescending(x =>
            x.StickerCompletedAt)
        .Select(x =>
            x.StickerCompletedBy)
        .FirstOrDefault();

                actionBy ??=
                    header.Lines
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.CountingCompletedBy))
                        .OrderByDescending(x =>
                            x.CountingCompletedAt)
                        .Select(x =>
                            x.CountingCompletedBy)
                        .FirstOrDefault();

                actionBy ??=
                    header.Lines
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.WeighingCompletedBy))
                        .OrderByDescending(x =>
                            x.WeighingCompletedAt)
                        .Select(x =>
                            x.WeighingCompletedBy)
                        .FirstOrDefault();

                actionBy ??= header.CreatedBy;

                header.CompletedBy =
                    actionBy;

                if (!wasAlreadyCompleted)
                {
                    await _supplierEvaluationGenerationService
                        .UpdateWorkflowStageAsync(
                            header.PoId,
                            "PENDING_FINAL_RR",
                            "RMW_PROCESSING_COMPLETED",
                            actionBy,
                            now,
                            $"RMW Processing {header.ProcessingNo} completed. " +
                            "Material is ready for Final Receiving Report."
                        );
                }
            }

            // ---------------------------------------------------------
            // ACTIVE WEIGHING
            // ---------------------------------------------------------

            else if (statuses.Any(x =>
                x == "WEIGHING_IN_PROGRESS"))
            {
                header.Status =
                    "WEIGHING_IN_PROGRESS";
            }

            // ---------------------------------------------------------
            // SOMETHING STILL NEEDS WEIGHING
            // ---------------------------------------------------------

            else if (statuses.Any(x =>
                x == "READY_FOR_WEIGHING"))
            {
                header.Status =
                    "READY_FOR_WEIGHING";
            }

            // ---------------------------------------------------------
            // SOMETHING STILL NEEDS COUNTING
            // ---------------------------------------------------------

            else if (statuses.Any(x =>
                x == "COUNTING_IN_PROGRESS"))
            {
                header.Status =
                    "COUNTING_IN_PROGRESS";
            }

            else if (statuses.Any(x =>
                x == "READY_FOR_COUNTING"))
            {
                header.Status =
                    "READY_FOR_COUNTING";
            }

            // ---------------------------------------------------------
            // ONLY STICKER WORK REMAINS
            // ---------------------------------------------------------

            else if (statuses.Any(x =>
                x == "READY_FOR_STICKER"))
            {
                header.Status =
                    "READY_FOR_STICKER";
            }

            else
            {
                header.Status =
                    "PROCESSING";
            }

            header.UpdatedAt = now;
        }
        public async Task<List<RmwProcessingListDto>>
    GetAllAsync()
        {
            return await _context.RmwProcessingHeaders
     .Where(x =>
         x.Status != "COMPLETED")
     .OrderByDescending(x =>
         x.ProcessingId)
                 .Select(x => new RmwProcessingListDto
                {
                    ProcessingId =
                        x.ProcessingId,

                    ProcessingNo =
                        x.ProcessingNo,

                    QuarantineId =
                        x.QuarantineId,

                    QuarantineNo =
                        _context.QuarantineHeaders
                            .Where(q =>
                                q.QuarantineId ==
                                x.QuarantineId)
                            .Select(q =>
                                q.QuarantineNo)
                            .FirstOrDefault() ?? "",

                    QcId =
                        x.QcId,

                    QcNo =
                        _context.QcInspectionHeaders
                            .Where(q =>
                                q.QcId == x.QcId)
                            .Select(q =>
                                q.QcNo)
                            .FirstOrDefault() ?? "",

                    IncomingReceivingId =
                        x.IncomingReceivingId,

                    IncomingNo =
                        _context.IncomingReceivings
                            .Where(i =>
                                i.IncomingReceivingId ==
                                x.IncomingReceivingId)
                            .Select(i =>
                                i.IncomingNo)
                            .FirstOrDefault(),

                    PoId =
                        x.PoId,

                    PoNo =
                        _context.PurchaseOrderHeaders
                            .Where(p =>
                                p.PoId == x.PoId)
                            .Select(p =>
                                p.PoNo)
                            .FirstOrDefault() ?? "",

                    SupplierId =
                        x.SupplierId,

                    SupplierName =
                        _context.Suppliers
                            .Where(s =>
                                s.SupplierId ==
                                x.SupplierId)
                            .Select(s =>
                                s.SupplierName)
                            .FirstOrDefault() ?? "",

                    Status =
                        x.Status,

                    Lines = x.Lines
                        .OrderBy(l =>
                            l.ProcessingLineId)
                        .Select(l =>
                            new RmwProcessingLineDto
                            {
                                ProcessingLineId =
                                    l.ProcessingLineId,

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

                                QaAcceptedQty =
                                    l.QaAcceptedQty,

                                ActualQty =
                                    l.ActualQty,

                                VarianceQty =
                                    l.VarianceQty,

                                Uom =
                                    l.Uom,

                                Status =
                                    l.Status,

                                WeighingStartedBy =
                                    l.WeighingStartedBy,

                                WeighingStartedAt =
                                    l.WeighingStartedAt,

                                WeighingCompletedBy =
                                    l.WeighingCompletedBy,

                                WeighingCompletedAt =
                                    l.WeighingCompletedAt,


                                CountingStartedBy =
    l.CountingStartedBy,

                                CountingStartedAt =
    l.CountingStartedAt,

                                CountingCompletedBy =
    l.CountingCompletedBy,

                                CountingCompletedAt =
    l.CountingCompletedAt,


                                StickerCompletedBy =
                                    l.StickerCompletedBy,

                                StickerCompletedAt =
                                    l.StickerCompletedAt
                            })
                        .ToList()
                })
                .ToListAsync();
        }
    }
}