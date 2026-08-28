using inventory_api.Data;
using inventory_api.DTOs.Purchasing.RawMaterialProcessing;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.RawMaterialProcessing
{
    public class RmwProcessingService
    {
        private readonly AppDbContext _context;

        public RmwProcessingService(
            AppDbContext context)
        {
            _context = context;
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
                "WEIGHING_IN_PROGRESS"
            )
            {
                throw new InvalidOperationException(
                    $"Weighing cannot be completed. Current status: {line.Status}"
                );
            }

            if (dto.ActualQty <= 0)
            {
                throw new InvalidOperationException(
                    "Actual quantity must be greater than zero."
                );
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "User ID is required."
                );
            }

            var now = DateTime.Now;

            line.ActualQty =
                dto.ActualQty;

            line.VarianceQty =
                dto.ActualQty -
                line.QaAcceptedQty;

            line.Status =
                "WEIGHING_COMPLETED";

            line.WeighingCompletedBy =
                userId.Trim();

            line.WeighingCompletedAt =
                now;

            line.Remarks =
                string.IsNullOrWhiteSpace(
                    dto.Remarks)
                    ? line.Remarks
                    : dto.Remarks.Trim();

            line.UpdatedAt =
                now;

            await UpdateHeaderStatusAsync(
                line.ProcessingId,
                now
            );

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
                "WEIGHING_COMPLETED"
            )
            {
                throw new InvalidOperationException(
                    $"Sticker cannot be completed. Current status: {line.Status}"
                );
            }

            if (!line.ActualQty.HasValue)
            {
                throw new InvalidOperationException(
                    "Actual quantity has not been recorded."
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
                "STICKER_COMPLETED";

            line.StickerCompletedBy =
                userId.Trim();

            line.StickerCompletedAt =
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
                        x.ProcessingId ==
                        processingId);

            if (header == null)
            {
                throw new InvalidOperationException(
                    "RMW processing header not found."
                );
            }

            var statuses =
                header.Lines
                    .Select(x => x.Status)
                    .ToList();

            if (
                statuses.All(x =>
                    x == "STICKER_COMPLETED")
            )
            {
                header.Status =
                    "READY_FOR_FINAL_RR";

                header.CompletedAt =
                    now;
            }
            else if (
                statuses.Any(x =>
                    x == "WEIGHING_IN_PROGRESS")
            )
            {
                header.Status =
                    "WEIGHING_IN_PROGRESS";
            }
            else if (
                statuses.Any(x =>
                    x == "WEIGHING_COMPLETED" ||
                    x == "STICKER_COMPLETED")
            )
            {
                header.Status =
                    "PROCESSING";
            }
            else
            {
                header.Status =
                    "READY_FOR_WEIGHING";
            }

            header.UpdatedAt =
                now;
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