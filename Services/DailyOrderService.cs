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
        public async Task<List<DailyOrderListDto>> GetAllAsync(
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

            var result = headers
                .SelectMany(h => h.Lines.Select(l => new DailyOrderListDto
                {
                    OrderId = h.order_id,
                    ClassName = h.class_name ?? "",
                    Year = h.date_ordered?.Year ?? 0,
                    Month = h.date_ordered?.ToString("MMMM") ?? "",
                    OrderNo = h.order_no,
                    CustomerName = h.customer_name,
                    ProductName = l.product_name,
                    RequiredQty = l.required_qty,
                    AllocatedQty = l.allocated_qty,
                    RemainingQty = l.remaining_qty,
                    AllocationStatus = l.allocation_status,
                    DateOrdered = h.date_ordered,
                    DeliveryDate = h.delivery_date,
                    DateDelivered = h.date_delivered,
                    Status = h.status,
                    SpecialInstructions = h.special_instructions
                }))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(className))
                result = result.Where(x => x.ClassName == className);

            if (year.HasValue)
                result = result.Where(x => x.Year == year.Value);

            if (!string.IsNullOrWhiteSpace(month))
                result = result.Where(x => x.Month == month);

            if (!string.IsNullOrWhiteSpace(status))
                result = result.Where(x => x.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                result = result.Where(x =>
                    (x.CustomerName ?? "").ToLower().Contains(keyword) ||
                    (x.OrderNo ?? "").ToLower().Contains(keyword) ||
                    (x.ProductName ?? "").ToLower().Contains(keyword));
            }

            return result.ToList();
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

            var dto = new DailyOrderDetailsDto
            {
                OrderId = order.order_id,
                OrderNo = order.order_no,
                CustomerName = order.customer_name,
                ClassName = order.class_name,
                RouteName = order.route_name,
                DeliveryDate = order.delivery_date,
                SpecialInstructions = order.special_instructions,
               
                Status = order.status,
                Lines = order.Lines.Select(l => new DailyOrderLineDto
                {
                    OrderLineId = l.order_line_id,
                    ProductName = l.product_name,
                    RequiredQty = l.required_qty,
                    AllocatedQty = l.allocated_qty,
                    AvailableBeforeAllocation = l.required_qty, // temporary placeholder
                    AllocationResult = l.allocation_status,
                    Allocations = l.Allocations
                        .OrderBy(a => a.priority_rank)
                        .Select(a => new DailyOrderAllocationDto
                        {
                            LotNo = a.lot_no,
                            ManufacturingDate = a.manufacturing_date,
                            ExpirationDate = a.expiration_date,
                            OnHandQty = a.on_hand_qty,
                            ReservedQty = a.reserved_qty,
                            AvailableQty = a.available_qty,
                            AllocatedQty = a.allocated_qty,
                            PriorityRank = a.priority_rank
                        })
                        .ToList()
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

            string orderNo = await GenerateOrderNoAsync();

            var header = new DailyOrderHeader
            {
                order_no = orderNo,
                customer_name = request.CustomerName,
                class_name = request.ClassName,
                route_name = request.RouteName,
                date_ordered = request.DateOrdered,
                delivery_date = request.DeliveryDate,
                special_instructions = request.SpecialInstructions,
                status = "For Allocation",
                created_by = request.CreatedBy,
                created_at = DateTime.Now
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
                    allocation_status = "Not Allocated"
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

                var required = line.required_qty;
                decimal allocatedTotal = 0;

                var lots = await _context.ProductLotNumbers
                    .Where(x => x.product_id == line.product_id && x.quantity > 0)
                    .OrderBy(x => x.expiration_date)
                    .ThenBy(x => x.manufacturing_date)
                    .GroupBy(x => x.lot_no)
                    .Select(g => g.First())
                    .ToListAsync();

                int priority = 1;

                foreach (var lot in lots)
                {
                    if (allocatedTotal >= required)
                        break;

                    decimal alreadyAllocated = await _context.DailyOrderAllocations
                        .Where(a => a.product_id == line.product_id && a.lot_no == lot.lot_no)
                        .SumAsync(a => (decimal?)a.allocated_qty) ?? 0;

                    var available = lot.quantity - alreadyAllocated;

                    var allocateQty = Math.Min(available, required - allocatedTotal);

                    if (allocateQty <= 0)
                        continue;

                    var allocation = new DailyOrderAllocation
                    {
                        order_line_id = line.order_line_id,
                        product_id = line.product_id,
                        lot_no = lot.lot_no,
                        manufacturing_date = lot.manufacturing_date,
                        expiration_date = lot.expiration_date,
                        on_hand_qty = lot.quantity,
                        reserved_qty = alreadyAllocated,
                        available_qty = available,
                        allocated_qty = allocateQty,
                        priority_rank = priority++,
                        created_at = DateTime.Now
                    };

                    _context.DailyOrderAllocations.Add(allocation);

                    allocatedTotal += allocateQty;
                }

                line.allocated_qty = allocatedTotal;
                line.remaining_qty = line.required_qty - allocatedTotal;

                if (allocatedTotal == 0)
                    line.allocation_status = "Not Allocated";
                else if (allocatedTotal < line.required_qty)
                    line.allocation_status = "Partial";
                else
                    line.allocation_status = "Fully Allocated";
            }

            await _context.SaveChangesAsync();

            if (order.Lines.All(x => x.remaining_qty == 0))
                order.status = "Allocated";
            else if (order.Lines.Any(x => x.allocated_qty > 0))
                order.status = "Partially Allocated";
            else
                order.status = "For Allocation";

            order.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();

            return new { Message = "Allocation completed." };
        }

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
            order.updated_at = DateTime.Now;

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
            order.updated_at = DateTime.Now;

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
            order.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Order deleted successfully."
            };
        }




    }
}