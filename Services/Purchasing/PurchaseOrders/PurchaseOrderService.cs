using inventory_api.Data;
using inventory_api.DTOs.Purchasing.PurchaseOrders;
using inventory_api.Models.Purchasing.PurchaseOrders;

using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.PurchaseOrders
{
    public class PurchaseOrderService
    {
        private readonly AppDbContext _context;

        public PurchaseOrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GeneratePoNoAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"PO-{year}-";

            var lastPoNo = await _context.PurchaseOrderHeaders
                .Where(x => x.PoNo.StartsWith(prefix))
                .OrderByDescending(x => x.PoId)
                .Select(x => x.PoNo)
                .FirstOrDefaultAsync();

            var nextNo = 1;

            if (!string.IsNullOrWhiteSpace(lastPoNo))
            {
                var numberPart = lastPoNo.Replace(prefix, "");

                if (int.TryParse(numberPart, out var lastNumber))
                {
                    nextNo = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNo:0000}";
        }

        public async Task<int> CreateAsync(CreatePurchaseOrderDto dto, string userId)
        {


            await using var transaction =
    await _context.Database.BeginTransactionAsync();

            try
            {


                var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == dto.CanvassId);

            if (canvass == null)
                throw new Exception("Canvassing record not found.");

            //if (canvass.Status != "COMPLETED")
            //    throw new Exception("Only completed canvassing can create Purchase Order.");

            if (canvass.Status != "OPEN" && canvass.Status != "COMPLETED")
                throw new Exception("Only active canvassing can create Purchase Order.");

            if (dto.SupplierId <= 0)
                throw new Exception("Supplier is required.");

            if (dto.Lines == null || !dto.Lines.Any())
                throw new Exception("Purchase Order must have at least one material.");


                var hasPlannedSchedules =
        dto.Schedules != null &&
        dto.Schedules.Any();

                if (!hasPlannedSchedules && !dto.DeliveryDate.HasValue)
                {
                    throw new Exception(
                        "A delivery date or at least one planned delivery schedule is required."
                    );
                }

                if (hasPlannedSchedules)
                {
                    foreach (var schedule in dto.Schedules)
                    {
                        if (schedule.ScheduledDate == default)
                        {
                            throw new Exception(
                                "Every planned delivery schedule must have a delivery date."
                            );
                        }

                        if (schedule.Lines == null ||
                            !schedule.Lines.Any(x => x.ScheduledQty > 0))
                        {
                            throw new Exception(
                                "Every planned delivery schedule must contain at least one quantity."
                            );
                        }

                        var duplicateScheduleLines = schedule.Lines
                            .GroupBy(x => x.CanvassLineId)
                            .Where(x => x.Count() > 1)
                            .Select(x => x.Key)
                            .ToList();

                        if (duplicateScheduleLines.Any())
                        {
                            throw new Exception(
                                "A planned schedule contains duplicate material lines."
                            );
                        }
                    }

                    var scheduledTotals = dto.Schedules
                        .SelectMany(x => x.Lines)
                        .Where(x => x.ScheduledQty > 0)
                        .GroupBy(x => x.CanvassLineId)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Sum(y => y.ScheduledQty)
                        );

                    foreach (var poLineDto in dto.Lines)
                    {
                        scheduledTotals.TryGetValue(
                            poLineDto.CanvassLineId,
                            out var scheduledTotal
                        );

                        if (Math.Abs(scheduledTotal - poLineDto.PoQty) > 0.0001m)
                        {
                            throw new Exception(
                                $"The total scheduled quantity must equal the PO quantity " +
                                $"for canvass line {poLineDto.CanvassLineId}. " +
                                $"PO Qty: {poLineDto.PoQty:N4}; " +
                                $"Scheduled: {scheduledTotal:N4}."
                            );
                        }
                    }

                    var invalidCanvassLineIds = scheduledTotals.Keys
                        .Where(canvassLineId =>
                            dto.Lines.All(x =>
                                x.CanvassLineId != canvassLineId))
                        .ToList();

                    if (invalidCanvassLineIds.Any())
                    {
                        throw new Exception(
                            "One or more planned schedule lines do not belong to this Purchase Order."
                        );
                    }
                }


                var poNo = await GeneratePoNoAsync();



            var subtotal = dto.Lines.Sum(x => x.PoQty * x.PoUnitPrice);
            var totalAmount = subtotal + dto.OtherCharges;

            var header = new PurchaseOrderHeader
            {
                PoNo = poNo,
                CanvassId = dto.CanvassId,
                SupplierId = dto.SupplierId,
                PoDate = dto.PoDate,
                DeliveryDate = hasPlannedSchedules
    ? dto.Schedules.Min(x => x.ScheduledDate).Date
    : dto.DeliveryDate?.Date,
                PaymentTerms = dto.PaymentTerms,
                Remarks = dto.Remarks,
                Status = "DRAFT",
                SupplierAddress = dto.SupplierAddress,
                RequestedBy = dto.RequestedBy,
                Subtotal = subtotal,
                OtherCharges = dto.OtherCharges,
                TotalAmount = totalAmount,
                PrintedPoNo = dto.PrintedPoNo,
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            var requestedCanvassLineIds = dto.Lines
    .Select(x => x.CanvassLineId)
    .Distinct()
    .ToList();

            var duplicateLines = await _context.PurchaseOrderLines
                .Where(pol => requestedCanvassLineIds.Contains(pol.CanvassLineId))
                .Where(pol => pol.Header != null && pol.Header.Status != "CANCELLED")
                .Select(pol => pol.CanvassLineId)
                .Distinct()
                .ToListAsync();

            if (duplicateLines.Any())
                throw new Exception("One or more canvass lines already belong to an active Purchase Order.");


            foreach (var line in dto.Lines)
            {
                var validQuote = await _context.PurchasingCanvassQuotes
                    .AnyAsync(q =>
                        q.QuoteId == line.QuoteId &&
                        q.CanvassLineId == line.CanvassLineId &&
                        q.SupplierId == dto.SupplierId &&
                        q.IsRecommended);

                if (!validQuote)
                    throw new Exception("Selected material does not match the selected recommended supplier.");
            }

            var now = DateTime.Now;

            foreach (var line in dto.Lines)
            {
                if (line.PoQty <= 0)
                    throw new Exception("PO quantity must be greater than zero.");

                if (line.PoUnitPrice <= 0)
                    throw new Exception("PO unit price must be greater than zero.");

                var lineTotal = line.PoQty * line.PoUnitPrice;

                header.Lines.Add(new PurchaseOrderLine
                {
                    CanvassLineId = line.CanvassLineId,
                    QuoteId = line.QuoteId,
                    MaterialId = line.MaterialId,

                    PoQty = line.PoQty,
                    Uom = line.Uom,

                    QuotationUnitPrice = line.QuotationUnitPrice,
                    PoUnitPrice = line.PoUnitPrice,
                    LineTotal = lineTotal,

                    Remarks = line.Remarks,

                    ReceivedQty = 0,
                    BalanceQty = line.PoQty,

                    Status = "OPEN",
                    CreatedAt = now
                });
            }

            _context.PurchaseOrderHeaders.Add(header);

            /*
             * Save the PO first so that PoId and PoLineId values
             * are generated before creating schedule records.
             */
            await _context.SaveChangesAsync();

                var schedulesToCreate =
                    new List<PurchaseOrderDeliverySchedule>();

                if (hasPlannedSchedules)
                {
                    var scheduleNo = 1;

                    foreach (var scheduleDto in dto.Schedules
                                 .OrderBy(x => x.ScheduledDate))
                    {
                        var schedule =
                            new PurchaseOrderDeliverySchedule
                            {
                                PoId = header.PoId,
                                ScheduleNo = scheduleNo,
                                ScheduledDate = scheduleDto.ScheduledDate.Date,
                                Status = "OPEN",

                                Remarks =
                                    string.IsNullOrWhiteSpace(scheduleDto.Remarks)
                                        ? $"Planned delivery schedule #{scheduleNo}."
                                        : scheduleDto.Remarks,

                                CreatedBy = userId,
                                CreatedAt = now
                            };

                        foreach (var scheduleLineDto in scheduleDto.Lines
                                     .Where(x => x.ScheduledQty > 0))
                        {
                            var poLine = header.Lines
                                .FirstOrDefault(x =>
                                    x.CanvassLineId ==
                                    scheduleLineDto.CanvassLineId);

                            if (poLine == null)
                            {
                                throw new Exception(
                                    "A planned delivery line could not be matched to the Purchase Order."
                                );
                            }

                            schedule.Lines.Add(
                                new PurchaseOrderDeliveryScheduleLine
                                {
                                    PoLineId = poLine.PoLineId,

                                    ScheduledQty =
                                        scheduleLineDto.ScheduledQty,

                                    ReceivedQty = 0,

                                    BalanceQty =
                                        scheduleLineDto.ScheduledQty,

                                    Status = "OPEN",

                                    CreatedAt = now
                                }
                            );
                        }

                        schedulesToCreate.Add(schedule);
                        scheduleNo++;
                    }
                }
                else
                {
                    var initialSchedule =
                        new PurchaseOrderDeliverySchedule
                        {
                            PoId = header.PoId,
                            ScheduleNo = 1,
                            ScheduledDate = dto.DeliveryDate!.Value.Date,
                            Status = "OPEN",
                            Remarks = "Initial delivery schedule.",
                            CreatedBy = userId,
                            CreatedAt = now
                        };

                    foreach (var poLine in header.Lines)
                    {
                        initialSchedule.Lines.Add(
                            new PurchaseOrderDeliveryScheduleLine
                            {
                                PoLineId = poLine.PoLineId,

                                ScheduledQty = poLine.PoQty,
                                ReceivedQty = 0,
                                BalanceQty = poLine.PoQty,

                                Status = "OPEN",
                                CreatedAt = now
                            }
                        );
                    }

                    schedulesToCreate.Add(initialSchedule);
                }

                _context.PurchaseOrderDeliverySchedules
                    .AddRange(schedulesToCreate);

                await _context.SaveChangesAsync();
                await UpdateMprfPoStatusAsync(dto.CanvassId);

                await transaction.CommitAsync();

                return header.PoId;

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        }


        public async Task<object?> GetCreateOptionsAsync(int canvassId)
        {
            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == canvassId);

            if (canvass == null)
                return null;
            var requestingDepartment = await _context.PurchasingMprfHeaders
    .Where(x => x.mprf_id == canvass.MprfId)
    .Select(x => x.category)
    .FirstOrDefaultAsync();

            var activeCanvassLineIds = await _context.PurchaseOrderLines
                .Where(pol => pol.Header != null && pol.Header.Status != "CANCELLED")
                .Select(pol => pol.CanvassLineId)
                .Distinct()
                .ToListAsync();



            var remaining = await (
                from cl in _context.PurchasingCanvassLines
                join q in _context.PurchasingCanvassQuotes
                    on cl.CanvassLineId equals q.CanvassLineId
                join s in _context.Suppliers
                    on q.SupplierId equals s.SupplierId
                join m in _context.Materials
                    on cl.MaterialId equals m.material_id
                where cl.CanvassId == canvassId
                      && q.IsRecommended
                      && !activeCanvassLineIds.Contains(cl.CanvassLineId)
                select new
                {
                    cl.CanvassLineId,
                    q.QuoteId,
                    q.SupplierId,
                    s.SupplierName,
                    SupplierAddress = s.Address,
                    q.PaymentTerms,
                    cl.MaterialId,
                    m.material_code,
                    m.material_name,
                    Qty = cl.PurchasingQty,
                    Uom = cl.Uom ?? m.uom,
                    UnitPrice = q.UnitPrice
                }
            ).ToListAsync();

            var existingPoSuppliers = await (
    from po in _context.PurchaseOrderHeaders
    where po.CanvassId == canvassId
          && po.Status != "CANCELLED"
    select new
    {
        po.PoId,
        po.PoNo,
        po.SupplierId,
        po.Status,
        po.TotalAmount
    }
).ToListAsync();

            return new
            {
                requesting_department = requestingDepartment ?? "",
                suppliers = remaining
                    .GroupBy(x => new { x.SupplierId, x.SupplierName, x.SupplierAddress, x.PaymentTerms })
                    .Select(g => new
                    {
                        g.Key.SupplierId,
                        g.Key.SupplierName,
                        g.Key.SupplierAddress,
                        g.Key.PaymentTerms,
                        LineCount = g.Count(),
                        TotalAmount = g.Sum(x => x.Qty * x.UnitPrice)
                    })
                    .ToList(),

                lines = remaining,
                existing_po_suppliers = existingPoSuppliers
            };




        }
        public async Task<List<PurchaseOrderListDto>> GetAllAsync()
        {
            var data = await _context.PurchaseOrderHeaders
                .OrderByDescending(x => x.PoId)
                .Select(x => new PurchaseOrderListDto
                {
                    PoId = x.PoId,
                    PoNo = x.PoNo,
                    PoDate = x.PoDate,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    TotalAmount = x.TotalAmount,
                    Status = x.Status,

                    CreatedBy = x.CreatedBy,

                    CreatedByName = _context.Users
    .Where(u => u.user_id == x.CreatedBy)
    .Select(u => u.full_name)
    .FirstOrDefault() ?? x.CreatedBy,

                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return data;
        }

        public async Task<PurchaseOrderDetailsDto?> GetByIdAsync(int poId)
        {
            var data = await _context.PurchaseOrderHeaders
                .Where(x => x.PoId == poId)
                .Select(x => new PurchaseOrderDetailsDto
                {
                    PoId = x.PoId,
                    PoNo = x.PoNo,
                    CanvassId = x.CanvassId,
                    SupplierId = x.SupplierId,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    SupplierAddress = x.SupplierAddress,
                    RequestedBy = x.RequestedBy,

                    Subtotal = x.Subtotal,
                    OtherCharges = x.OtherCharges,
                    TotalAmount = x.TotalAmount,

                    CheckedBy = x.CheckedBy,
                    CheckedAt = x.CheckedAt,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedAt = x.ApprovedAt,

                    PoDate = x.PoDate,
                    DeliveryDate = x.DeliveryDate,
                    PaymentTerms = x.PaymentTerms,
                    Remarks = x.Remarks,
                    Status = x.Status,
                    PrintedPoNo = x.PrintedPoNo,

                    Lines = x.Lines.Select(l => new PurchaseOrderLineDto
                    {
                        PoLineId = l.PoLineId,
                        MaterialId = l.MaterialId,

                        MaterialCode = _context.Materials
                            .Where(m => m.material_id == l.MaterialId)
                            .Select(m => m.material_code)
                            .FirstOrDefault() ?? "",

                        MaterialName = _context.Materials
                            .Where(m => m.material_id == l.MaterialId)
                            .Select(m => m.material_name)
                            .FirstOrDefault() ?? "",

                        PoQty = l.PoQty,
                        Uom = l.Uom,
                        QuotationUnitPrice = l.QuotationUnitPrice,
                        PoUnitPrice = l.PoUnitPrice,
                        LineTotal = l.LineTotal,

                        ReceivedQty = l.ReceivedQty,
                        BalanceQty = l.BalanceQty,

                        Status = l.Status,
                        Remarks = l.Remarks
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return data;
        }

        public async Task SubmitForApprovalAsync(int poId)
        {
            var po = await _context.PurchaseOrderHeaders
                .FirstOrDefaultAsync(x => x.PoId == poId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            if (po.Status != "DRAFT")
                throw new Exception("Only draft PO can be submitted for approval.");

            po.Status = "FOR_APPROVAL";
            po.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task ApproveAsync(int poId, string userId)
        {
            var po = await _context.PurchaseOrderHeaders
                .FirstOrDefaultAsync(x => x.PoId == poId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            if (po.Status != "FOR_APPROVAL")
                throw new Exception("Only PO for approval can be approved.");

            po.Status = "APPROVED";
            po.ApprovedBy = userId;
            po.ApprovedAt = DateTime.Now;
            po.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task CancelAsync(int poId)
        {
            var po = await _context.PurchaseOrderHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.PoId == poId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            if (po.Lines.Any(x => x.ReceivedQty > 0))
            {
                throw new Exception(
                    "Cannot cancel PO because it already has received quantity."
                );
            }

            var schedules = await _context.PurchaseOrderDeliverySchedules
                .Include(x => x.Lines)
                .Where(x => x.PoId == poId)
                .ToListAsync();

            var now = DateTime.Now;

            po.Status = "CANCELLED";
            po.UpdatedAt = now;

            foreach (var line in po.Lines)
            {
                line.Status = "CANCELLED";
                line.UpdatedAt = now;
            }

            foreach (var schedule in schedules)
            {
                schedule.Status = "CANCELLED";
                schedule.UpdatedAt = now;

                foreach (var scheduleLine in schedule.Lines)
                {
                    scheduleLine.Status = "CANCELLED";
                    scheduleLine.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync();
            await UpdateMprfPoStatusAsync(po.CanvassId);
        }

        private async Task UpdateMprfPoStatusAsync(int canvassId)
        {
            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == canvassId);

            if (canvass == null) return;

       

            var mprf = await _context.PurchasingMprfHeaders
                .FirstOrDefaultAsync(x => x.mprf_id == canvass.MprfId);

            if (mprf == null) return;

            var totalRecommendedSuppliers = await (
                from cl in _context.PurchasingCanvassLines
                join q in _context.PurchasingCanvassQuotes
                    on cl.CanvassLineId equals q.CanvassLineId
                where cl.CanvassId == canvassId
                      && q.IsRecommended
                select q.SupplierId
            ).Distinct().CountAsync();

            var poStatuses = await _context.PurchaseOrderHeaders
    .Where(po => po.CanvassId == canvassId)
    .Select(po => po.Status)
    .ToListAsync();

            var hasAnyPo = poStatuses.Any();

            var allPosCancelled =
                hasAnyPo &&
                poStatuses.All(x => x == "CANCELLED");

            var totalActivePoSuppliers = await _context.PurchaseOrderHeaders
                .Where(po =>
                    po.CanvassId == canvassId &&
                    po.Status != "CANCELLED")
                .Select(po => po.SupplierId)
                .Distinct()
                .CountAsync();

            if (allPosCancelled)
            {
                mprf.status = "CANCELLED";
            }
            else if (
                totalRecommendedSuppliers > 0 &&
                totalActivePoSuppliers >= totalRecommendedSuppliers)
            {
                mprf.status = "PO_CREATED";
            }
            else
            {
                mprf.status = "CANVASSING";
            }

            mprf.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}