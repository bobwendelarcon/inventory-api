using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    using Microsoft.EntityFrameworkCore;

    public class DailyOrderService
    {
        private readonly AppDbContext _context;

        public DailyOrderService(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // GET ALL (FOR TABLE)
        // =========================================
        public async Task<DailyOrderListResponse> GetAllAsync(
     string? className,
     int? year,
     string? month,
     string? status,
     string? search)
        {
            var headers = await _context.DailyOrderHeaders
     .Where(h => !h.is_deleted)
     .Include(h => h.Lines)
     .ToListAsync();

            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;

            var products = await _context.Products.ToListAsync();
            var productDict = products.ToDictionary(x => x.product_id, x => x);

          var result = headers
    .SelectMany(h => h.Lines.Select(l =>
    {
        productDict.TryGetValue(l.product_id, out var p);

        return new DailyOrderListDto
        {
            OrderId = h.order_id,
            ClassName = h.class_name ?? "",
            Year = h.date_ordered?.Year ?? 0,
            Month = h.date_ordered?.ToString("MMMM") ?? "",
            OrderNo = h.order_no,
            CustomerName = h.customer_name,
            ProductName = p?.product_name ?? l.product_name,
            RequiredQty = l.required_qty,
            AllocatedQty = l.allocated_qty,
            RemainingQty = l.remaining_qty,
            DispatchedQty = l.dispatched_qty,

            // 🔥 ADD THESE
            Uom = p?.uom ?? "",
            PackQty = p?.pack_qty ?? 0,
            PackUom = p?.pack_uom ?? "",

            AllocationStatus = l.allocation_status,
            DateOrdered = h.date_ordered,
            DeliveryDate = h.delivery_date,
            DateDelivered = h.date_delivered,
            Status = h.status,
            LineStatus = l.status,
            SpecialInstructions = h.special_instructions,

            AgingDays =
                h.delivery_date.HasValue
                ? (
                    h.date_delivered.HasValue
                        ? (h.date_delivered.Value.Date - h.delivery_date.Value.Date).Days
                        : (today - h.delivery_date.Value.Date).Days
                  )
                : 0
        };
    }))
    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(className))
                result = result.Where(x => x.ClassName == className);

            if (year.HasValue)
                result = result.Where(x => x.Year == year.Value);

            if (!string.IsNullOrWhiteSpace(month))
                result = result.Where(x => x.Month == month);

            //if (string.IsNullOrWhiteSpace(status))
            //{
            //    result = result.Where(x => (x.Status ?? "").Trim().ToUpper() != "COMPLETED");
            //}
            //else
            //{
            //    var selectedStatus = status.Trim().ToUpper();
            //    result = result.Where(x => (x.Status ?? "").Trim().ToUpper() == selectedStatus);
            //}

            if (!string.IsNullOrWhiteSpace(status))
            {
                var selectedStatus = status.Trim().ToUpper();

                if (selectedStatus == "ALL_EXCL_COMPLETED")
                {
                    result = result.Where(x =>
                        (x.Status ?? "").Trim().ToUpper() != "COMPLETED"
                    );
                }
                else if (selectedStatus == "ALL")
                {
                    // no filter → show everything
                }
                else
                {
                    result = result.Where(x =>
                        (x.Status ?? "").Trim().ToUpper() == selectedStatus
                    );
                }
            }
            else
            {
                // fallback (default behavior)
                result = result.Where(x =>
                    (x.Status ?? "").Trim().ToUpper() != "COMPLETED"
                );
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                result = result.Where(x =>
                    (x.CustomerName ?? "").ToLower().Contains(keyword) ||
                    (x.OrderNo ?? "").ToLower().Contains(keyword) ||
                    (x.ProductName ?? "").ToLower().Contains(keyword));
            }

            var filteredList = result.ToList();

            var grouped = filteredList.GroupBy(x => x.OrderId);

            var summary = new DailyOrderSummaryDto
            {
                TotalOrders = grouped.Count(),

                ForAllocation = grouped.Count(g =>
                    (g.First().Status ?? "").Trim().ToUpper() == "FOR ALLOCATION"
     ),

                Allocated = grouped.Count(g =>
                    (g.First().Status ?? "").Trim().ToUpper() == "ALLOCATED"
     ),

                Partial = grouped.Count(g =>
                    (g.First().Status ?? "").Trim().ToUpper() == "PARTIALLY ALLOCATED"
     ),

                ReadyDispatch = grouped.Count(g =>
                    (g.First().Status ?? "").Trim().ToUpper() == "READY FOR DISPATCH"
     ),

                PartiallyDelivered = grouped.Count(g =>
                    (g.First().Status ?? "").Trim().ToUpper() == "PARTIALLY DELIVERED"
     ),

                Completed = grouped.Count(g =>
                    (g.First().Status ?? "").Trim().ToUpper() == "COMPLETED"
     ),

                Overdue = grouped.Count(g =>
                    g.Any(x =>
                        x.DeliveryDate.HasValue &&
                        x.DeliveryDate.Value.Date < today &&
                        (g.First().Status ?? "").Trim().ToUpper() != "COMPLETED"
                    )
     )
            };


            return new DailyOrderListResponse
            {
                Summary = summary,
                Data = filteredList
            };
        }



        // =========================================
        // GET DETAILS (VIEW MODAL)
        // =========================================
        public async Task<DailyOrderDetailsDto> GetByIdAsync(long orderId)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                    .ThenInclude(l => l.Allocations)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            string? sourceBranchName = null;

            if (!string.IsNullOrWhiteSpace(order.source_branch_id))
            {
                sourceBranchName = await _context.Branches
                    .Where(b => b.branch_id == order.source_branch_id)
                    .Select(b => b.branch_name)
                    .FirstOrDefaultAsync();
            }

            var products = await _context.Products.ToListAsync();
            var productDict = products.ToDictionary(x => x.product_id, x => x);
            var lotStocks = await _context.ProductLotNumbers
    .Where(x => !x.is_deleted)
    .ToListAsync();

            var dto = new DailyOrderDetailsDto
            {
                OrderId = order.order_id,
                OrderNo = order.order_no,
                CustomerName = order.customer_name,
                ClassName = order.class_name,
                SourceBranchId = order.source_branch_id,
                SourceBranchName = sourceBranchName,
                RouteName = order.route_name,
                DeliveryDate = order.delivery_date,
                SpecialInstructions = order.special_instructions,
                Status = order.status,
                Lines = order.Lines.Select(l =>
                {
                    productDict.TryGetValue(l.product_id, out var p);

                    var availableStock = lotStocks
      .Where(x =>
          x.product_id == l.product_id &&
          x.branch_id == order.source_branch_id)
      .Sum(lot =>
      {
          var reservedQty = _context.DailyOrderAllocations
              .Where(a =>
                  a.product_id == l.product_id &&
                  a.branch_id == lot.branch_id &&
                  a.lot_no == lot.lot_no &&
                 // a.order_line_id != l.order_line_id &&
                  a.allocated_qty > 0)
              .Sum(a => (decimal?)a.allocated_qty) ?? 0;

          return Math.Max(0, lot.quantity - reservedQty);
      });


                    return new DailyOrderLineDto
                    {
                        OrderLineId = l.order_line_id,
                        ProductName = p?.product_name ?? l.product_name,
                        //RequiredQty = l.required_qty,
                        //AllocatedQty = l.allocated_qty,
                        //// AvailableBeforeAllocation = l.required_qty,
                        //AvailableBeforeAllocation = l.Allocations.Sum(a => a.available_qty),
                        RequiredQty = l.required_qty,
                        AllocatedQty = l.allocated_qty,
                        RemainingQty = l.remaining_qty,
                        AvailableBeforeAllocation = availableStock,
                        AllocationResult = l.allocation_status,

                        Uom = p?.uom ?? "",
                        PackQty = p?.pack_qty ?? 0,
                        PackUom = p?.pack_uom ?? "",

                        Allocations = l.Allocations
                            .OrderBy(a => a.priority_rank)
                            .Select(a => new DailyOrderAllocationDto
                            {
                                BranchId = a.branch_id,
                                LotNo = a.lot_no,
                                ManufacturingDate = a.manufacturing_date,
                                ExpirationDate = a.expiration_date,
                                OnHandQty = a.on_hand_qty,
                                ReservedQty = a.reserved_qty,
                                AvailableQty = a.available_qty,
                                AllocatedQty = a.allocated_qty,
                                PriorityRank = a.priority_rank,

                                Uom = p?.uom ?? "",
                                PackQty = p?.pack_qty ?? 0,
                                PackUom = p?.pack_uom ?? ""
                            })
                            .ToList()
                    };
                }).ToList()
            };

            return dto;
        }

        // =========================================
        // CREATE ORDER
        // =========================================
        public async Task<object> CreateAsync(CreateDailyOrderRequest request)
        {
            if (request == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new Exception("Customer is required.");

            if (string.IsNullOrWhiteSpace(request.CustomerName))
                throw new Exception("Customer name is required.");

            if (string.IsNullOrWhiteSpace(request.SourceBranchId))
                throw new Exception("Source branch is required.");

            if (request.Lines == null || !request.Lines.Any())
                throw new Exception("At least one order line is required.");

            string orderNo = await GenerateOrderNoAsync();

            var header = new DailyOrderHeader
            {
                order_no = orderNo,
                customer_id = request.CustomerId,
                customer_name = request.CustomerName,
                source_branch_id = request.SourceBranchId,
                class_name = request.ClassName,
                route_name = request.RouteName,
                date_ordered = request.DateOrdered,
                delivery_date = request.DeliveryDate,
                special_instructions = request.SpecialInstructions,
                status = "For Allocation",
                created_by = request.CreatedBy,
                created_at = DateTime.UtcNow
            };

            _context.DailyOrderHeaders.Add(header);
            await _context.SaveChangesAsync();

            foreach (var line in request.Lines)
            {
                var entity = new DailyOrderLine
                {
                    order_id = header.order_id,
                    product_id = line.ProductId,
                    product_name = line.ProductName,
                    required_qty = line.RequiredQty,
                    allocated_qty = 0,
                    remaining_qty = line.RequiredQty,
                    allocation_status = "Not Allocated",
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow,
                    dispatched_qty = 0,
                    status = "PENDING"
                };

                _context.DailyOrderLines.Add(entity);
            }

            await _context.SaveChangesAsync();

            return new
            {
                OrderId = header.order_id,
                OrderNo = orderNo,
                Message = "Order created successfully."
            };
        }

        // =========================================
        // ALLOCATE (FEFO)
        // =========================================

        public async Task<object> AllocateAsync(long orderId)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId);

            if (order == null)
                throw new Exception("Order not found.");

            foreach (var line in order.Lines)
            {
                // clear old allocations for this line before re-running FEFO
                var existingAllocations = await _context.DailyOrderAllocations
                    .Where(a => a.order_line_id == line.order_line_id)
                    .ToListAsync();

                if (existingAllocations.Any())
                {
                    _context.DailyOrderAllocations.RemoveRange(existingAllocations);
                    await _context.SaveChangesAsync();
                }

                // remaining balance still to deliver
                line.remaining_qty = Math.Max(0, line.required_qty - line.dispatched_qty);

                if (line.remaining_qty <= 0)
                {
                    line.allocated_qty = 0;
                    line.allocation_status = "Completed";
                    line.updated_at = DateTime.UtcNow;
                    continue;
                }

                // only allocate what is still undelivered
                var required = line.remaining_qty;
                decimal allocatedTotal = 0;

                if (string.IsNullOrWhiteSpace(order.source_branch_id))
                    throw new Exception("Order source branch is missing.");

                var lots = await _context.ProductLotNumbers
                    .Where(x =>
                        x.product_id == line.product_id &&
                        x.branch_id == order.source_branch_id &&
                        x.quantity > 0 &&
                        !x.is_deleted)
                    .OrderBy(x => x.expiration_date)
                    .ThenBy(x => x.manufacturing_date)
                    .ThenBy(x => x.lot_no)
                    .ToListAsync();

                int priority = 1;

                foreach (var lot in lots)
                {
                    if (allocatedTotal >= required)
                        break;

                    decimal alreadyAllocated = await _context.DailyOrderAllocations
                        .Where(a =>
                            a.product_id == line.product_id &&
                            a.branch_id == lot.branch_id &&
                            a.lot_no == lot.lot_no &&
                            a.order_line_id != line.order_line_id &&
                            a.allocated_qty > 0
                        )
                        .SumAsync(a => (decimal?)a.allocated_qty) ?? 0;

                    var available = lot.quantity - alreadyAllocated;
                    if (available <= 0)
                        continue;

                    var allocateQty = Math.Min(available, required - allocatedTotal);
                    if (allocateQty <= 0)
                        continue;

                    var allocation = new DailyOrderAllocation
                    {
                        order_line_id = line.order_line_id,
                        product_id = line.product_id,
                        branch_id = lot.branch_id,
                        lot_no = lot.lot_no,
                        manufacturing_date = lot.manufacturing_date,
                        expiration_date = lot.expiration_date,
                        on_hand_qty = lot.quantity,
                        reserved_qty = alreadyAllocated,
                        available_qty = available,
                        allocated_qty = allocateQty,
                        priority_rank = priority++,
                        created_at = DateTime.UtcNow
                    };

                    _context.DailyOrderAllocations.Add(allocation);
                    allocatedTotal += allocateQty;
                }

                line.allocated_qty = allocatedTotal;

                var remainingToAllocate = Math.Max(0, line.remaining_qty - line.allocated_qty);

                if (line.allocated_qty == 0)
                {
                    line.allocation_status = "NO STOCK";
                }
                else if (remainingToAllocate > 0)
                {
                    line.allocation_status = "PARTIAL";
                }
                else
                {
                    line.allocation_status = "FULLY ALLOCATED";
                }

                line.updated_at = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            bool allDelivered = order.Lines.All(x => x.dispatched_qty >= x.required_qty);
            bool allAllocatedForRemaining = order.Lines.All(x =>
                x.remaining_qty <= 0 || (x.allocated_qty > 0 && (x.remaining_qty - x.allocated_qty) <= 0));
            bool anyAllocated = order.Lines.Any(x => x.allocated_qty > 0);
            bool anyDelivered = order.Lines.Any(x => x.dispatched_qty > 0);

            order.status = allDelivered
                ? "COMPLETED"
                : anyDelivered
                    ? "PARTIALLY DELIVERED"
                    : allAllocatedForRemaining
                        ? "Allocated"
                        : anyAllocated
                            ? "Partially Allocated"
                            : "For Allocation";

            order.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new { Message = "Allocation completed." };
        }


        //    public async Task<object> AllocateAsync(long orderId)
        //    {
        //        var order = await _context.DailyOrderHeaders
        //            .Include(h => h.Lines)
        //            .FirstOrDefaultAsync(x => x.order_id == orderId);

        //        if (order == null)
        //            throw new Exception("Order not found.");

        //        foreach (var line in order.Lines)
        //        {
        //            // clear old allocations for this line before re-running FEFO
        //            var existingAllocations = await _context.DailyOrderAllocations
        //                .Where(a => a.order_line_id == line.order_line_id)
        //                .ToListAsync();

        //            if (existingAllocations.Any())
        //            {
        //                _context.DailyOrderAllocations.RemoveRange(existingAllocations);
        //                await _context.SaveChangesAsync();
        //            }

        //            // remaining balance still to deliver
        //            line.remaining_qty = Math.Max(0, line.required_qty - line.dispatched_qty);

        //            if (line.remaining_qty <= 0)
        //            {
        //                line.allocated_qty = 0;
        //                line.allocation_status = "Completed";
        //                line.updated_at = DateTime.UtcNow;
        //                continue;
        //            }


        //            // only allocate what is still undelivered
        //            var required = line.remaining_qty;
        //            decimal allocatedTotal = 0;
        //            if (string.IsNullOrWhiteSpace(order.source_branch_id))
        //                throw new Exception("Order source branch is missing.");

        //            var lots = await _context.ProductLotNumbers
        //                .Where(x =>
        //                    x.product_id == line.product_id &&
        //                    x.branch_id == order.source_branch_id &&   // ✅ BRANCH FILTER
        //                    x.quantity > 0 &&
        //                    !x.is_deleted)
        //                .OrderBy(x => x.expiration_date)
        //                .ThenBy(x => x.manufacturing_date)
        //                .ThenBy(x => x.lot_no)
        //                .ToListAsync();

        //            int priority = 1;

        //            foreach (var lot in lots)
        //            {
        //                if (allocatedTotal >= required)
        //                    break;
        //                decimal alreadyAllocated = await _context.DailyOrderAllocations
        //.Where(a =>
        //    a.product_id == line.product_id &&
        //     a.branch_id == order.source_branch_id &&   // ✅ NEW
        //    a.lot_no == lot.lot_no &&
        //    a.order_line_id != line.order_line_id &&   // 🔥 IMPORTANT
        //    a.allocated_qty > 0
        //)
        //.SumAsync(a => (decimal?)a.allocated_qty) ?? 0;



        //                var available = lot.quantity - alreadyAllocated;
        //                if (available <= 0)
        //                    continue;

        //                var allocateQty = Math.Min(available, required - allocatedTotal);
        //                if (allocateQty <= 0)
        //                    continue;

        //                var allocation = new DailyOrderAllocation
        //                {
        //                    order_line_id = line.order_line_id,
        //                    product_id = line.product_id,
        //                    branch_id = lot.branch_id,   // ✅ NEW
        //                    lot_no = lot.lot_no,
        //                    manufacturing_date = lot.manufacturing_date,
        //                    expiration_date = lot.expiration_date,
        //                    on_hand_qty = lot.quantity,
        //                    reserved_qty = alreadyAllocated,
        //                    available_qty = available,
        //                    allocated_qty = allocateQty,
        //                    priority_rank = priority++,
        //                    created_at = DateTime.UtcNow
        //                };

        //                _context.DailyOrderAllocations.Add(allocation);
        //                allocatedTotal += allocateQty;
        //            }

        //            line.allocated_qty = allocatedTotal;

        //            var remainingToAllocate = Math.Max(0, line.remaining_qty - line.allocated_qty);

        //            if (line.allocated_qty == 0)
        //                line.allocation_status = "Not Allocated";
        //            else if (remainingToAllocate > 0)
        //                line.allocation_status = "Partial";
        //            else
        //                line.allocation_status = "Fully Allocated";

        //            line.updated_at = DateTime.UtcNow;
        //        }

        //        await _context.SaveChangesAsync();

        //        bool allDelivered = order.Lines.All(x => x.dispatched_qty >= x.required_qty);
        //        bool allAllocatedForRemaining = order.Lines.All(x =>
        //            x.remaining_qty <= 0 || (x.allocated_qty > 0 && (x.remaining_qty - x.allocated_qty) <= 0));
        //        bool anyAllocated = order.Lines.Any(x => x.allocated_qty > 0);
        //        bool anyDelivered = order.Lines.Any(x => x.dispatched_qty > 0);

        //        order.status = allDelivered
        //            ? "COMPLETED"
        //            : anyDelivered
        //                ? "PARTIALLY DELIVERED"
        //                : allAllocatedForRemaining
        //                    ? "Allocated"
        //                    : anyAllocated
        //                        ? "Partially Allocated"
        //                        : "For Allocation";

        //        order.updated_at = DateTime.UtcNow;

        //        await _context.SaveChangesAsync();

        //        return new { Message = "Allocation completed." };
        //    }

        // =========================================
        // READY FOR DISPATCH
        // =========================================
        public async Task<object> MarkReadyForDispatchAsync(long orderId)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId);

            if (order == null)
                throw new Exception("Order not found.");

            if (order.status == "Ready for Dispatch")
                throw new Exception("Order is already ready for dispatch.");

            if (order.status == "Completed" || order.status == "Cancelled")
                throw new Exception("This order can no longer be dispatched.");

            // allow full or partial allocation
            if (!order.Lines.Any(l => l.allocated_qty > 0))
                throw new Exception("Order has no allocated quantity to dispatch.");

            order.status = "Ready for Dispatch";
            order.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Order marked as Ready for Dispatch."
            };
        }

        // =========================================
        // GENERATE ORDER NUMBER
        // =========================================
        private async Task<string> GenerateOrderNoAsync()
        {
            var today = DateTime.Today;

            var count = await _context.DailyOrderHeaders
                .CountAsync(x => x.created_at.Date == today);

            return $"DO-{today:yyyyMMdd}-{(count + 1):D3}";
        }
        // =========================================
        // UPDATE DAILY ORDER
        // =========================================

        public async Task<object> UpdateHeaderAsync(long orderId, UpdateDailyOrderRequest request)
        {
            var order = await _context.DailyOrderHeaders
                .FirstOrDefaultAsync(x => x.order_id == orderId);

            if (order == null)
                throw new Exception("Order not found.");

            if (order.status == "Ready for Dispatch" || order.status == "Completed")
                throw new Exception("This order can no longer be edited.");

            order.customer_name = request.CustomerName;
            order.class_name = request.ClassName;
            order.route_name = request.RouteName;
            order.delivery_date = request.DeliveryDate;
            order.special_instructions = request.SpecialInstructions;
            order.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Order updated successfully."
            };
        }

        // =========================================
        // DELETE DAILY ORDER
        // =========================================
        public async Task<object> DeleteAsync(long orderId)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId);

            if (order == null)
                throw new Exception("Order not found.");

            if (order.is_deleted)
                throw new Exception("Order is already deleted.");

            if (order.status == "Ready for Dispatch" ||
                order.status == "Partially Dispatched" ||
                order.status == "Completed")
            {
                throw new Exception("This order can no longer be deleted.");
            }

            // optional: remove allocations for this order lines
            var lineIds = order.Lines.Select(x => x.order_line_id).ToList();

            var allocations = await _context.DailyOrderAllocations
                .Where(a => lineIds.Contains(a.order_line_id))
                .ToListAsync();

            if (allocations.Any())
            {
                _context.DailyOrderAllocations.RemoveRange(allocations);
            }

            order.is_deleted = true;
            order.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Order deleted successfully."
            };
        }




    }
}