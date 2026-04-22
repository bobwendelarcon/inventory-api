using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class ChecklistOutService
    {
        private readonly AppDbContext _context;

        public ChecklistOutService(AppDbContext context)
        {
            _context = context;
        }

    //    public async Task<ChecklistOutResponseDto> ReleaseAsync(ChecklistOutRequestDto dto)
    //    {
    //        if (dto == null)
    //            throw new Exception("Invalid request.");

    //        if (dto.checklist_id <= 0)
    //            throw new Exception("checklist_id is required.");

    //        if (string.IsNullOrWhiteSpace(dto.lot_no))
    //            throw new Exception("lot_no is required.");

    //        if (dto.quantity <= 0)
    //            throw new Exception("quantity must be greater than 0.");

    //        if (string.IsNullOrWhiteSpace(dto.scanned_by))
    //            throw new Exception("scanned_by is required.");

    //        using var transaction = await _context.Database.BeginTransactionAsync();

    //        try
    //        {
    //            // 1. Checklist header
    //            var checklistHeader = await _context.DeliveryChecklistHeaders
    //.FirstOrDefaultAsync(x =>
    //    x.checklist_id == dto.checklist_id);

    //            if (checklistHeader == null)
    //                throw new Exception("Checklist not found.");

    //            var status = (checklistHeader.status ?? "").ToUpper();

    //            if (status != "LOADING" && status != "PARTIAL")
    //                throw new Exception("Checklist can be processed only when LOADING or PARTIAL.");

    //            // 2. Checklist line by checklist + lot
    //            var checklistLine = await _context.DeliveryChecklistLines
    //.FirstOrDefaultAsync(x =>
    //    x.checklist_line_id == dto.checklist_line_id &&
    //    x.checklist_id == dto.checklist_id &&
    //    !x.is_deleted);

    //            if (checklistLine == null)
    //                throw new Exception("Scanned lot is not included in this checklist.");

    //            if (checklistLine.remaining_qty <= 0)
    //                throw new Exception("This checklist line is already completed.");

    //            if (dto.quantity > checklistLine.remaining_qty)
    //                throw new Exception($"Quantity exceeds checklist remaining qty. Remaining: {checklistLine.remaining_qty}");

    //            // 3. Actual lot inventory
    //            if (string.IsNullOrWhiteSpace(checklistLine.branch_id))
    //                throw new Exception("Checklist line branch_id is missing.");

    //            var lot = await _context.ProductLotNumbers
    //                .FirstOrDefaultAsync(x =>
    //                    x.product_id == checklistLine.product_id &&
    //                    x.lot_no == checklistLine.lot_no &&
    //                    x.branch_id == checklistLine.branch_id &&
    //                    !x.is_deleted);

    //            if (lot == null)
    //                throw new Exception("Actual inventory lot not found for the selected branch.");

    //            if (lot == null)
    //                throw new Exception("Actual inventory lot not found.");

    //            if (lot.quantity < dto.quantity)
    //                throw new Exception("Actual lot inventory is insufficient.");

    //            // 4. Related daily order
    //            var orderHeader = await _context.DailyOrderHeaders
    //                .FirstOrDefaultAsync(x =>
    //                    x.order_id == checklistLine.order_id &&
    //                    !x.is_deleted);

    //            if (orderHeader == null)
    //                throw new Exception("Related daily order header not found.");

    //            var orderLine = await _context.DailyOrderLines
    //                .FirstOrDefaultAsync(x =>
    //                    x.order_line_id == checklistLine.order_line_id);

    //            if (orderLine == null)
    //                throw new Exception("Related daily order line not found.");

    //            // 5. Deduct actual stock
    //            lot.quantity -= dto.quantity;
    //            lot.updated_at = DateTime.UtcNow;


    //            // 🔍 Resolve customer_id from different sources
    //            string? resolvedCustomerId = dto.customer_id;

    //            if (string.IsNullOrWhiteSpace(resolvedCustomerId))
    //            {
    //                resolvedCustomerId = checklistLine.customer_id;
    //            }

    //            // 🔥 FINAL FALLBACK: lookup using customer_name
    //            if (string.IsNullOrWhiteSpace(resolvedCustomerId) &&
    //                !string.IsNullOrWhiteSpace(checklistLine.customer_name))
    //            {
    //                var partner = await _context.Partners
    //                    .FirstOrDefaultAsync(p =>
    //                        !string.IsNullOrEmpty(p.partner_name) &&
    //                        p.partner_name.Trim().ToUpper() == checklistLine.customer_name.Trim().ToUpper());

    //                resolvedCustomerId = partner?.partner_id;
    //            }





    //            // 6. Save inventory transaction
    //            var transactionData = new InventoryTransaction
    //            {
    //                product_id = checklistLine.product_id,
    //                branch_id = lot.branch_id,
    //                transaction_type = "OUT",
    //                reference_type = "CHECKLIST_OUT",
    //                lot_no = checklistLine.lot_no,
    //                quantity = dto.quantity,
    //                scanned_by = dto.scanned_by,
    //                remarks = dto.remarks ?? "",
    //                dr_no = dto.dr_no ?? "",
    //                inv_no = dto.inv_no ?? "",
    //                po_no = dto.po_no ?? "",
    //                supplier_id = null,
    //                customer_id = resolvedCustomerId,
    //                checklist_id = checklistHeader.checklist_id,
    //                checklist_no = checklistHeader.checklist_no,
    //                checklist_line_id = checklistLine.checklist_line_id,
    //                order_id = orderHeader.order_id,
    //                order_no = orderHeader.order_no,
    //                order_line_id = orderLine.order_line_id,
    //                created_at = DateTime.UtcNow,
    //                updated_at = DateTime.UtcNow,
    //                is_deleted = false
    //            };

    //            _context.InventoryTransactions.Add(transactionData);

    //            // 7. Update checklist line
    //            checklistLine.released_qty += dto.quantity;
    //            checklistLine.remaining_qty -= dto.quantity;
    //            checklistLine.updated_at = DateTime.UtcNow;
    //            checklistLine.status = checklistLine.remaining_qty <= 0 ? "COMPLETED" : "PARTIAL";

    //            // 8. Update daily order line (official fulfillment)
              
    //            orderLine.dispatched_qty += dto.quantity;

    //            // consume the currently allocated qty because it has already been delivered
    //            orderLine.allocated_qty = Math.Max(0, orderLine.allocated_qty - dto.quantity);

    //            // remaining balance still to deliver
    //            orderLine.remaining_qty = Math.Max(0, orderLine.required_qty - orderLine.dispatched_qty);

    //            // 8.1 Reduce allocation from FEFO table
    //            var allocations = await _context.DailyOrderAllocations
    // .Where(a =>
    //     a.order_line_id == orderLine.order_line_id &&
    //     a.lot_no == checklistLine.lot_no &&
    //     a.branch_id == checklistLine.branch_id &&
    //     a.allocated_qty > 0
    // )
    // .OrderBy(a => a.priority_rank)
    // .ToListAsync();

    //            decimal qtyToDeduct = dto.quantity;

    //            foreach (var alloc in allocations)
    //            {
    //                if (qtyToDeduct <= 0)
    //                    break;

    //                var deduct = Math.Min(alloc.allocated_qty, qtyToDeduct);

    //                alloc.allocated_qty -= deduct;
    //                qtyToDeduct -= deduct;

    //                if (alloc.allocated_qty <= 0)
    //                {
    //                    alloc.allocated_qty = 0;
    //                    // OPTIONAL: mark consumed or delete
    //                    // _context.DailyOrderAllocations.Remove(alloc);
    //                }
    //            }

    //            if (qtyToDeduct > 0)
    //                throw new Exception("Allocated quantity mismatch for this branch. Unable to fully deduct allocation.");



    //            orderLine.updated_at = DateTime.UtcNow;
    //            orderLine.status = orderLine.dispatched_qty >= orderLine.required_qty
    //                ? "COMPLETED"
    //                : "PARTIALLY DELIVERED";

    //            await _context.SaveChangesAsync();

    //            // 9. Recompute checklist header
    //            var checklistLines = await _context.DeliveryChecklistLines
    //                .Where(x => x.checklist_id == checklistHeader.checklist_id && !x.is_deleted)
    //                .ToListAsync();

    //            bool allChecklistDone = checklistLines.All(x => x.remaining_qty <= 0);
    //            bool anyChecklistReleased = checklistLines.Any(x => x.released_qty > 0);

    //            checklistHeader.status = allChecklistDone
    //                ? "COMPLETED"
    //                : anyChecklistReleased
    //                    ? "PARTIAL"
    //                    : "LOADING";

              

    //            // 10. Recompute daily order header
    //            var orderLines = await _context.DailyOrderLines
    //                .Where(x => x.order_id == orderHeader.order_id)
    //                .ToListAsync();

    //            bool allOrderDone = orderLines.All(x => x.dispatched_qty >= x.required_qty);
    //            bool anyOrderDone = orderLines.Any(x => x.dispatched_qty > 0);

    //            orderHeader.status = allOrderDone
    //                ? "COMPLETED"
    //                : anyOrderDone
    //                    ? "PARTIALLY DELIVERED"
    //                    : orderHeader.status;

    //            if (allOrderDone)
    //                orderHeader.date_delivered = DateTime.UtcNow;

    //            orderHeader.updated_at = DateTime.UtcNow;

    //            await _context.SaveChangesAsync();
    //            await transaction.CommitAsync();

    //            return new ChecklistOutResponseDto
    //            {
    //                success = true,
    //                message = "Checklist OUT processed successfully.",
    //                checklist_id = checklistHeader.checklist_id,
    //                checklist_line_id = checklistLine.checklist_line_id,
    //                order_id = orderHeader.order_id,
    //                order_line_id = orderLine.order_line_id,
    //                product_id = checklistLine.product_id,
    //                lot_no = checklistLine.lot_no,
    //                released_qty = checklistLine.released_qty,
    //                checklist_remaining_qty = checklistLine.remaining_qty,
    //                dispatched_qty = orderLine.dispatched_qty,
    //                checklist_status = checklistHeader.status,
    //                order_line_status = orderLine.status,
    //                order_status = orderHeader.status
    //            };
    //        }
    //        catch
    //        {
    //            await transaction.RollbackAsync();
    //            throw;
    //        }
    //    }

        public async Task<ChecklistOutResponseDto> ReleaseAsync(ChecklistOutRequestDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (dto.checklist_id <= 0)
                throw new Exception("checklist_id is required.");

            if (dto.checklist_line_id <= 0)
                throw new Exception("checklist_line_id is required.");

            if (string.IsNullOrWhiteSpace(dto.lot_no))
                throw new Exception("lot_no is required.");

            if (dto.quantity <= 0)
                throw new Exception("quantity must be greater than 0.");

            if (string.IsNullOrWhiteSpace(dto.scanned_by))
                throw new Exception("scanned_by is required.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var checklistHeader = await _context.DeliveryChecklistHeaders
                    .FirstOrDefaultAsync(x => x.checklist_id == dto.checklist_id);

                if (checklistHeader == null)
                    throw new Exception("Checklist not found.");

                var headerStatus = (checklistHeader.status ?? "").ToUpper();
                if (headerStatus != "LOADING" && headerStatus != "PARTIAL")
                    throw new Exception("Checklist can be processed only when LOADING or PARTIAL.");

                var checklistLine = await _context.DeliveryChecklistLines
                    .FirstOrDefaultAsync(x =>
                        x.checklist_line_id == dto.checklist_line_id &&
                        x.checklist_id == dto.checklist_id &&
                        !x.is_deleted);

                if (checklistLine == null)
                    throw new Exception("Scanned lot is not included in this checklist.");

                if (!string.Equals(checklistLine.lot_no, dto.lot_no, StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Scanned lot does not match checklist line lot.");

                if (string.IsNullOrWhiteSpace(checklistLine.branch_id))
                    throw new Exception("Checklist line branch_id is missing.");

                if (checklistLine.remaining_qty <= 0)
                    throw new Exception("This checklist line is already completed.");

                if (dto.quantity > checklistLine.remaining_qty)
                    throw new Exception($"Quantity exceeds checklist remaining qty. Remaining: {checklistLine.remaining_qty}");

                var lot = await _context.ProductLotNumbers
                    .FirstOrDefaultAsync(x =>
                        x.product_id == checklistLine.product_id &&
                        x.lot_no == checklistLine.lot_no &&
                        x.branch_id == checklistLine.branch_id &&
                        !x.is_deleted);

                if (lot == null)
                    throw new Exception("Actual inventory lot not found for the selected branch.");

                if (lot.quantity < dto.quantity)
                    throw new Exception("Actual lot inventory is insufficient.");

                var orderHeader = await _context.DailyOrderHeaders
                    .FirstOrDefaultAsync(x =>
                        x.order_id == checklistLine.order_id &&
                        !x.is_deleted);

                if (orderHeader == null)
                    throw new Exception("Related daily order header not found.");

                var orderLine = await _context.DailyOrderLines
                    .FirstOrDefaultAsync(x => x.order_line_id == checklistLine.order_line_id);

                if (orderLine == null)
                    throw new Exception("Related daily order line not found.");

                lot.quantity -= dto.quantity;
                lot.updated_at = DateTime.UtcNow;

                string? resolvedCustomerId = dto.customer_id;

                if (string.IsNullOrWhiteSpace(resolvedCustomerId))
                    resolvedCustomerId = checklistLine.customer_id;

                if (string.IsNullOrWhiteSpace(resolvedCustomerId) &&
                    !string.IsNullOrWhiteSpace(checklistLine.customer_name))
                {
                    var partner = await _context.Partners
                        .FirstOrDefaultAsync(p =>
                            !string.IsNullOrEmpty(p.partner_name) &&
                            p.partner_name.Trim().ToUpper() == checklistLine.customer_name.Trim().ToUpper());

                    resolvedCustomerId = partner?.partner_id;
                }

                var transactionData = new InventoryTransaction
                {
                    product_id = checklistLine.product_id,
                    branch_id = checklistLine.branch_id, // ✅ MULTI-BRANCH SAFE
                    transaction_type = "OUT",
                    reference_type = "CHECKLIST_OUT",
                    lot_no = checklistLine.lot_no,
                    quantity = dto.quantity,
                    scanned_by = dto.scanned_by,
                    remarks = dto.remarks ?? "",
                    dr_no = dto.dr_no ?? "",
                    inv_no = dto.inv_no ?? "",
                    po_no = dto.po_no ?? "",
                    supplier_id = null,
                    customer_id = resolvedCustomerId,
                    checklist_id = checklistHeader.checklist_id,
                    checklist_no = checklistHeader.checklist_no,
                    checklist_line_id = checklistLine.checklist_line_id,
                    order_id = orderHeader.order_id,
                    order_no = orderHeader.order_no,
                    order_line_id = orderLine.order_line_id,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow,
                    is_deleted = false
                };

                _context.InventoryTransactions.Add(transactionData);

                checklistLine.released_qty += dto.quantity;
                checklistLine.remaining_qty -= dto.quantity;
                checklistLine.updated_at = DateTime.UtcNow;
                checklistLine.status = checklistLine.remaining_qty <= 0 ? "COMPLETED" : "PARTIAL";

                orderLine.dispatched_qty += dto.quantity;
                orderLine.allocated_qty = Math.Max(0, orderLine.allocated_qty - dto.quantity);
                orderLine.remaining_qty = Math.Max(0, orderLine.required_qty - orderLine.dispatched_qty);

                var allocations = await _context.DailyOrderAllocations
                    .Where(a =>
                        a.order_line_id == orderLine.order_line_id &&
                        a.lot_no == checklistLine.lot_no &&
                        a.branch_id == checklistLine.branch_id &&
                        a.allocated_qty > 0)
                    .OrderBy(a => a.priority_rank)
                    .ToListAsync();

                decimal qtyToDeduct = dto.quantity;

                foreach (var alloc in allocations)
                {
                    if (qtyToDeduct <= 0)
                        break;

                    var deduct = Math.Min(alloc.allocated_qty, qtyToDeduct);

                    alloc.allocated_qty -= deduct;
                    qtyToDeduct -= deduct;

                    if (alloc.allocated_qty <= 0)
                        alloc.allocated_qty = 0;
                }

                if (qtyToDeduct > 0)
                    throw new Exception("Allocated quantity mismatch for this branch. Unable to fully deduct allocation.");

                orderLine.updated_at = DateTime.UtcNow;
                orderLine.status = orderLine.dispatched_qty >= orderLine.required_qty
                    ? "COMPLETED"
                    : "PARTIALLY DELIVERED";

                await _context.SaveChangesAsync();

                var checklistLines = await _context.DeliveryChecklistLines
                    .Where(x => x.checklist_id == checklistHeader.checklist_id && !x.is_deleted)
                    .ToListAsync();

                bool allChecklistDone = checklistLines.All(x => x.remaining_qty <= 0);
                bool anyChecklistReleased = checklistLines.Any(x => x.released_qty > 0);

                checklistHeader.status = allChecklistDone
                    ? "COMPLETED"
                    : anyChecklistReleased ? "PARTIAL" : "LOADING";

                var orderLines = await _context.DailyOrderLines
                    .Where(x => x.order_id == orderHeader.order_id)
                    .ToListAsync();

                bool allOrderDone = orderLines.All(x => x.dispatched_qty >= x.required_qty);
                bool anyOrderDone = orderLines.Any(x => x.dispatched_qty > 0);

                orderHeader.status = allOrderDone
                    ? "COMPLETED"
                    : anyOrderDone ? "PARTIALLY DELIVERED" : orderHeader.status;

                if (allOrderDone)
                    orderHeader.date_delivered = DateTime.UtcNow;

                orderHeader.updated_at = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ChecklistOutResponseDto
                {
                    success = true,
                    message = "Checklist OUT processed successfully.",
                    checklist_id = checklistHeader.checklist_id,
                    checklist_line_id = checklistLine.checklist_line_id,
                    order_id = orderHeader.order_id,
                    order_line_id = orderLine.order_line_id,
                    product_id = checklistLine.product_id,
                    lot_no = checklistLine.lot_no ?? "",
                    released_qty = checklistLine.released_qty,
                    checklist_remaining_qty = checklistLine.remaining_qty,
                    dispatched_qty = orderLine.dispatched_qty,
                    checklist_status = checklistHeader.status,
                    order_line_status = orderLine.status,
                    order_status = orderHeader.status
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<List<ChecklistListItemDto>> GetChecklistsAsync()
        {
            var list = await _context.DeliveryChecklistHeaders
                .Where(x => x.status == "LOADING" || x.status == "PARTIAL")
              //  .Where(x => x.status == "READY" || x.status == "LOADING" || x.status == "PARTIAL")
                .OrderByDescending(x => x.checklist_id)
                .Select(x => new ChecklistListItemDto
                {
                    checklist_id = x.checklist_id,
                    checklist_no = x.checklist_no,
                    route_name = x.route_name,
                    truck_name = x.truck_name,
                    driver_name = x.driver_name,
                    status = x.status,
                    created_at = x.created_at.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToListAsync();

            return list;
        }

        public async Task<ChecklistDetailsDto> GetChecklistDetailsAsync(long checklistId)
        {
            if (checklistId <= 0)
                throw new Exception("Invalid checklist_id.");

            var header = await _context.DeliveryChecklistHeaders
                .FirstOrDefaultAsync(x => x.checklist_id == checklistId);

            if (header == null)
                throw new Exception("Checklist not found.");

       

            var lines = await _context.DeliveryChecklistLines
                .Where(x => x.checklist_id == checklistId && !x.is_deleted)
                .OrderBy(x => x.customer_name)
                .ThenBy(x => x.product_name)
                .ThenBy(x => x.lot_no)
                .Select(x => new ChecklistDetailLineDto
                {
                    checklist_line_id = x.checklist_line_id,
                    order_id = x.order_id,
                    order_no = x.order_no,
                    order_line_id = x.order_line_id,


                    customer_id = x.customer_id,
                    customer_name = x.customer_name,


                    product_id = x.product_id,
                    product_name = x.product_name,
                    branch_id = x.branch_id,   // ✅ NEW
                    uom = x.uom,
                    pack_uom = x.pack_uom,
                    pack_qty = x.pack_qty,
                    lot_no = x.lot_no ?? "",
                    manufacturing_date = x.manufacturing_date,
                    expiration_date = x.expiration_date,
                    required_qty = x.required_qty,
                    allocated_qty = x.allocated_qty,
                    checklist_qty = x.checklist_qty,
                    released_qty = x.released_qty,
                    remaining_qty = x.remaining_qty,
                    status = x.status,
                    remarks = x.remarks
                })
                .ToListAsync();

            return new ChecklistDetailsDto
            {
                checklist_id = header.checklist_id,
                checklist_no = header.checklist_no,
                delivery_date = header.delivery_date,
                route_name = header.route_name,
                truck_name = header.truck_name,
                driver_name = header.driver_name,
                helper_name = header.helper_name,
                status = header.status,
                remarks = header.remarks,
                lines = lines
            };
        }

        public async Task<object> ReopenAsync(long checklistId)
        {
            if (checklistId <= 0)
                throw new Exception("Invalid checklist_id.");

            var header = await _context.DeliveryChecklistHeaders
                .FirstOrDefaultAsync(x => x.checklist_id == checklistId);

            if (header == null)
                throw new Exception("Checklist not found.");

            if ((header.status ?? "").ToUpper() != "LOADING")
                throw new Exception("Only LOADING checklist can be reopened.");

            var status = (header.status ?? "").ToUpper();

            if (status == "PARTIAL")
                throw new Exception("PARTIAL checklist cannot be reopened.");

            if (status != "LOADING")
                throw new Exception("Only LOADING checklist can be reopened.");

            var hasReleased = await _context.DeliveryChecklistLines
                .AnyAsync(x =>
                    x.checklist_id == checklistId &&
                    !x.is_deleted &&
                    x.released_qty > 0);

            if (hasReleased)
                throw new Exception("Cannot reopen checklist because loading already started.");

            header.status = "READY";


            var lines = await _context.DeliveryChecklistLines
    .Where(x => x.checklist_id == checklistId && !x.is_deleted && x.remaining_qty > 0)
    .ToListAsync();

            foreach (var line in lines)
            {
                if ((line.status ?? "").ToUpper() != "COMPLETED")
                {
                    line.status = "READY";
                    line.updated_at = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return new
            {
                success = true,
                message = "Checklist reopened successfully."
            };
        }
    }
}