using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class ProductToProduceService
    {
        private readonly AppDbContext _context;
        private readonly InventoryTransactionService _inventoryService;

        public ProductToProduceService(
     AppDbContext context,
     InventoryTransactionService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
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
                    source_type = l.SourceType,
                    produced_qty = 0,
                    status = "PENDING"
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

        public async Task<object> GetListAsync(
     int page = 1,
     int pageSize = 50,
     string status = "ACTIVE",
     string search = "")
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;

            status = (status ?? "ACTIVE").Trim().ToUpper();
            search = (search ?? "").Trim().ToLower();

            var baseQuery =
                from l in _context.ProductToProduceLines
                join h in _context.ProductToProduceHeaders
                    on l.ptp_id equals h.ptp_id
                select new
                {
                    ptpId = h.ptp_id,
                    ptpLineId = l.ptp_line_id,
                    ptpNo = h.ptp_no,

                    productId = l.product_id,
                    productName = l.product_name,

                    remarks = string.IsNullOrWhiteSpace(l.remarks)
                        ? h.remarks
                        : l.remarks,

                    requiredQty = l.requested_qty,
                    producedQty = l.produced_qty,
                    remainingQty = l.requested_qty - l.produced_qty,

                    uom = l.uom,
                    status = l.status,

                    startedAt = l.started_at,
                    completedAt = l.completed_at,

                    requestedBy = h.created_by,
                    requestedDate = h.requested_date,
                    createdAt = h.created_at
                };

            // Summary cards should count ALL records, not only filtered records
            var pending = await baseQuery.CountAsync(x => x.status == "PENDING");
            var ongoing = await baseQuery.CountAsync(x => x.status == "IN_PROGRESS");
            var partial = await baseQuery.CountAsync(x => x.status == "PARTIAL");
            var completed = await baseQuery.CountAsync(x => x.status == "COMPLETED");

            var query = baseQuery;

            // Status filter
            if (status == "ACTIVE")
            {
                query = query.Where(x =>
                    x.status != "COMPLETED" &&
                    x.status != "CANCELLED");
            }
            else if (status != "ALL")
            {
                query = query.Where(x => x.status == status);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    (x.ptpNo ?? "").ToLower().Contains(search) ||
                    (x.productName ?? "").ToLower().Contains(search) ||
                    (x.productId ?? "").ToLower().Contains(search) ||
                    (x.requestedBy ?? "").ToLower().Contains(search) ||
                    (x.remarks ?? "").ToLower().Contains(search));
            }

            var totalRecords = await query.CountAsync();

            var list = await query
                .OrderByDescending(x => x.createdAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new
            {
                page,
                pageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),

                pending,
                ongoing,
                partial,
                completed,

                items = list
            };
        }


        public async Task DeleteLineAsync(long ptpLineId)
        {
            var line = await _context.ProductToProduceLines
                .FirstOrDefaultAsync(x => x.ptp_line_id == ptpLineId);

            if (line == null)
                throw new Exception("PTP line not found.");

            if ((line.status ?? "").Trim().ToUpper() != "PENDING")
                throw new Exception("Only pending PTP requests can be deleted.");

            var ptpId = line.ptp_id;

            _context.ProductToProduceLines.Remove(line);
            await _context.SaveChangesAsync();

            var hasLines = await _context.ProductToProduceLines
                .AnyAsync(x => x.ptp_id == ptpId);

            if (!hasLines)
            {
                var header = await _context.ProductToProduceHeaders
                    .FirstOrDefaultAsync(x => x.ptp_id == ptpId);

                if (header != null)
                {
                    _context.ProductToProduceHeaders.Remove(header);
                    await _context.SaveChangesAsync();
                }
            }
        }


        public async Task StartProductionAsync(long ptpLineId)
        {
            var line = await _context.ProductToProduceLines
                .FirstOrDefaultAsync(x => x.ptp_line_id == ptpLineId);

            if (line == null)
                throw new Exception("PTP line not found.");

            if ((line.status ?? "").Trim().ToUpper() != "PENDING")
                throw new Exception("Only pending PTP can be started.");

            line.status = "IN_PROGRESS";
            line.started_at = DateTime.UtcNow;

            var header = await _context.ProductToProduceHeaders
                .FirstOrDefaultAsync(x => x.ptp_id == line.ptp_id);

            if (header != null)
            {
                header.status = "IN_PROGRESS";
                header.updated_at = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ProduceStockAsync(ProduceStockDto dto, string producedBy)
        {
            if (dto.quantity <= 0)
                throw new Exception("Invalid produced quantity.");

            if (string.IsNullOrWhiteSpace(dto.branchId))
                throw new Exception("Warehouse is required.");

            if (string.IsNullOrWhiteSpace(dto.lotNo))
                throw new Exception("Lot No is required.");

            var line = await _context.ProductToProduceLines
                .FirstOrDefaultAsync(x => x.ptp_line_id == dto.ptpLineId);

            if (line == null)
                throw new Exception("PTP line not found.");

            if ((line.status ?? "").Trim().ToUpper() == "PENDING")
                throw new Exception("Start production first before producing stock.");

            if ((line.status ?? "").Trim().ToUpper() == "COMPLETED")
                throw new Exception("This PTP request is already completed.");

            if (string.IsNullOrWhiteSpace(dto.transmittalNo))
                throw new Exception("Transmittal No. is required.");

            var remainingQty = line.requested_qty - line.produced_qty;

            if (dto.quantity > remainingQty)
                throw new Exception($"Produced quantity cannot exceed remaining quantity ({remainingQty}).");

            var header = await _context.ProductToProduceHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.ptp_id == line.ptp_id);

            if (header == null)
                throw new Exception("PTP header not found.");

            var productId = (line.product_id ?? "").Trim();
            var productExists = await _context.Products
    .AnyAsync(x => x.product_id == productId && !x.is_deleted);

            if (!productExists)
            {
                throw new Exception($"Product '{productId}' not found in Products table.");
            }


            var trNo = dto.transmittalNo.Trim();

          

            await _inventoryService.AddAsync(new CreateInventoryTransactionDto
            {
                product_id = productId,
                branch_id = dto.branchId,
                transaction_type = "IN",
                lot_no = dto.lotNo,
                quantity = (double)dto.quantity,
                scanned_by = producedBy,
                remarks = $"Production Stock IN - {header.ptp_no}",
                dr_no = trNo,
               
                manufacturing_date = dto.manufacturingDate,
                expiration_date = dto.expirationDate
            });

            line.produced_qty += dto.quantity;

            if (line.produced_qty >= line.requested_qty)
            {
                line.status = "COMPLETED";
                line.completed_at = DateTime.UtcNow;
            }
            else
            {
                line.status = "PARTIAL";
            }

            if (header.Lines.All(x => x.status == "COMPLETED"))
                header.status = "COMPLETED";
            else if (header.Lines.Any(x => x.status == "PARTIAL" || x.status == "IN_PROGRESS"))
                header.status = "IN_PROGRESS";
            else
                header.status = "PENDING";

            header.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }


    }
}