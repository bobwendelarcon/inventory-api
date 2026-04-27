using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class ProductToProduceService
    {
        private readonly AppDbContext _context;

        public ProductToProduceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PlanningShortageDto>> GetPlanningShortagesAsync()
        {
            var result = await _context.DailyOrderHeaders
                .Where(h =>
                    !h.is_deleted &&
                    (h.status ?? "").ToUpper() != "COMPLETED" &&
                    (h.status ?? "").ToUpper() != "CANCELLED")
                .SelectMany(h => h.Lines)
                .Where(l => l.required_qty > l.allocated_qty)
                .Join(_context.Products,
                    l => l.product_id,
                    p => p.product_id,
                   (l, p) => new
                   {
                       l.product_id,
                       p.product_name,
                       p.uom,
                       p.pack_uom,
                       p.pack_qty,
                       shortage = l.required_qty - l.allocated_qty
                   })
                .GroupBy(x => new
                {
                    x.product_id,
                    x.product_name,
                    x.uom,
                    x.pack_uom,
                    x.pack_qty
                })
               .Select(g => new PlanningShortageDto
               {
                   ProductId = g.Key.product_id,
                   ProductName = g.Key.product_name,
                   Uom = g.Key.uom ?? "",
                   PackUom = g.Key.pack_uom,
                   PackQty = g.Key.pack_qty,
                   ShortageQty = g.Sum(x => x.shortage)
               })
                .OrderBy(x => x.ProductName)
                .ToListAsync();

            return result;
        }

        public async Task<long> CreateAsync(CreateProductToProduceDto dto)
        {
            if (dto.Lines == null || dto.Lines.Count == 0)
                throw new Exception("No items to produce.");

            var ptpNo = $"PTP-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var header = new ProductToProduceHeader
            {
                ptp_no = ptpNo,
                requested_date = dto.RequestedDate,
                created_by = dto.CreatedBy,
                remarks = dto.Remarks,
                created_at = DateTime.UtcNow,
                status = "PENDING",
                Lines = dto.Lines.Select(l => new ProductToProduceLine
                {
                    product_id = l.ProductId,
                    product_name = l.ProductName,
                    suggested_qty = l.SuggestedQty,
                    qty_input = l.QtyInput,
                    uom_type = l.UomType,
                    requested_qty = l.RequestedQty,
                    uom = l.Uom,
                    pack_uom = l.PackUom,
                    pack_qty = l.PackQty,
                    delivery_date = l.DeliveryDate,
                    remarks = l.Remarks,
                    source_type = l.SourceType
                }).ToList()
            };

            _context.ProductToProduceHeaders.Add(header);
            await _context.SaveChangesAsync();

            return header.ptp_id;
        }

        public async Task<object?> GetByIdAsync(long id)
        {
            return await _context.ProductToProduceHeaders
                .Where(x => x.ptp_id == id)
                .Select(x => new
                {
                    x.ptp_id,
                    x.ptp_no,
                    x.requested_date,
                    x.status,
                    x.remarks,
                    x.created_by,
                    x.created_at,
                    lines = x.Lines.Select(l => new
                    {
                        l.ptp_line_id,
                        l.product_id,
                        l.product_name,
                        l.suggested_qty,
                        l.qty_input,
                        l.uom_type,
                        l.requested_qty,
                        l.uom,
                        l.pack_uom,
                        l.pack_qty,
                        l.delivery_date,
                        l.remarks,
                        l.source_type,
                        l.status
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
}