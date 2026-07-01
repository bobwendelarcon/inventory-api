using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    using Microsoft.EntityFrameworkCore;
    using MySqlConnector;

    public class DailyOrderService
    {
        private readonly AppDbContext _context;
        private readonly string _connectionString;

        public DailyOrderService(
    AppDbContext context,
    IConfiguration configuration)
        {
            _context = context;

            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new Exception("Connection string not found.");
        }

        // =========================================
        // GET ALL (FOR TABLE)
        // =========================================
        public async Task<DailyOrderListResponse> GetAllAsync(
       string? className,
       int? year,
       string? month,
       string? status,
       string? search,
       string? sortBy,
       string? sortDir,
       int page = 1,
       int pageSize = 50)
        {
            var headers = await _context.DailyOrderHeaders
     .Where(h => !h.is_deleted)
     .Include(h => h.Lines)
     .ToListAsync();

            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;

            var products = await _context.Products.ToListAsync();
            var productDict = products.ToDictionary(x => x.product_id, x => x);

            var users = await _context.Users
    .AsNoTracking()
    .ToListAsync();

            var userDict = users
     .SelectMany(u => new[]
     {
        new { Key = (u.user_id ?? "").Trim().ToLower(), Value = u.full_name },
        new { Key = (u.username ?? "").Trim().ToLower(), Value = u.full_name },
        new { Key = (u.full_name ?? "").Trim().ToLower(), Value = u.full_name }
     })
     .Where(x => !string.IsNullOrWhiteSpace(x.Key))
     .GroupBy(x => x.Key)
     .ToDictionary(
         g => g.Key,
         g => g.First().Value
     );

            var result = headers
    .SelectMany(h => h.Lines.Select(l =>
    {
        productDict.TryGetValue(l.product_id, out var p);

        return new DailyOrderListDto
        {
            OrderId = h.order_id,
            OrderLineId = l.order_line_id,
            ClassName = h.class_name ?? "",
            Year = h.date_ordered?.Year ?? 0,
            Month = h.date_ordered?.ToString("MMMM") ?? "",
            OrderNo = h.order_no,
            CustomerName = h.customer_name,
            ProductName = p?.product_name ?? l.product_name,
            ProductDescription = p?.product_description ?? "",
            RequiredQty = l.required_qty,
            AllocatedQty = l.allocated_qty,
            RemainingQty = Math.Max(0, l.required_qty - l.dispatched_qty),
            DispatchedQty = l.dispatched_qty,
            CreatedBy = userDict.TryGetValue(
        (h.created_by ?? "").Trim().ToLower(),
        out var fullName)
    ? fullName
    : h.created_by,

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
                    // no filter
                }
                else if (selectedStatus == "OVERDUE")
                {
                    result = result.Where(x =>
                        x.DeliveryDate.HasValue &&
                        x.DeliveryDate.Value.Date < today &&
                        (x.Status ?? "").Trim().ToUpper() != "COMPLETED" &&
                        (x.Status ?? "").Trim().ToUpper() != "CANCELLED"
                    );
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

            var isDesc = (sortDir ?? "asc").Trim().ToLower() == "desc";
            var sortKey = (sortBy ?? "deliveryDate").Trim();

            filteredList = sortKey switch
            {
                "dateOrdered" => isDesc
                    ? filteredList.OrderByDescending(x => x.DateOrdered).ToList()
                    : filteredList.OrderBy(x => x.DateOrdered).ToList(),

                "customer" => isDesc
                    ? filteredList.OrderByDescending(x => x.CustomerName).ToList()
                    : filteredList.OrderBy(x => x.CustomerName).ToList(),

                "product" => isDesc
                    ? filteredList.OrderByDescending(x => x.ProductName).ToList()
                    : filteredList.OrderBy(x => x.ProductName).ToList(),

                "remainingQty" => isDesc
                    ? filteredList.OrderByDescending(x => x.RemainingQty).ToList()
                    : filteredList.OrderBy(x => x.RemainingQty).ToList(),

                "requiredQty" => isDesc
                    ? filteredList.OrderByDescending(x => x.RequiredQty).ToList()
                    : filteredList.OrderBy(x => x.RequiredQty).ToList(),

                _ => isDesc
                    ? filteredList.OrderByDescending(x => x.DeliveryDate ?? DateTime.MaxValue).ToList()
                    : filteredList.OrderBy(x => x.DeliveryDate ?? DateTime.MaxValue).ToList()
            };

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
        (g.First().Status ?? "").Trim().ToUpper() != "COMPLETED" &&
        (g.First().Status ?? "").Trim().ToUpper() != "CANCELLED"
    )
)
            };


            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;

            var totalRecords = filteredList.Count;

            var pagedData = filteredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new DailyOrderListResponse
            {
                Summary = summary,
                Data = pagedData,
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize,
                HasMore = page * pageSize < totalRecords
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

            var branches = await _context.Branches.ToListAsync();

            var branchDict = branches.ToDictionary(
                x => x.branch_id,
                x => x.branch_name
            );

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

                    var warehouseAvailableStocks = lotStocks
                        .Where(x => x.product_id == l.product_id)
                        .GroupBy(x => x.branch_id)
                        .Select(g =>
                        {
                            var branchId = g.Key;

                            var availableQty = g.Sum(lot =>
                            {
                                var reservedQty = _context.DailyOrderAllocations
                                    .Where(a =>
                                        a.product_id == l.product_id &&
                                        a.branch_id == lot.branch_id &&
                                        a.lot_no == lot.lot_no &&
                                        a.allocated_qty > 0)
                                    .Join(_context.DailyOrderLines,
                                        a => a.order_line_id,
                                        dl => dl.order_line_id,
                                        (a, dl) => new { a, dl })
                                    .Join(_context.DailyOrderHeaders,
                                        x => x.dl.order_id,
                                        h => h.order_id,
                                        (x, h) => new { x.a, x.dl, h })
                                    .Where(x => !x.h.is_deleted)
                                    .Sum(x => (decimal?)x.a.allocated_qty) ?? 0;

                                return Math.Max(0, lot.quantity - reservedQty);
                            });

                            return new WarehouseAvailableDto
                            {
                                BranchId = branchId,
                                WarehouseName =
                                    !string.IsNullOrWhiteSpace(branchId) &&
                                    branchDict.ContainsKey(branchId)
                                        ? branchDict[branchId]
                                        : branchId ?? "-",
                                AvailableQty = availableQty,
                                IsPreferred = branchId == order.source_branch_id
                            };
                        })
                        .Where(x => x.AvailableQty > 0)
                        .OrderBy(x => x.IsPreferred ? 0 : 1)
                        .ThenByDescending(x => x.AvailableQty)
                        .ToList();

                    var totalAvailableStock = warehouseAvailableStocks.Sum(x => x.AvailableQty);

                    return new DailyOrderLineDto
                    {
                        OrderLineId = l.order_line_id,
                        ProductName = p?.product_name ?? l.product_name,
                        ProductDescription = p?.product_description ?? "",

                        RequiredQty = l.required_qty,
                        AllocatedQty = l.allocated_qty,
                        UnallocatedQty = Math.Max(
                            0,
                            l.required_qty - l.dispatched_qty - l.allocated_qty
                        ),
                        RemainingQty = Math.Max(0, l.required_qty - l.dispatched_qty),

                        AvailableBeforeAllocation = totalAvailableStock,
                        TotalAvailableStock = totalAvailableStock,
                        WarehouseAvailableStocks = warehouseAvailableStocks,

                        AllocationResult = l.allocation_status,

                        Uom = p?.uom ?? "",
                        PackQty = p?.pack_qty ?? 0,
                        PackUom = p?.pack_uom ?? "",

                        Allocations = l.Allocations
    .OrderBy(a => a.priority_rank)
    .Select(a => new DailyOrderAllocationDto
    {
        BranchId = a.branch_id,
        WarehouseName =
            !string.IsNullOrWhiteSpace(a.branch_id) &&
            branchDict.ContainsKey(a.branch_id)
                ? branchDict[a.branch_id]
                : a.branch_id,

        LotNo = a.lot_no,
        ManufacturingDate = a.manufacturing_date,
        ExpirationDate = a.expiration_date,
        OnHandQty = a.on_hand_qty,
        ReservedQty = a.reserved_qty,
        AvailableQty = a.available_qty,
        AllocatedQty = a.allocated_qty,
        AllocationMode = a.allocation_mode,
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

        //        public async Task<DailyOrderDetailsDto> GetByIdAsync(long orderId)
        //        {
        //            var order = await _context.DailyOrderHeaders
        //                .Include(h => h.Lines)
        //                    .ThenInclude(l => l.Allocations)
        //                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

        //            if (order == null)
        //                throw new Exception("Order not found.");

        //            string? sourceBranchName = null;

        //            if (!string.IsNullOrWhiteSpace(order.source_branch_id))
        //            {
        //                sourceBranchName = await _context.Branches
        //                    .Where(b => b.branch_id == order.source_branch_id)
        //                    .Select(b => b.branch_name)
        //                    .FirstOrDefaultAsync();
        //            }

        //            var products = await _context.Products.ToListAsync();
        //            var productDict = products.ToDictionary(x => x.product_id, x => x);
        //            var lotStocks = await _context.ProductLotNumbers
        //    .Where(x => !x.is_deleted)
        //    .ToListAsync();

        //            var dto = new DailyOrderDetailsDto
        //            {
        //                OrderId = order.order_id,
        //                OrderNo = order.order_no,
        //                CustomerName = order.customer_name,
        //                ClassName = order.class_name,
        //                SourceBranchId = order.source_branch_id,
        //                SourceBranchName = sourceBranchName,
        //                RouteName = order.route_name,
        //                DeliveryDate = order.delivery_date,
        //                SpecialInstructions = order.special_instructions,
        //                Status = order.status,
        //                Lines = order.Lines.Select(l =>
        //                {
        //                    productDict.TryGetValue(l.product_id, out var p);

        //                    var availableStock = lotStocks
        //      .Where(x =>
        //          x.product_id == l.product_id &&
        //          x.branch_id == order.source_branch_id)
        //      .Sum(lot =>
        //      {
        //          var reservedQty = _context.DailyOrderAllocations
        //    .Where(a =>
        //        a.product_id == l.product_id &&
        //        a.branch_id == lot.branch_id &&
        //        a.lot_no == lot.lot_no &&
        //        a.allocated_qty > 0)
        //    .Join(_context.DailyOrderLines,
        //        a => a.order_line_id,
        //        dl => dl.order_line_id,
        //        (a, dl) => new { a, dl })
        //    .Join(_context.DailyOrderHeaders,
        //        x => x.dl.order_id,
        //        h => h.order_id,
        //        (x, h) => new { x.a, x.dl, h })
        //    .Where(x => !x.h.is_deleted)
        //    .Sum(x => (decimal?)x.a.allocated_qty) ?? 0;

        //          return Math.Max(0, lot.quantity - reservedQty);
        //      });


        //                    return new DailyOrderLineDto
        //                    {
        //                        OrderLineId = l.order_line_id,
        //                        ProductName = p?.product_name ?? l.product_name,
        //                        ProductDescription = p?.product_description ?? "",
        //                        //RequiredQty = l.required_qty,
        //                        //AllocatedQty = l.allocated_qty,
        //                        //// AvailableBeforeAllocation = l.required_qty,
        //                        //AvailableBeforeAllocation = l.Allocations.Sum(a => a.available_qty),
        //                        RequiredQty = l.required_qty,
        //                        AllocatedQty = l.allocated_qty,
        //                        UnallocatedQty = Math.Max(
        //    0,
        //    l.required_qty - l.dispatched_qty - l.allocated_qty
        //),
        //                        RemainingQty = l.remaining_qty,
        //                        AvailableBeforeAllocation = availableStock,
        //                        AllocationResult = l.allocation_status,

        //                        Uom = p?.uom ?? "",
        //                        PackQty = p?.pack_qty ?? 0,
        //                        PackUom = p?.pack_uom ?? "",

        //                        Allocations = l.Allocations
        //                            .OrderBy(a => a.priority_rank)
        //                            .Select(a => new DailyOrderAllocationDto
        //                            {
        //                                BranchId = a.branch_id,
        //                                LotNo = a.lot_no,
        //                                ManufacturingDate = a.manufacturing_date,
        //                                ExpirationDate = a.expiration_date,
        //                                OnHandQty = a.on_hand_qty,
        //                                ReservedQty = a.reserved_qty,
        //                                AvailableQty = a.available_qty,
        //                                AllocatedQty = a.allocated_qty,
        //                                AllocationMode = a.allocation_mode,
        //                                PriorityRank = a.priority_rank,

        //                                Uom = p?.uom ?? "",
        //                                PackQty = p?.pack_qty ?? 0,
        //                                PackUom = p?.pack_uom ?? ""
        //                            })
        //                            .ToList()
        //                    };
        //                }).ToList()
        //            };

        //            return dto;
        //        }

        public async Task<object> GetAvailableLotsAsync(long orderId)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            if (string.IsNullOrWhiteSpace(order.source_branch_id))
                throw new Exception("Order source branch is missing.");

            var products = await _context.Products.ToListAsync();
            var productDict = products.ToDictionary(x => x.product_id, x => x);

            var branches = await _context.Branches.ToListAsync();

            var branchDict = branches.ToDictionary(
                x => x.branch_id,
                x => x.branch_name
            );


            var result = new List<object>();

            foreach (var line in order.Lines)
            {
                productDict.TryGetValue(line.product_id, out var product);

                var lots = await _context.ProductLotNumbers
                    .Where(l =>
                        //l.product_id == line.product_id &&
                        //l.branch_id == order.source_branch_id &&
                        //l.quantity > 0 &&
                        //!l.is_deleted)
                        l.product_id == line.product_id &&
                        l.quantity > 0 &&
                        !l.is_deleted)
                    //.OrderBy(l => l.expiration_date)
                    //.ThenBy(l => l.manufacturing_date)
                    //.ThenBy(l => l.lot_no)
                   
                    .OrderBy(l => l.branch_id == order.source_branch_id ? 0 : 1)
                    .ThenBy(l => l.expiration_date)
                    .ThenBy(l => l.manufacturing_date)
                    .ThenBy(l => l.lot_no)
                     .ToListAsync();

                var lotDtos = new List<object>();

                foreach (var lot in lots)
                {
                    var reservedQty = await _context.DailyOrderAllocations
                        .Where(a =>
                            a.product_id == line.product_id &&
                            a.branch_id == lot.branch_id &&
                            a.lot_no == lot.lot_no &&
                            a.order_line_id != line.order_line_id &&
                            a.allocated_qty > 0)
                        .Join(_context.DailyOrderLines,
                            a => a.order_line_id,
                            dl => dl.order_line_id,
                            (a, dl) => new { a, dl })
                        .Join(_context.DailyOrderHeaders,
                            x => x.dl.order_id,
                            h => h.order_id,
                            (x, h) => new { x.a, x.dl, h })
                        .Where(x => !x.h.is_deleted)
                        .SumAsync(x => (decimal?)x.a.allocated_qty) ?? 0;

                    var currentAllocated = await _context.DailyOrderAllocations
                        .Where(a =>
                            a.order_line_id == line.order_line_id &&
                            a.lot_no == lot.lot_no &&
                            a.branch_id == lot.branch_id)
                        .SumAsync(a => (decimal?)a.allocated_qty) ?? 0;

                    var available = Math.Max(0, lot.quantity - reservedQty);

                    //update 06192026

                    //lotDtos.Add(new
                    //{
                    //    OrderLineId = line.order_line_id,
                    //    ProductId = line.product_id,
                    //    ProductName = product?.product_name ?? line.product_name,

                    //    LotNo = lot.lot_no,
                    //    ManufacturingDate = lot.manufacturing_date,
                    //    ExpirationDate = lot.expiration_date,

                    //    OnHandQty = lot.quantity,
                    //    ReservedQty = reservedQty,
                    //    AvailableQty = available,

                    //    ExistingAllocatedQty = currentAllocated,

                    //    Uom = product?.uom ?? "",
                    //    PackQty = product?.pack_qty ?? 0,
                    //    PackUom = product?.pack_uom ?? ""
                    //});

                    lotDtos.Add(new
                    {
                        OrderLineId = line.order_line_id,
                        ProductId = line.product_id,
                        ProductName = product?.product_name ?? line.product_name,

                        LotNo = lot.lot_no,

                        BranchId = lot.branch_id,
                        WarehouseName =
        !string.IsNullOrWhiteSpace(lot.branch_id) &&
        branchDict.ContainsKey(lot.branch_id)
            ? branchDict[lot.branch_id]
            : lot.branch_id,

                        ManufacturingDate = lot.manufacturing_date,
                        ExpirationDate = lot.expiration_date,

                        OnHandQty = lot.quantity,
                        ReservedQty = reservedQty,
                        AvailableQty = available,

                        ExistingAllocatedQty = currentAllocated,

                        Uom = product?.uom ?? "",
                        PackQty = product?.pack_qty ?? 0,
                        PackUom = product?.pack_uom ?? ""
                    });
                }

                result.Add(new
                {
                    OrderLineId = line.order_line_id,
                    ProductName = product?.product_name ?? line.product_name,
                    RequiredQty = line.required_qty,
                    AllocatedQty = line.allocated_qty,
                    RemainingQty = line.remaining_qty,
                    Uom = product?.uom ?? "",
                    PackQty = product?.pack_qty ?? 0,
                    PackUom = product?.pack_uom ?? "",
                    Lots = lotDtos
                });
            }

            return result;
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

        public async Task<object> AllocateAsync(long orderId, AllocateDailyOrderRequest request)
        {


            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            if (request == null || request.Lines == null || !request.Lines.Any())
                throw new Exception("No allocation lines received.");

            if (string.IsNullOrWhiteSpace(order.source_branch_id))
                throw new Exception("Order source branch is missing.");
            var orderStatus = (order.status ?? "").Trim().ToUpper();

            if (orderStatus == "READY FOR DISPATCH")
            {
                throw new Exception(
                    "Cannot re-run FEFO. Order is already Ready for Dispatch. Click Back to Allocation first."
                );
            }

            if (orderStatus == "COMPLETED")
            {
                throw new Exception(
                    "Completed order can no longer be allocated."
                );
            }

            var phToday = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")
            ).Date;

            foreach (var line in order.Lines)
            {
                var requestLine = request.Lines
                    .FirstOrDefault(x => x.OrderLineId == line.order_line_id);

                if (requestLine == null)
                    continue;

                // LOCK ACTIVE CHECKLIST
                if (await HasActiveChecklistLineAsync(line.order_line_id))
                {
                    throw new Exception(
                        $"Cannot re-run FEFO for '{line.product_name}'. This line already belongs to an active delivery checklist."
                    );
                }

                var maxAllocatable = Math.Max(
                    0,
                    line.required_qty - line.dispatched_qty
                );

                if (requestLine.AllocateQty <= 0)
                    throw new Exception($"Allocate qty must be greater than zero for {line.product_name}.");

                if (requestLine.AllocateQty > maxAllocatable)
                    throw new Exception($"Allocate qty cannot exceed remaining qty for {line.product_name}.");

                // ✅ DELETE OLD ALLOCATION FIRST
                var existingAllocations = await _context.DailyOrderAllocations
                    .Where(a => a.order_line_id == line.order_line_id)
                    .ToListAsync();

                if (existingAllocations.Any())
                    _context.DailyOrderAllocations.RemoveRange(existingAllocations);

                // ✅ RESET LINE BEFORE REALLOCATING
                line.allocated_qty = 0;
                line.remaining_qty = Math.Max(0, line.required_qty - line.dispatched_qty);
                line.allocation_status = "Not Allocated";
                line.updated_at = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                decimal required = requestLine.AllocateQty;
                decimal allocatedTotal = 0;

                Console.WriteLine("==== SERVICE FEFO ====");
                Console.WriteLine($"LineId: {line.order_line_id}");
                Console.WriteLine($"Product: {line.product_name}");
                Console.WriteLine($"Requested Qty: {required}");

                var lots = await _context.ProductLotNumbers
                    .Where(x =>
                        //x.product_id == line.product_id &&
                        //x.branch_id == order.source_branch_id &&
                        //x.quantity > 0 &&
                        //!x.is_deleted &&
                        x.product_id == line.product_id &&
                        x.quantity > 0 &&
                        !x.is_deleted &&
                        (
                            x.expiration_date == null ||
                            x.expiration_date.Value.Date >= phToday
                        )
                    )
                    //.OrderBy(x => x.expiration_date)
                    //.ThenBy(x => x.manufacturing_date)
                    //.ThenBy(x => x.lot_no)
                    //.ToListAsync();
                    .OrderBy(x => x.branch_id == order.source_branch_id ? 0 : 1)
.ThenBy(x => x.expiration_date)
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
                        .Join(_context.DailyOrderLines,
                            a => a.order_line_id,
                            dl => dl.order_line_id,
                            (a, dl) => new { a, dl })
                        .Join(_context.DailyOrderHeaders,
                            x => x.dl.order_id,
                            h => h.order_id,
                            (x, h) => new { x.a, x.dl, h })
                        .Where(x => !x.h.is_deleted)
                        .SumAsync(x => (decimal?)x.a.allocated_qty) ?? 0;

                    var available = lot.quantity - alreadyAllocated;

                    if (available <= 0)
                        continue;

                    var allocateQty = Math.Min(available, required - allocatedTotal);

                    if (allocateQty <= 0)
                        continue;

                    _context.DailyOrderAllocations.Add(new DailyOrderAllocation
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
                        allocation_mode = "FEFO",
                        priority_rank = priority++,
                        created_at = DateTime.UtcNow
                    });

                    allocatedTotal += allocateQty;
                }

                // ✅ FINAL VALUE MUST COME ONLY FROM NEW FEFO RUN
                line.allocated_qty = allocatedTotal;

                // remaining is unallocated qty
                line.remaining_qty = Math.Max(
                    0,
                    line.required_qty - line.dispatched_qty - line.allocated_qty
                );

                if (line.allocated_qty == 0)
                    line.allocation_status = "NO STOCK";
                else if (line.remaining_qty > 0)
                    line.allocation_status = "PARTIAL";
                else
                    line.allocation_status = "FULLY ALLOCATED";

                line.updated_at = DateTime.UtcNow;

                Console.WriteLine($"Final Allocated: {line.allocated_qty}");
                Console.WriteLine($"Final Remaining: {line.remaining_qty}");
            }

            await _context.SaveChangesAsync();

            bool allDelivered = order.Lines.All(x => x.dispatched_qty >= x.required_qty);
            bool allAllocatedForRemaining = order.Lines.All(x => x.remaining_qty <= 0);
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

            if (string.IsNullOrWhiteSpace(request.SourceBranchId))
                throw new Exception("Source branch is required.");

            order.source_branch_id = request.SourceBranchId;

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


        public async Task<object> UpdateLineRequiredQtyAsync(
    long orderId,
    long orderLineId,
    UpdateDailyOrderLineQtyRequest request)
        {
            if (request == null)
                throw new Exception("Invalid request.");

            if (request.RequiredQty <= 0)
                throw new Exception("Required qty must be greater than zero.");

            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            var line = order.Lines.FirstOrDefault(x => x.order_line_id == orderLineId);

            if (line == null)
                throw new Exception("Order line not found.");

            if (await HasActiveChecklistLineAsync(line.order_line_id))
            {
                throw new Exception(
                    $"Cannot edit required qty for '{line.product_name}'. This line already belongs to an active delivery checklist."
                );
            }

            var orderStatus = (order.status ?? "").Trim().ToUpper();

            if (orderStatus == "COMPLETED")
                throw new Exception("Completed order can no longer be edited.");

            //if (orderStatus == "READY FOR DISPATCH")
            //    throw new Exception("Ready for Dispatch order can no longer be edited. Reopen/remove dispatch process first.");

            if (line.dispatched_qty > 0 && request.RequiredQty < line.dispatched_qty)
                throw new Exception($"Required qty cannot be less than dispatched qty ({line.dispatched_qty}).");

            var allocations = await _context.DailyOrderAllocations
                .Where(a => a.order_line_id == orderLineId)
                .ToListAsync();

            if (allocations.Any())
                _context.DailyOrderAllocations.RemoveRange(allocations);

            line.required_qty = request.RequiredQty;
            line.allocated_qty = 0;
            line.remaining_qty = Math.Max(0, line.required_qty - line.dispatched_qty);
            line.allocation_status = line.remaining_qty <= 0 ? "Completed" : "Not Allocated";
            line.updated_at = DateTime.UtcNow;

            await RecomputeDailyOrderHeaderStatusAsync(order);

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Required qty updated. Existing allocations were cleared.",
                OrderId = order.order_id,
                OrderLineId = line.order_line_id,
                RequiredQty = line.required_qty,
                AllocatedQty = line.allocated_qty,
                RemainingQty = line.remaining_qty,
                AllocationStatus = line.allocation_status,
                OrderStatus = order.status
            };
        }


        //private async Task<bool> HasActiveChecklistLineAsync(long orderLineId)
        //{
        //    return await _context.Database
        //        .SqlQueryRaw<int>(@"
        //    SELECT 1
        //    FROM delivery_checklist_line dcl
        //    INNER JOIN delivery_checklist_header dch
        //        ON dcl.checklist_id = dch.checklist_id
        //    WHERE dcl.order_line_id = {0}
        //      AND IFNULL(dcl.is_deleted, 0) = 0
        //      AND IFNULL(dch.is_deleted, 0) = 0
        //      AND UPPER(TRIM(dch.status)) IN ('READY', 'LOADING', 'PARTIAL')
        //    LIMIT 1;
        //", orderLineId)
        //        .AnyAsync();
        //}

        private async Task<bool> HasActiveChecklistLineAsync(long orderLineId)
        {
            await using var conn = new MySqlConnection(_connectionString);

            await conn.OpenAsync();

            string sql = @"
        SELECT COUNT(*)
        FROM delivery_checklist_line dcl
        INNER JOIN delivery_checklist_header dch
            ON dcl.checklist_id = dch.checklist_id
        WHERE dcl.order_line_id = @orderLineId
          AND IFNULL(dcl.is_deleted, 0) = 0
          AND IFNULL(dch.is_deleted, 0) = 0
          AND UPPER(TRIM(dch.status)) IN ('READY', 'LOADING', 'PARTIAL');";

            await using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@orderLineId", orderLineId);

            var result = await cmd.ExecuteScalarAsync();

            return Convert.ToInt32(result) > 0;
        }
        public async Task<object> ClearLineAllocationAsync(long orderId, long orderLineId)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            var line = order.Lines.FirstOrDefault(x => x.order_line_id == orderLineId);

            if (line == null)
                throw new Exception("Order line not found.");

            if (await HasActiveChecklistLineAsync(line.order_line_id))
            {
                throw new Exception(
                    $"Cannot clear allocation for '{line.product_name}'. This line already belongs to an active delivery checklist."
                );
            }

            var orderStatus = (order.status ?? "").Trim().ToUpper();

            if (orderStatus == "COMPLETED")
                throw new Exception("Completed order can no longer be edited.");

            //if (orderStatus == "READY FOR DISPATCH")
            //    throw new Exception("Ready for Dispatch order can no longer be edited.");

            var allocations = await _context.DailyOrderAllocations
                .Where(a => a.order_line_id == orderLineId)
                .ToListAsync();

            if (allocations.Any())
                _context.DailyOrderAllocations.RemoveRange(allocations);

            line.allocated_qty = 0;
            line.remaining_qty = Math.Max(0, line.required_qty - line.dispatched_qty);
            line.allocation_status = line.remaining_qty <= 0 ? "Completed" : "Not Allocated";
            line.updated_at = DateTime.UtcNow;

            await RecomputeDailyOrderHeaderStatusAsync(order);

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Allocation cleared successfully.",
                OrderId = order.order_id,
                OrderLineId = line.order_line_id,
                RequiredQty = line.required_qty,
                AllocatedQty = line.allocated_qty,
                RemainingQty = line.remaining_qty,
                AllocationStatus = line.allocation_status,
                OrderStatus = order.status
            };
        }

        public async Task<object> DeleteOrderLineAsync(long orderId, long orderLineId)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            var line = order.Lines.FirstOrDefault(x => x.order_line_id == orderLineId);

            if (line == null)
                throw new Exception("Order line not found.");

            if (await HasActiveChecklistLineAsync(line.order_line_id))
                throw new Exception("Cannot delete this line because it belongs to an active delivery checklist.");

            if (line.allocated_qty > 0)
                throw new Exception("Cannot delete this line because it is already allocated. Clear allocation first.");

            if (line.dispatched_qty > 0)
                throw new Exception("Cannot delete this line because it is already dispatched.");

            // Delete any allocation records for this line
            var allocations = await _context.DailyOrderAllocations
                .Where(x => x.order_line_id == orderLineId)
                .ToListAsync();

            if (allocations.Any())
                _context.DailyOrderAllocations.RemoveRange(allocations);

            _context.DailyOrderLines.Remove(line);

            // If this was the last line, soft delete the header
            if (order.Lines.Count == 1)
            {
                order.is_deleted = true;
                order.deleted_at = DateTime.UtcNow;
                order.updated_at = DateTime.UtcNow;
            }
            else
            {
                await RecomputeDailyOrderHeaderStatusAsync(order);
            }

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Order line deleted successfully."
            };
        }

        public async Task<object> AddOrderLineAsync(
    long orderId,
    AddDailyOrderLineRequest request)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            var status = (order.status ?? "").Trim().ToUpper();

            if (status == "READY FOR DISPATCH" || status == "COMPLETED")
                throw new Exception("Cannot add product to this order status.");

            if (string.IsNullOrWhiteSpace(request.ProductId))
                throw new Exception("Product is required.");

            if (request.RequiredQty <= 0)
                throw new Exception("Required qty must be greater than zero.");

            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.product_id == request.ProductId);

            if (product == null)
                throw new Exception("Product not found.");

            var duplicate = order.Lines.Any(x =>
                x.product_id == request.ProductId &&
                x.dispatched_qty <= 0);

            if (duplicate)
                throw new Exception("This product already exists in this order.");

            var line = new DailyOrderLine
            {
                order_id = order.order_id,
                product_id = product.product_id,
                product_name = product.product_name,
                required_qty = request.RequiredQty,
                allocated_qty = 0,
                remaining_qty = request.RequiredQty,
                dispatched_qty = 0,
                allocation_status = "Not Allocated",
                status = "PENDING",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.DailyOrderLines.Add(line);

            await RecomputeDailyOrderHeaderStatusAsync(order);
            await _context.SaveChangesAsync();

            return new
            {
                Message = "Product line added successfully.",
                OrderId = order.order_id
            };
        }

        private Task RecomputeDailyOrderHeaderStatusAsync(DailyOrderHeader order)
        {
            foreach (var line in order.Lines)
            {
                var stillNeedsAllocation = Math.Max(
                    0,
                    line.required_qty - line.dispatched_qty - line.allocated_qty
                );

                line.remaining_qty = stillNeedsAllocation;

                if (line.dispatched_qty >= line.required_qty)
                {
                    line.allocation_status = "Completed";
                }
                else if (line.allocated_qty <= 0)
                {
                    line.allocation_status = "Not Allocated";
                }
                else if (stillNeedsAllocation > 0)
                {
                    line.allocation_status = "Partial";
                }
                else
                {
                    line.allocation_status = "Fully Allocated";
                }

                line.updated_at = DateTime.UtcNow;
            }

            bool allCompleted = order.Lines.All(x => x.dispatched_qty >= x.required_qty);
            bool anyDispatched = order.Lines.Any(x => x.dispatched_qty > 0);
            bool anyAllocated = order.Lines.Any(x => x.allocated_qty > 0);
            bool allAllocated = order.Lines.All(x =>
                x.dispatched_qty >= x.required_qty ||
                (
                    x.allocated_qty > 0 &&
                    Math.Max(0, x.required_qty - x.dispatched_qty - x.allocated_qty) <= 0
                )
            );

            if (allCompleted)
                order.status = "COMPLETED";
            else if (anyDispatched)
                order.status = "PARTIALLY DELIVERED";
            else if (allAllocated)
                order.status = "Allocated";
            else if (anyAllocated)
                order.status = "Partially Allocated";
            else
                order.status = "For Allocation";

            order.updated_at = DateTime.UtcNow;

            return Task.CompletedTask;
        }

        // =========================================
        // DELETE DAILY ORDER
        // =========================================
        public async Task<object> DeleteAsync(long orderId)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            var status = (order.status ?? "").Trim().ToUpper();

            if (status == "READY FOR DISPATCH" || status == "COMPLETED")
                throw new Exception("Cannot delete order that is already dispatched or completed.");

            if (order.Lines.Any(x => x.dispatched_qty > 0))
                throw new Exception("Cannot delete order with dispatched quantity.");

            var lineIds = order.Lines.Select(x => x.order_line_id).ToList();

            var allocations = await _context.DailyOrderAllocations
                .Where(a => lineIds.Contains(a.order_line_id))
                .ToListAsync();

            if (allocations.Any())
                _context.DailyOrderAllocations.RemoveRange(allocations);

            order.is_deleted = true;
            order.deleted_at = DateTime.UtcNow;
            order.updated_at = DateTime.UtcNow;

            foreach (var line in order.Lines)
            {
                line.updated_at = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Order deleted successfully."
            };
        }

        public async Task<object> ManualAllocateAsync(
    long orderId,
    ManualAllocateRequest request)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            if (string.IsNullOrWhiteSpace(order.source_branch_id))
                throw new Exception("Order source branch is missing.");

            var orderStatus = (order.status ?? "").Trim().ToUpper();

            if (orderStatus == "READY FOR DISPATCH")
            {
                throw new Exception(
                    "Cannot manually allocate. Order is already Ready for Dispatch. Click Back to Allocation first."
                );
            }

            if (orderStatus == "COMPLETED")
            {
                throw new Exception(
                    "Completed order can no longer be allocated."
                );
            }

            foreach (var requestLine in request.Lines)
            {
                var line = order.Lines
                    .FirstOrDefault(x => x.order_line_id == requestLine.OrderLineId);

                if (line == null)
                    continue;

                if (await HasActiveChecklistLineAsync(line.order_line_id))
                {
                    throw new Exception(
                        $"Cannot manually allocate '{line.product_name}'. This line already belongs to an active delivery checklist."
                    );
                }

                // REMOVE OLD ALLOCATIONS
                var oldAllocations = await _context.DailyOrderAllocations
                    .Where(x => x.order_line_id == line.order_line_id)
                    .ToListAsync();

                if (oldAllocations.Any())
                    _context.DailyOrderAllocations.RemoveRange(oldAllocations);

                line.allocated_qty = 0;
                line.remaining_qty = Math.Max(
                    0,
                    line.required_qty - line.dispatched_qty
                );

                await _context.SaveChangesAsync();

                decimal totalAllocated = 0;
                int priority = 1;

                foreach (var lotRequest in requestLine.Lots)
                {
                    if (lotRequest.AllocateQty <= 0)
                        continue;

                    var lot = await _context.ProductLotNumbers
    .FirstOrDefaultAsync(x =>
        x.product_id == line.product_id &&
        x.branch_id == lotRequest.BranchId &&
        x.lot_no == lotRequest.LotNo &&
        !x.is_deleted);

                    if (lot == null)
                        throw new Exception($"Lot {lotRequest.LotNo} not found.");

                    decimal alreadyReserved = await _context.DailyOrderAllocations
                        .Where(a =>
    a.product_id == line.product_id &&
    a.branch_id == lotRequest.BranchId &&
    a.lot_no == lot.lot_no &&
    a.order_line_id != line.order_line_id &&
    a.allocated_qty > 0
)
                        .Join(_context.DailyOrderLines,
                            a => a.order_line_id,
                            dl => dl.order_line_id,
                            (a, dl) => new { a, dl })
                        .Join(_context.DailyOrderHeaders,
                            x => x.dl.order_id,
                            h => h.order_id,
                            (x, h) => new { x.a, x.dl, h })
                        .Where(x => !x.h.is_deleted)
                        .SumAsync(x => (decimal?)x.a.allocated_qty) ?? 0;

                    var available = lot.quantity - alreadyReserved;

                    if (lotRequest.AllocateQty > available)
                    {
                        throw new Exception(
                            $"Lot {lot.lot_no} only has {available} available."
                        );
                    }

                    _context.DailyOrderAllocations.Add(new DailyOrderAllocation
                    {
                        order_line_id = line.order_line_id,
                        product_id = line.product_id,
                        branch_id = lot.branch_id,
                        lot_no = lot.lot_no,

                        manufacturing_date = lot.manufacturing_date,
                        expiration_date = lot.expiration_date,

                        on_hand_qty = lot.quantity,
                        reserved_qty = alreadyReserved,
                        available_qty = available,

                        allocated_qty = lotRequest.AllocateQty,

                        allocation_mode = "MANUAL",

                        priority_rank = priority++,
                        created_at = DateTime.UtcNow
                    });

                    totalAllocated += lotRequest.AllocateQty;
                }

                var maxAllocatable = Math.Max(
                    0,
                    line.required_qty - line.dispatched_qty
                );

                if (totalAllocated > maxAllocatable)
                {
                    throw new Exception(
                        $"Manual allocation exceeds remaining qty for {line.product_name}."
                    );
                }

                line.allocated_qty = totalAllocated;

                line.remaining_qty = Math.Max(
                    0,
                    line.required_qty -
                    line.dispatched_qty -
                    line.allocated_qty
                );

                if (line.allocated_qty == 0)
                    line.allocation_status = "NO STOCK";
                else if (line.remaining_qty > 0)
                    line.allocation_status = "PARTIAL";
                else
                    line.allocation_status = "FULLY ALLOCATED";

                line.updated_at = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            bool allDelivered = order.Lines.All(x =>
                x.dispatched_qty >= x.required_qty);

            bool allAllocated = order.Lines.All(x =>
                x.remaining_qty <= 0);

            bool anyAllocated = order.Lines.Any(x =>
                x.allocated_qty > 0);

            bool anyDelivered = order.Lines.Any(x =>
                x.dispatched_qty > 0);

            order.status = allDelivered
                ? "COMPLETED"
                : anyDelivered
                    ? "PARTIALLY DELIVERED"
                    : allAllocated
                        ? "Allocated"
                        : anyAllocated
                            ? "Partially Allocated"
                            : "For Allocation";

            order.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Manual allocation completed."
            };
        }


        private async Task<bool> HasActiveChecklistAsync(long orderId)
        {
            await using var conn = new MySqlConnection(_connectionString);

            await conn.OpenAsync();

            string sql = @"
        SELECT COUNT(*)
        FROM delivery_checklist_line dcl
        INNER JOIN delivery_checklist_header dch
            ON dcl.checklist_id = dch.checklist_id
        WHERE dcl.order_id = @orderId
          AND IFNULL(dcl.is_deleted, 0) = 0
          AND IFNULL(dch.is_deleted, 0) = 0
          AND UPPER(TRIM(dch.status)) IN ('READY', 'LOADING', 'PARTIAL');";

            await using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@orderId", orderId);

            var result = await cmd.ExecuteScalarAsync();

            return Convert.ToInt32(result) > 0;
        }

        //private async Task<bool> HasActiveChecklistLineAsync(long orderLineId)
        //{
        //    return await _context.Database
        //        .SqlQueryRaw<int>(@"
        //    SELECT 1
        //    FROM delivery_checklist_line dcl
        //    INNER JOIN delivery_checklist_header dch
        //        ON dcl.checklist_id = dch.checklist_id
        //    WHERE dcl.order_line_id = {0}
        //      AND IFNULL(dcl.is_deleted, 0) = 0
        //      AND IFNULL(dch.is_deleted, 0) = 0
        //      AND UPPER(TRIM(dch.status)) IN ('READY', 'LOADING', 'PARTIAL')
        //    LIMIT 1;
        //", orderLineId)
        //        .AnyAsync();
        //}
        public async Task<object> BackToAllocationAsync(long orderId)
        {
            var order = await _context.DailyOrderHeaders
                .Include(h => h.Lines)
                .FirstOrDefaultAsync(x => x.order_id == orderId && !x.is_deleted);

            if (order == null)
                throw new Exception("Order not found.");

            var status = (order.status ?? "").Trim().ToUpper();

            if (status != "READY FOR DISPATCH")
                throw new Exception("Only Ready for Dispatch orders can be moved back to allocation.");

            if (await HasActiveChecklistAsync(orderId))
                throw new Exception("Cannot move back to allocation because this order already has an active delivery checklist. Reopen/delete checklist first.");

            if (order.Lines.Any(x => x.dispatched_qty > 0))
                throw new Exception("Cannot move back to allocation because this order already has dispatched quantity.");

            bool anyAllocated = order.Lines.Any(x => x.allocated_qty > 0);

            bool allAllocated = order.Lines.All(x =>
                x.allocated_qty > 0 &&
                Math.Max(0, x.required_qty - x.dispatched_qty - x.allocated_qty) <= 0
            );

            if (allAllocated)
                order.status = "Allocated";
            else if (anyAllocated)
                order.status = "Partially Allocated";
            else
                order.status = "For Allocation";

            order.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new
            {
                Message = "Order moved back to allocation.",
                OrderId = order.order_id,
                Status = order.status
            };
        }




    }
}