using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class ReturnService
    {
        private readonly AppDbContext _context;

        public ReturnService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReturnHeader>> GetAllAsync(string statusFilter = "active")
        {
            var query = _context.ReturnHeaders
                .Where(x => !x.is_deleted);

            if (statusFilter == "active")
            {
                query = query.Where(x =>
                    (x.status ?? "").ToUpper() != "RELEASED FOR REPROCESS" &&
                    (x.status ?? "").ToUpper() != "CANCELLED");
            }
            else if (statusFilter == "completed")
            {
                query = query.Where(x =>
                    (x.status ?? "").ToUpper() == "RELEASED FOR REPROCESS");
            }

            return await query
                .OrderByDescending(x => x.created_at)
                .ToListAsync();
        }

        public async Task<ReturnHeader> GetByIdAsync(long id)
        {
            var data = await _context.ReturnHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.return_id == id && !x.is_deleted);

            if (data == null)
                throw new Exception("Return not found.");

            return data;
        }

        public async Task<ReturnHeader> CreateAsync(CreateReturnDto dto)
        {
            if (dto.lines == null || !dto.lines.Any())
                throw new Exception("Return must have at least one item.");

            if (dto.lines.Any(x => x.quantity <= 0))
                throw new Exception("Return quantity must be greater than zero.");

            var now = DateTime.UtcNow;

            var header = new ReturnHeader
            {
                return_no = await GenerateReturnNoAsync(),
                customer_id = dto.customer_id,
                customer_name = dto.customer_name,
                return_date = dto.return_date == default ? now : dto.return_date,
                reason = dto.reason,
                remarks = dto.remarks,
                created_by = dto.created_by,
                status = "QUARANTINE",
                created_at = now,
                Lines = dto.lines.Select(l => new ReturnLine
                {
                    product_id = l.product_id,
                    product_name = l.product_name,
                    branch_id = l.branch_id,
                    lot_no = l.lot_no,
                    quantity = l.quantity,
                    uom = l.uom,
                    quarantine_location = l.quarantine_location,
                    condition_status = string.IsNullOrWhiteSpace(l.condition_status)
                        ? "FOR INSPECTION"
                        : l.condition_status,
                    release_status = "IN QUARANTINE",
                    released_qty = 0,
                    remarks = l.remarks,

                    order_id = l.order_id,
                    order_no = l.order_no,
                    checklist_id = l.checklist_id,
                    checklist_no = l.checklist_no,
                    source_transaction_id = l.source_transaction_id,
                    po_no = l.po_no,
                    dr_no = l.dr_no,
                    inv_no = l.inv_no,



                    created_at = now
                }).ToList()
            };

            _context.ReturnHeaders.Add(header);

            foreach (var l in dto.lines)
            {
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    product_id = l.product_id,
                    branch_id = l.branch_id,
                    lot_no = l.lot_no,
                    quantity = l.quantity,

                    transaction_type = "RETURN_IN",

                    scanned_by = string.IsNullOrWhiteSpace(dto.created_by) ? "SYSTEM" : dto.created_by,
                    remarks = $"Return to quarantine - {header.return_no}",

                    supplier_id = null,
                    customer_id = string.IsNullOrWhiteSpace(header.customer_id) ? null : header.customer_id,

                    dr_no = string.IsNullOrWhiteSpace(l.dr_no) ? null : l.dr_no,
                    inv_no = string.IsNullOrWhiteSpace(l.inv_no) ? null : l.inv_no,
                    po_no = string.IsNullOrWhiteSpace(l.po_no) ? null : l.po_no,

                    checklist_id = l.checklist_id,
                    checklist_no = string.IsNullOrWhiteSpace(l.checklist_no) ? null : l.checklist_no,
                    checklist_line_id = null,

                    order_id = l.order_id,
                    order_no = string.IsNullOrWhiteSpace(l.order_no) ? null : l.order_no,
                    order_line_id = null,

                    created_at = now
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception(msg);
            }
            return header;
        }

        public async Task<ReturnHeader> ReleaseForReprocessAsync(long returnId, ReleaseReturnForReprocessDto dto)
        {
            var header = await _context.ReturnHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.return_id == returnId && !x.is_deleted);

            if (header == null)
                throw new Exception("Return not found.");

            if (header.status == "CANCELLED")
                throw new Exception("Cancelled return cannot be released.");

            if (dto.lines == null || !dto.lines.Any())
                throw new Exception("No return lines selected.");

            var now = DateTime.UtcNow;

            foreach (var item in dto.lines)
            {
                var line = header.Lines.FirstOrDefault(x => x.return_line_id == item.return_line_id);

                if (line == null)
                    throw new Exception($"Return line {item.return_line_id} not found.");

                if (item.quantity <= 0)
                    throw new Exception("Release quantity must be greater than zero.");

                var remainingQty = line.quantity - line.released_qty;

                if (item.quantity > remainingQty)
                    throw new Exception($"Release quantity exceeds remaining quarantine qty for {line.product_name}.");

                line.released_qty += item.quantity;
                line.updated_at = now;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    product_id = line.product_id,
                    branch_id = line.branch_id,
                    lot_no = line.lot_no,
                    quantity = item.quantity,

                    transaction_type = "RETURN_RELEASE",

                    scanned_by = string.IsNullOrWhiteSpace(dto.released_by) ? "SYSTEM" : dto.released_by,
                    remarks = $"Released for reprocess - {header.return_no}",

                    supplier_id = null,
                    customer_id = string.IsNullOrWhiteSpace(header.customer_id) ? null : header.customer_id,

                    dr_no = string.IsNullOrWhiteSpace(line.dr_no) ? null : line.dr_no,
                    inv_no = string.IsNullOrWhiteSpace(line.inv_no) ? null : line.inv_no,
                    po_no = null,

                    checklist_id = line.checklist_id,
                    checklist_no = string.IsNullOrWhiteSpace(line.checklist_no) ? null : line.checklist_no,
                    checklist_line_id = null,

                    order_id = line.order_id,
                    order_no = string.IsNullOrWhiteSpace(line.order_no) ? null : line.order_no,
                    order_line_id = null,

                    created_at = now
                });

                if (line.released_qty >= line.quantity)
                    line.release_status = "RELEASED";
                else
                    line.release_status = "PARTIALLY RELEASED";

                if (line.released_qty >= line.quantity)
                    line.release_status = "RELEASED";
                else
                    line.release_status = "PARTIALLY RELEASED";
            }

            var totalQty = header.Lines.Sum(x => x.quantity);
            var totalReleased = header.Lines.Sum(x => x.released_qty);

            if (totalReleased <= 0)
                header.status = "QUARANTINE";
            else if (totalReleased < totalQty)
                header.status = "PARTIALLY RELEASED";
            else
                header.status = "RELEASED FOR REPROCESS";

            header.updated_at = now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception(msg);
            }
            return header;
        }

        public async Task CancelAsync(long returnId)
        {
            var header = await _context.ReturnHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.return_id == returnId && !x.is_deleted);

            if (header == null)
                throw new Exception("Return not found.");

            if (header.Lines.Any(x => x.released_qty > 0))
                throw new Exception("Cannot cancel return because some items were already released.");

            header.status = "CANCELLED";
            header.is_deleted = true;
            header.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task<string> GenerateReturnNoAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"RET-{today}-";

            var countToday = await _context.ReturnHeaders
                .CountAsync(x => x.return_no.StartsWith(prefix));

            return $"{prefix}{(countToday + 1).ToString("D3")}";
        }

        public async Task<List<ReturnHeader>> GetRecentAsync(int limit = 5)
        {
            return await _context.ReturnHeaders
                .Include(x => x.Lines)
                .Where(x => !x.is_deleted)
                .OrderByDescending(x => x.created_at)
                .Take(limit)
                .ToListAsync();
        }
    }
}