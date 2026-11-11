using inventory_api.Data;
using inventory_api.DTOs.Inventory.MaterialRequisitions;
using inventory_api.Models.Manufacturing.Materials;
using inventory_api.Models.Manufacturing.Materials.Requisitions;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Inventory
{
    public class MaterialRequisitionService
    {
        private readonly AppDbContext _context;

        public MaterialRequisitionService(
            AppDbContext context)
        {
            _context = context;
        }


        public async Task<int> CreateDraftAsync(
            CreateMaterialRequisitionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.BranchId))
                throw new InvalidOperationException("Branch is required.");

            if (string.IsNullOrWhiteSpace(dto.CreatedBy))
                throw new InvalidOperationException("Created by is required.");

            if (dto.Lines == null || dto.Lines.Count == 0)
                throw new InvalidOperationException(
                    "At least one material is required.");

            var now = DateTime.UtcNow;
            var today = now.Date;
            var branchId = dto.BranchId.Trim();

            var requisition = new MaterialRequisition
            {
                RequisitionNo = await GenerateRequisitionNoAsync(),
                BranchId = branchId,
                RequisitionDate = dto.RequisitionDate == default
                    ? now : dto.RequisitionDate,
                RequestedBy = dto.RequestedBy?.Trim(),
                TimeRequested = dto.TimeRequested,
                Status = "DRAFT",
                Remarks = dto.Remarks?.Trim(),
                CreatedBy = dto.CreatedBy.Trim(),
                CreatedAt = now
            };

            foreach (var lineDto in dto.Lines)
            {
                if (lineDto.MaterialId <= 0 ||
                    lineDto.RequestedQuantity <= 0)
                {
                    throw new InvalidOperationException(
                        "Invalid material or requested quantity.");
                }

                var material = await _context.Materials
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.material_id == lineDto.MaterialId);

                if (material == null)
                    throw new KeyNotFoundException(
                        $"Material {lineDto.MaterialId} was not found.");

                var method = material.is_lot_tracked
                    ? (lineDto.AllocationMethod ?? "AUTO_FEFO")
                        .Trim().ToUpperInvariant()
                    : "QUANTITY_ONLY";

                if (material.is_lot_tracked &&
                    method != "AUTO_FEFO" &&
                    method != "MANUAL")
                {
                    throw new InvalidOperationException(
                        $"Invalid allocation method for {material.material_name}.");
                }

                var line = new MaterialRequisitionLine
                {
                    MaterialId = material.material_id,
                    RequestedQuantity = lineDto.RequestedQuantity,
                    ActualQuantity = null,
                    Uom = material.uom,
                    AllocationMethod = method,
                    Remarks = lineDto.Remarks,
                    CreatedAt = now
                };

                if (method == "AUTO_FEFO" ||
                    method == "QUANTITY_ONLY")
                {
                    var preview = await PreviewFefoAllocationAsync(
                        new PreviewMaterialFefoDto
                        {
                            MaterialId = lineDto.MaterialId,
                            BranchId = branchId,
                            RequestedQuantity = lineDto.RequestedQuantity
                        });

                    foreach (var item in preview.Allocations)
                    {
                        line.Allocations.Add(
                            new MaterialRequisitionAllocation
                            {
                                MaterialLotId = item.MaterialLotId,
                                LotNo = method == "QUANTITY_ONLY"
                                    ? null : item.LotNo,
                                AllocationStage = "PRODUCTION_PROPOSAL",
                                AllocationMethod = method,
                                AllocatedQuantity = item.AllocatedQuantity,
                                ReleasedQuantity = 0,
                                AllocatedBy = dto.CreatedBy.Trim(),
                                AllocatedAt = now
                            });
                    }
                }
                else
                {
                    var selections = lineDto.Allocations
                        ?? new List<CreateMaterialRequisitionAllocationDto>();

                    // Support the old single-lot request temporarily.
                    if (selections.Count == 0 &&
                        lineDto.MaterialLotId.HasValue)
                    {
                        selections = new List<CreateMaterialRequisitionAllocationDto>
                {
                    new CreateMaterialRequisitionAllocationDto
                    {
                        MaterialLotId = lineDto.MaterialLotId,
                        Quantity = lineDto.RequestedQuantity
                    }
                };
                    }

                    if (selections.Any(x =>
                        !x.MaterialLotId.HasValue ||
                        x.Quantity <= 0))
                    {
                        throw new InvalidOperationException(
                            "Manual allocations require a lot and positive quantity.");
                    }

                    if (selections
                        .GroupBy(x => x.MaterialLotId)
                        .Any(g => g.Count() > 1))
                    {
                        throw new InvalidOperationException(
                            "Duplicate lots are not allowed within one line.");
                    }

                    if (selections.Sum(x => x.Quantity) >
                        lineDto.RequestedQuantity)
                    {
                        throw new InvalidOperationException(
                            "Allocated quantity exceeds requested quantity.");
                    }

                    foreach (var selection in selections)
                    {
                        var lot = await _context.MaterialLotNumbers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.material_lot_id == selection.MaterialLotId &&
                                x.material_id == lineDto.MaterialId &&
                                x.branch_id == branchId &&
                                x.is_active);

                        if (lot == null)
                            throw new InvalidOperationException(
                                "Selected lot is invalid or belongs to another branch.");

                        if (lot.expiration_date.HasValue &&
                            lot.expiration_date.Value.Date < today)
                        {
                            throw new InvalidOperationException(
                                $"Lot {lot.lot_no} is expired.");
                        }

                        if (selection.Quantity > lot.quantity)
                            throw new InvalidOperationException(
                                $"Insufficient stock in lot {lot.lot_no}.");

                        line.Allocations.Add(
                            new MaterialRequisitionAllocation
                            {
                                MaterialLotId = lot.material_lot_id,
                                LotNo = lot.lot_no,
                                AllocationStage = "PRODUCTION_PROPOSAL",
                                AllocationMethod = "MANUAL",
                                AllocatedQuantity = selection.Quantity,
                                ReleasedQuantity = 0,
                                AllocatedBy = dto.CreatedBy.Trim(),
                                AllocatedAt = now
                            });
                    }
                }

                // Keep legacy line lot fields empty.
                // Multiple lots are now stored in the allocation table.
                line.MaterialLotId = null;
                line.LotNo = null;
                line.ExpirationDate = null;

                requisition.Lines.Add(line);
            }

            _context.MaterialRequisitions.Add(requisition);

            await _context.SaveChangesAsync();

            return requisition.RequisitionId;
        }





        private async Task<string>
            GenerateRequisitionNoAsync()
        {
            var year =
                DateTime.UtcNow.Year;

            var prefix =
                $"MRS-{year}-";

            var lastNo =
                await _context.MaterialRequisitions
                    .AsNoTracking()
                    .Where(x =>
                        x.RequisitionNo.StartsWith(
                            prefix))
                    .OrderByDescending(x =>
                        x.RequisitionNo)
                    .Select(x =>
                        x.RequisitionNo)
                    .FirstOrDefaultAsync();

            var nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(
                    lastNo))
            {
                var numberPart =
                    lastNo.Substring(
                        prefix.Length);

                if (int.TryParse(
                        numberPart,
                        out var parsed))
                {
                    nextNumber =
                        parsed + 1;
                }
            }

            return
                $"{prefix}{nextNumber:D4}";
        }
        public async Task<MaterialFefoPreviewResultDto>
    PreviewFefoAllocationAsync(PreviewMaterialFefoDto dto)
        {
            if (dto.MaterialId <= 0)
                throw new InvalidOperationException(
                    "Invalid material.");

            if (string.IsNullOrWhiteSpace(dto.BranchId))
                throw new InvalidOperationException(
                    "Branch is required.");

            if (dto.RequestedQuantity <= 0)
                throw new InvalidOperationException(
                    "Requested quantity must be greater than zero.");

            var branchId = dto.BranchId.Trim();

            var material = await _context.Materials
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.material_id == dto.MaterialId);

            if (material == null)
                throw new KeyNotFoundException(
                    "Material was not found.");

            var result = new MaterialFefoPreviewResultDto
            {
                MaterialId = dto.MaterialId,
                IsLotTracked = material.is_lot_tracked,
                AllocationMethod = material.is_lot_tracked
                    ? "AUTO_FEFO"
                    : "QUANTITY_ONLY",
                RequestedQuantity = dto.RequestedQuantity
            };

            var today = DateTime.UtcNow.Date;

            var lots = await _context.MaterialLotNumbers
                .AsNoTracking()
                .Where(x =>
                    x.material_id == dto.MaterialId &&
                    x.branch_id == branchId &&
                    x.is_active &&
                    x.quantity > 0)
                .OrderBy(x => x.expiration_date == null)
                .ThenBy(x => x.expiration_date)
                .ThenBy(x => x.manufacturing_date)
                .ThenBy(x => x.material_lot_id)
                .ToListAsync();

            // Exclude expired lots for lot-tracked materials.
            if (material.is_lot_tracked)
            {
                lots = lots
                    .Where(x =>
                        x.expiration_date == null ||
                        x.expiration_date.Value.Date >= today)
                    .ToList();
            }
            else
            {
                // Existing internal stock record for non-lot materials.
                lots = lots
                    .Where(x =>
                        x.lot_no ==
                        $"NON-LOT-MAT-{dto.MaterialId}")
                    .ToList();
            }

            var remaining = dto.RequestedQuantity;

            foreach (var lot in lots)
            {
                if (remaining <= 0)
                    break;

                var allocated = Math.Min(
                    remaining,
                    lot.quantity);

                if (allocated <= 0)
                    continue;

                result.Allocations.Add(
                    new MaterialFefoAllocationDto
                    {
                        MaterialLotId = lot.material_lot_id,

                        LotNo = material.is_lot_tracked
                            ? lot.lot_no
                            : null,

                        ExpirationDate = material.is_lot_tracked
                            ? lot.expiration_date
                            : null,

                        AllocatedQuantity = allocated
                    });

                remaining -= allocated;
            }

            result.AllocatedQuantity =
                dto.RequestedQuantity - remaining;

            result.ShortageQuantity = remaining;

            return result;
        }

        public async Task<object> GetAvailableLotsAsync(
         int materialId,
         string branchId)
        {
            if (materialId <= 0)
            {
                throw new InvalidOperationException(
                    "Invalid material.");
            }

            if (string.IsNullOrWhiteSpace(branchId))
            {
                throw new InvalidOperationException(
                    "Branch is required.");
            }

            branchId = branchId.Trim();

            var material =
                await _context.Materials
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.material_id == materialId);

            if (material == null)
            {
                throw new KeyNotFoundException(
                    "Material was not found.");
            }

            var isLotTracked =
                material.is_lot_tracked;

            var inventoryLots =
                await _context.MaterialLotNumbers
                    .AsNoTracking()
                    .Where(x =>
                        x.material_id == materialId &&
                        x.branch_id == branchId &&
                        x.is_active &&
                        x.quantity > 0)
                    .OrderBy(x =>
                        x.expiration_date == null)
                    .ThenBy(x =>
                        x.expiration_date)
                    .ThenBy(x =>
                        x.lot_no)
                    .ToListAsync();

            var result =
                inventoryLots
                    .Select(x => new
                    {
                        materialLotId =
                            x.material_lot_id,

                        materialId =
                            x.material_id,

                        lotNo =
                            isLotTracked
                                ? x.lot_no
                                : null,

                        quantity =
                            x.quantity,

                        uom =
                            x.uom,

                        manufacturingDate =
                            x.manufacturing_date,

                        expirationDate =
                            x.expiration_date,

                        isLotTracked =
                            isLotTracked
                    })
                    .ToList();

            return result;
        }



        public async Task<MaterialRequisitionDetailsDto?> GetByIdAsync(int id)
        {
            var requisition = await _context.MaterialRequisitions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.RequisitionId == id);

            if (requisition == null)
                return null;

            var branchName = await _context.Branches
                .AsNoTracking()
                .Where(x => x.branch_id == requisition.BranchId)
                .Select(x => x.branch_name)
                .FirstOrDefaultAsync();

            var result = new MaterialRequisitionDetailsDto
            {
                RequisitionId = requisition.RequisitionId,
                RequisitionNo = requisition.RequisitionNo,
                BranchId = requisition.BranchId,
                BranchName = branchName ?? requisition.BranchId,
                RequisitionDate = requisition.RequisitionDate,
                RequestedBy = requisition.RequestedBy,
                ReleasedBy = requisition.ReleasedBy,
                ReceivedBy = requisition.ReceivedBy,
                VerifiedBy = requisition.VerifiedBy,
                TimeRequested = requisition.TimeRequested,
                TimeServed = requisition.TimeServed,
                Status = requisition.Status,
                Remarks = requisition.Remarks,
                CreatedBy = requisition.CreatedBy,
                SubmittedBy = requisition.SubmittedBy,
                SubmittedAt = requisition.SubmittedAt,

                WarehouseConfirmedBy = requisition.WarehouseConfirmedBy,
                WarehouseConfirmedAt = requisition.WarehouseConfirmedAt,

                ApprovedBy = requisition.ApprovedBy,
                ApprovedAt = requisition.ApprovedAt,
                ApprovalRemarks = requisition.ApprovalRemarks,
                RejectedBy = requisition.RejectedBy,
                RejectedAt = requisition.RejectedAt,
                RejectionReason = requisition.RejectionReason,
                PostedBy = requisition.PostedBy,
                PostedAt = requisition.PostedAt,
                CreatedAt = requisition.CreatedAt
            };

            var lines = await (
                from line in _context.MaterialRequisitionLines.AsNoTracking()
                join material in _context.Materials.AsNoTracking()
                    on line.MaterialId equals material.material_id
                where line.RequisitionId == id
                orderby line.RequisitionLineId
                select new
                {
                    Line = line,
                    Material = material
                }
            ).ToListAsync();

            var lineIds = lines
                .Select(x => x.Line.RequisitionLineId)
                .ToList();

            var allocations = await _context.MaterialRequisitionAllocations
                .AsNoTracking()
                .Where(x => lineIds.Contains(x.RequisitionLineId))
                .OrderBy(x => x.AllocationId)
                .ToListAsync();

            var lotIds = allocations
                .Where(x => x.MaterialLotId.HasValue)
                .Select(x => x.MaterialLotId.Value)
                .Distinct()
                .ToList();

            var lots = await _context.MaterialLotNumbers
                .AsNoTracking()
                .Where(x => lotIds.Contains(x.material_lot_id))
                .ToDictionaryAsync(x => x.material_lot_id);

            foreach (var record in lines)
            {
                var line = record.Line;
                var material = record.Material;



                var lineAllocations = allocations
                    .Where(x =>
                        x.RequisitionLineId == line.RequisitionLineId)
                    .ToList();

                var productionAllocations = lineAllocations
                    .Where(x =>
                        x.AllocationStage == "PRODUCTION_PROPOSAL")
                    .ToList();

                var warehouseAllocations = lineAllocations
                    .Where(x =>
                        x.AllocationStage == "WAREHOUSE_CONFIRMED")
                    .ToList();

                // A completed Warehouse confirmation may have
                // zero allocation rows when stock is unavailable.
                var requisitionConfirmed =
                    requisition.WarehouseConfirmedAt.HasValue;

                var effectiveAllocations = requisitionConfirmed
                    ? warehouseAllocations
                    : productionAllocations;

                var allocatedQty = effectiveAllocations
                    .Sum(x => x.AllocatedQuantity);

                var releasedQty = warehouseAllocations
                    .Sum(x => x.ReleasedQuantity);

                var availableQty = effectiveAllocations
                    .Where(x => x.MaterialLotId.HasValue)
                    .Select(x => x.MaterialLotId!.Value)
                    .Distinct()
                    .Sum(lotId =>
                        lots.TryGetValue(lotId, out var lot)
                            ? lot.quantity
                            : 0m);



                var lineResult = new MaterialRequisitionDetailsLineDto
                {
                    RequisitionLineId = line.RequisitionLineId,
                    MaterialId = line.MaterialId,
                    MaterialCode = material.material_code,
                    MaterialName = material.material_name,

                    // Legacy single-lot fields retained for older clients.
                    MaterialLotId = line.MaterialLotId,
                    LotNo = line.LotNo,

                    LotDisplay = material.is_lot_tracked
    ? (effectiveAllocations.Count == 0
        ? "No lot allocated"
        : effectiveAllocations.Count == 1
            ? effectiveAllocations[0].LotNo ?? "—"
            : $"{effectiveAllocations.Count} lots")
    : "Not Lot Tracked",

                    ExpirationDate = line.ExpirationDate,

                    AllocationMethod = line.AllocationMethod,

                    RequestedQuantity = line.RequestedQuantity,

                    // Effective allocation used by the existing UI
                    AllocatedQuantity = allocatedQty,

                    // Production's original proposed quantity
                    ProductionProposedQuantity = productionAllocations
    .Sum(x => x.AllocatedQuantity),

                    // Warehouse's physically confirmed quantity
                    WarehouseConfirmedQuantity = warehouseAllocations
    .Sum(x => x.AllocatedQuantity),

                    ShortageQuantity = Math.Max(
                        0m, line.RequestedQuantity - allocatedQty),
                    ReleasedQuantity = releasedQty,
                    ActualQuantity = line.ActualQuantity,
                    AvailableQuantity = availableQty,
                    Uom = line.Uom,
                    Remarks = line.Remarks,
                    IsLotTracked = material.is_lot_tracked
                };

                foreach (var allocation in lineAllocations)
                {
                    lineResult.Allocations.Add(
                        new MaterialRequisitionAllocationDto
                        {
                            AllocationId = allocation.AllocationId,
                            MaterialLotId = allocation.MaterialLotId,
                            LotNo = allocation.LotNo,
                            AllocationStage = allocation.AllocationStage,
                            AllocationMethod = allocation.AllocationMethod,
                            AllocatedQuantity = allocation.AllocatedQuantity,
                            ReleasedQuantity = allocation.ReleasedQuantity
                        });
                }

                result.Lines.Add(lineResult);
            }

            return result;
        }




        public async Task ReleaseAsync(
            int requisitionId,
            PostMaterialRequisitionDto dto)
        {
            if (dto == null)
                throw new InvalidOperationException(
                    "Release details are required.");

            if (string.IsNullOrWhiteSpace(dto.ReleasedBy) ||
                string.IsNullOrWhiteSpace(dto.ReceivedBy) ||
                string.IsNullOrWhiteSpace(dto.VerifiedBy) ||
                string.IsNullOrWhiteSpace(dto.PostedBy))
            {
                throw new InvalidOperationException(
                    "Released By, Received By, Verified By and Posted By are required.");
            }

            if (dto.Lines == null || dto.Lines.Count == 0)
                throw new InvalidOperationException(
                    "At least one material line is required.");

            if (dto.Lines
                .GroupBy(x => x.RequisitionLineId)
                .Any(x => x.Count() > 1))
            {
                throw new InvalidOperationException(
                    "Duplicate requisition lines are not allowed.");
            }

            var now = DateTime.UtcNow;

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                var requisition = await _context.MaterialRequisitions
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.RequisitionId == requisitionId);

                if (requisition == null)
                    throw new KeyNotFoundException(
                        "Material requisition was not found.");

                if (requisition.Status != "APPROVED")
                    throw new InvalidOperationException(
                        "Only APPROVED requisitions can be released.");

                if (!requisition.WarehouseConfirmedAt.HasValue)
                    throw new InvalidOperationException(
                        "Warehouse allocation confirmation is required.");

                if (dto.Lines.Count != requisition.Lines.Count ||
                    dto.Lines.Any(x =>
                        !requisition.Lines.Any(y =>
                            y.RequisitionLineId == x.RequisitionLineId)))
                {
                    throw new InvalidOperationException(
                        "All requisition lines must be included exactly once.");
                }

                var lineIds = requisition.Lines
                    .Select(x => x.RequisitionLineId)
                    .ToList();

                var allocations = await _context
                    .MaterialRequisitionAllocations
                    .Where(x =>
                        lineIds.Contains(x.RequisitionLineId) &&
                        x.AllocationStage == "WAREHOUSE_CONFIRMED")
                    .ToListAsync();

                if (allocations.Any(x => x.ReleasedQuantity > 0))
                    throw new InvalidOperationException(
                        "This requisition contains previously released allocations.");

                var materialIds = requisition.Lines
                    .Select(x => x.MaterialId)
                    .Distinct()
                    .ToList();

                var materials = await _context.Materials
                    .AsNoTracking()
                    .Where(x => materialIds.Contains(x.material_id))
                    .ToDictionaryAsync(x => x.material_id);

                var lotIds = allocations
                    .Where(x => x.MaterialLotId.HasValue)
                    .Select(x => x.MaterialLotId!.Value)
                    .Distinct()
                    .ToList();

                var lots = await _context.MaterialLotNumbers
                    .Where(x => lotIds.Contains(x.material_lot_id))
                    .ToDictionaryAsync(x => x.material_lot_id);

                var totalReleasedByLot =
                    new Dictionary<int, decimal>();

                var totalReleased = 0m;

                foreach (var line in requisition.Lines)
                {
                    var lineDto = dto.Lines.Single(x =>
                        x.RequisitionLineId == line.RequisitionLineId);

                    if (!materials.TryGetValue(
                        line.MaterialId, out var material))
                    {
                        throw new KeyNotFoundException(
                            $"Material {line.MaterialId} was not found.");
                    }

                    var lineAllocations = allocations
                        .Where(x =>
                            x.RequisitionLineId == line.RequisitionLineId)
                        .ToList();

                    var confirmedQuantity = lineAllocations
                        .Sum(x => x.AllocatedQuantity);

                    if (confirmedQuantity > line.RequestedQuantity)
                        throw new InvalidOperationException(
                            $"Confirmed quantity exceeds requested quantity for {material.material_name}.");

                    if (lineDto.ActualQuantity < 0)
                        throw new InvalidOperationException(
                            "Actual quantity cannot be negative.");

                    if (confirmedQuantity == 0 &&
                        lineDto.ActualQuantity != 0)
                    {
                        throw new InvalidOperationException(
                            $"Actual quantity must be zero when no stock is confirmed for {material.material_name}.");
                    }

                    // Actual quantity is recorded separately.
                    // Inventory OUT uses confirmed allocation quantities.
                    line.ActualQuantity = lineDto.ActualQuantity;
                    line.UpdatedAt = now;

                    foreach (var allocation in lineAllocations)
                    {
                        if (!allocation.MaterialLotId.HasValue)
                            throw new InvalidOperationException(
                                $"Inventory stock record is missing for {material.material_name}.");

                        if (allocation.AllocatedQuantity <= 0)
                            throw new InvalidOperationException(
                                "Confirmed allocation quantity must be positive.");

                        var lotId = allocation.MaterialLotId.Value;

                        if (!lots.TryGetValue(lotId, out var lot) ||
                            lot.material_id != line.MaterialId ||
                            lot.branch_id != requisition.BranchId ||
                            !lot.is_active)
                        {
                            throw new InvalidOperationException(
                                $"Confirmed inventory lot for {material.material_name} is invalid.");
                        }

                        if (material.is_lot_tracked &&
                            lot.expiration_date.HasValue &&
                            lot.expiration_date.Value.Date < now.Date)
                        {
                            throw new InvalidOperationException(
                                $"Lot {lot.lot_no} is expired.");
                        }

                        if (!material.is_lot_tracked &&
                            lot.lot_no !=
                                $"NON-LOT-MAT-{line.MaterialId}")
                        {
                            throw new InvalidOperationException(
                                $"Invalid non-lot stock record for {material.material_name}.");
                        }

                        totalReleasedByLot.TryGetValue(
                            lotId, out var alreadyCounted);

                        totalReleasedByLot[lotId] =
                            alreadyCounted + allocation.AllocatedQuantity;
                    }
                }

                // Validate all stock before deducting any quantity.
                foreach (var item in totalReleasedByLot)
                {
                    var lot = lots[item.Key];

                    if (item.Value > lot.quantity)
                        throw new InvalidOperationException(
                            $"Insufficient stock in lot {lot.lot_no}. " +
                            $"Required: {item.Value:0.####}, " +
                            $"Available: {lot.quantity:0.####}.");
                }

                foreach (var allocation in allocations)
                {
                    var line = requisition.Lines.Single(x =>
                        x.RequisitionLineId == allocation.RequisitionLineId);

                    var lot = lots[allocation.MaterialLotId!.Value];

                    var quantityToRelease =
                        allocation.AllocatedQuantity;

                    lot.quantity -= quantityToRelease;
                    lot.updated_at = now;

                    allocation.ReleasedQuantity =
                        quantityToRelease;

                    totalReleased += quantityToRelease;

                    _context.MaterialInventoryTransactions.Add(
                        new MaterialInventoryTransaction
                        {
                            material_id = line.MaterialId,
                            branch_id = requisition.BranchId,
                            lot_no = lot.lot_no,
                            transaction_type = "MATERIAL_RELEASE",
                            quantity = -quantityToRelease,
                            uom = lot.uom,
                            supplier_id = lot.supplier_id,
                            reference_type = "MATERIAL_REQUISITION",
                            reference_id = requisition.RequisitionId,
                            reference_no = requisition.RequisitionNo,
                            remarks = line.Remarks,
                            encoded_by = dto.PostedBy.Trim(),
                            transaction_date = now,
                            created_at = now
                        });
                }

                if (totalReleased <= 0)
                    throw new InvalidOperationException(
                        "No confirmed stock is available for release.");

                requisition.ReleasedBy = dto.ReleasedBy.Trim();
                requisition.ReceivedBy = dto.ReceivedBy.Trim();
                requisition.VerifiedBy = dto.VerifiedBy.Trim();
                requisition.TimeServed = dto.TimeServed ?? now;

                requisition.PostedBy = dto.PostedBy.Trim();
                requisition.PostedAt = now;
                requisition.Status = "RELEASED";
                requisition.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }




        public async Task ConfirmWarehouseAllocationAsync(
            int requisitionId,
            ConfirmMaterialAllocationDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.ConfirmedBy))
                throw new InvalidOperationException(
                    "Confirmed By is required.");

            if (dto.Lines == null || dto.Lines.Count == 0)
                throw new InvalidOperationException(
                    "At least one requisition line is required.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            var requisition = await _context.MaterialRequisitions
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x =>
                    x.RequisitionId == requisitionId);

            if (requisition == null)
                throw new KeyNotFoundException(
                    "Material requisition was not found.");

            if (requisition.Status != "FOR_STAFF_APPROVAL")
                throw new InvalidOperationException(
                    "Only MRS awaiting Staff approval can be confirmed.");

            if (dto.Lines.Count != requisition.Lines.Count ||
                dto.Lines.Select(x => x.RequisitionLineId)
                    .Distinct().Count() != dto.Lines.Count)
                throw new InvalidOperationException(
                    "All requisition lines must be included exactly once.");

            var now = DateTime.UtcNow;

            var newAllocations =
                new List<MaterialRequisitionAllocation>();

            // Track quantities across all lines to prevent
            // allocating the same stock more than once.
            var totalsByLot = new Dictionary<int, decimal>();
            var stockByLot = new Dictionary<int, decimal>();

            foreach (var lineDto in dto.Lines)
            {
                var line = requisition.Lines.FirstOrDefault(x =>
                    x.RequisitionLineId == lineDto.RequisitionLineId);

                if (line == null)
                    throw new InvalidOperationException(
                        "Requisition line does not belong to this MRS.");

                var material = await _context.Materials
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.material_id == line.MaterialId);

                if (material == null)
                    throw new KeyNotFoundException(
                        $"Material {line.MaterialId} was not found.");

                if (lineDto.Allocations == null)
                    throw new InvalidOperationException(
                        "Allocations are required for every line.");

                if (lineDto.Allocations.Any(x => x.Quantity <= 0))
                    throw new InvalidOperationException(
                        "Allocation quantities must be positive.");

                if (lineDto.Allocations
                    .GroupBy(x => x.MaterialLotId)
                    .Any(g => g.Count() > 1))
                    throw new InvalidOperationException(
                        "Duplicate lots are not allowed within a line.");

                var total = lineDto.Allocations.Sum(x => x.Quantity);

                if (total > line.RequestedQuantity)
                    throw new InvalidOperationException(
                        $"Allocation exceeds requested quantity for " +
                        $"{material.material_name}.");

                foreach (var item in lineDto.Allocations)
                {
                    if (material.is_lot_tracked &&
                        !item.MaterialLotId.HasValue)
                        throw new InvalidOperationException(
                            $"A lot is required for {material.material_name}.");

                    // Non-lot materials use the existing
                    // internal stock record.
                    var lot = await _context.MaterialLotNumbers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.material_id == line.MaterialId &&
                            x.branch_id == requisition.BranchId &&
                            x.is_active &&
                            (material.is_lot_tracked
                                ? x.material_lot_id == item.MaterialLotId
                                : x.lot_no ==
                                  $"NON-LOT-MAT-{line.MaterialId}"));

                    if (lot == null)
                        throw new InvalidOperationException(
                            $"Invalid or unavailable stock lot for " +
                            $"{material.material_name}.");

                    if (material.is_lot_tracked &&
                        lot.expiration_date.HasValue &&
                        lot.expiration_date.Value.Date < now.Date)
                        throw new InvalidOperationException(
                            $"Lot {lot.lot_no} is expired.");

                    if (!stockByLot.ContainsKey(lot.material_lot_id))
                        stockByLot[lot.material_lot_id] = lot.quantity;

                    totalsByLot.TryGetValue(
                        lot.material_lot_id, out var allocated);

                    allocated += item.Quantity;

                    if (allocated > stockByLot[lot.material_lot_id])
                        throw new InvalidOperationException(
                            $"Insufficient stock for lot {lot.lot_no}.");

                    totalsByLot[lot.material_lot_id] = allocated;

                    newAllocations.Add(
                        new MaterialRequisitionAllocation
                        {
                            RequisitionLineId = line.RequisitionLineId,
                            MaterialLotId = lot.material_lot_id,
                            LotNo = material.is_lot_tracked
                                ? lot.lot_no
                                : null,
                            AllocationStage = "WAREHOUSE_CONFIRMED",
                            AllocationMethod = material.is_lot_tracked
                                ? "MANUAL"
                                : "QUANTITY_ONLY",
                            AllocatedQuantity = item.Quantity,
                            ReleasedQuantity = 0,
                            AllocatedBy = dto.ConfirmedBy.Trim(),
                            AllocatedAt = now,
                            Remarks = item.Remarks?.Trim()
                        });
                }
            }

            var lineIds = requisition.Lines
                .Select(x => x.RequisitionLineId)
                .ToList();

            // Remove only previous Warehouse confirmations.
            // Original Production proposals are preserved.
            var previousConfirmations =
                await _context.MaterialRequisitionAllocations
                    .Where(x =>
                        lineIds.Contains(x.RequisitionLineId) &&
                        x.AllocationStage == "WAREHOUSE_CONFIRMED")
                    .ToListAsync();

            if (previousConfirmations.Any(x =>
                x.ReleasedQuantity > 0))
                throw new InvalidOperationException(
                    "Released allocations cannot be replaced.");


            _context.MaterialRequisitionAllocations
                .RemoveRange(previousConfirmations);

            _context.MaterialRequisitionAllocations
                .AddRange(newAllocations);

            // Save Warehouse confirmation information
            requisition.WarehouseConfirmedBy =
                dto.ConfirmedBy.Trim();

            requisition.WarehouseConfirmedAt = now;
            requisition.UpdatedAt = now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

        }


        public async Task SubmitForApprovalAsync(
    int requisitionId,
    string submittedBy)
        {
            var requisition =
                await _context.MaterialRequisitions
                    .FirstOrDefaultAsync(x =>
                        x.RequisitionId == requisitionId);

            if (requisition == null)
                throw new KeyNotFoundException(
                    "Material requisition was not found.");

            if (!string.Equals(
                    requisition.Status,
                    "DRAFT",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only DRAFT requisitions can be submitted.");
            }

            requisition.Status =
     "FOR_STAFF_APPROVAL";

            requisition.SubmittedBy =
                submittedBy;

            requisition.SubmittedAt =
                DateTime.UtcNow;

            requisition.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }



        public async Task ApproveByHeadAsync(
            int requisitionId,
            string approvedBy,
            string? remarks)
        {
            if (string.IsNullOrWhiteSpace(approvedBy))
                throw new InvalidOperationException(
                    "Warehouse Head approver is required.");

            var requisition = await _context.MaterialRequisitions
                .FirstOrDefaultAsync(x =>
                    x.RequisitionId == requisitionId);

            if (requisition == null)
                throw new KeyNotFoundException(
                    "Material requisition was not found.");

            if (requisition.Status != "FOR_HEAD_APPROVAL")
                throw new InvalidOperationException(
                    "MRS is not awaiting Warehouse Head approval.");

            if (string.IsNullOrWhiteSpace(
                requisition.StaffApprovedBy))
                throw new InvalidOperationException(
                    "Warehouse Staff approval is required first.");

            if (string.Equals(
                requisition.StaffApprovedBy,
                approvedBy.Trim(),
                StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Staff and Head approvals must use different users.");

            var now = DateTime.UtcNow;

            requisition.HeadApprovedBy = approvedBy.Trim();
            requisition.HeadApprovedAt = now;
            requisition.HeadApprovalRemarks = remarks?.Trim();

            // Retain existing final approval fields.
            requisition.ApprovedBy = approvedBy.Trim();
            requisition.ApprovedAt = now;
            requisition.ApprovalRemarks = remarks?.Trim();

            requisition.Status = "APPROVED";
            requisition.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }




        public async Task ApproveByStaffAsync(
            int requisitionId,
            string approvedBy,
            string? remarks)
        {
            if (string.IsNullOrWhiteSpace(approvedBy))
                throw new InvalidOperationException(
                    "Warehouse Staff approver is required.");

            var requisition = await _context.MaterialRequisitions
                .FirstOrDefaultAsync(x =>
                    x.RequisitionId == requisitionId);


            if (requisition == null)
                throw new KeyNotFoundException(
                    "Material requisition was not found.");

            if (requisition.Status != "FOR_STAFF_APPROVAL")
                throw new InvalidOperationException(
                    "MRS is not awaiting Warehouse Staff approval.");

            if (!requisition.WarehouseConfirmedAt.HasValue)
                throw new InvalidOperationException(
                    "Warehouse allocation must be confirmed before Staff approval.");



            requisition.StaffApprovedBy = approvedBy.Trim();
            requisition.StaffApprovedAt = DateTime.UtcNow;
            requisition.StaffApprovalRemarks = remarks?.Trim();

            requisition.Status = "FOR_HEAD_APPROVAL";
            requisition.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }



        public async Task ApproveAsync(
    int requisitionId,
    string approvedBy,
    string? remarks)
        {
            var requisition =
                await _context.MaterialRequisitions
                    .FirstOrDefaultAsync(x =>
                        x.RequisitionId == requisitionId);

            if (requisition == null)
                throw new KeyNotFoundException(
                    "Material requisition was not found.");

            if (!string.Equals(
                    requisition.Status,
                    "FOR_APPROVAL",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only requisitions FOR APPROVAL can be approved.");
            }

            requisition.Status =
                "APPROVED";

            requisition.ApprovedBy =
                approvedBy;

            requisition.ApprovedAt =
                DateTime.UtcNow;

            requisition.ApprovalRemarks =
                string.IsNullOrWhiteSpace(remarks)
                    ? null
                    : remarks.Trim();

            requisition.RejectedBy = null;
            requisition.RejectedAt = null;
            requisition.RejectionReason = null;

            requisition.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }


        public async Task RejectAsync(
            int requisitionId,
            string rejectedBy,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(rejectedBy))
                throw new InvalidOperationException(
                    "Rejected By is required.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException(
                    "Rejection reason is required.");

            var requisition = await _context.MaterialRequisitions
                .FirstOrDefaultAsync(x =>
                    x.RequisitionId == requisitionId);

            if (requisition == null)
                throw new KeyNotFoundException(
                    "Material requisition was not found.");

            if (requisition.Status != "FOR_STAFF_APPROVAL" &&
                requisition.Status != "FOR_HEAD_APPROVAL")
            {
                throw new InvalidOperationException(
                    "Only requisitions awaiting Staff or Head approval can be rejected.");
            }

            var now = DateTime.UtcNow;

            requisition.RejectedBy = rejectedBy.Trim();
            requisition.RejectedAt = now;
            requisition.RejectionReason = reason.Trim();

            requisition.Status = "REJECTED";
            requisition.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }



        public async Task<object> GetAllAsync()
        {
            var requisitions =
                await (
                    from requisition in
                        _context.MaterialRequisitions
                            .AsNoTracking()

                    join branch in
                        _context.Branches.AsNoTracking()
                        on requisition.BranchId
                        equals branch.branch_id
                        into branchJoin

                    from branch in branchJoin.DefaultIfEmpty()

                        // Requested By
                    join requestedUser in
                        _context.Users.AsNoTracking()
                        on requisition.RequestedBy
                        equals requestedUser.user_id
                        into requestedUserJoin

                    from requestedUser in
                        requestedUserJoin.DefaultIfEmpty()

                        // Created By
                    join createdUser in
                        _context.Users.AsNoTracking()
                        on requisition.CreatedBy
                        equals createdUser.user_id
                        into createdUserJoin

                    from createdUser in
                        createdUserJoin.DefaultIfEmpty()

                    orderby requisition.RequisitionId descending

                    select new
                    {
                        requisitionId =
                            requisition.RequisitionId,

                        requisitionNo =
                            requisition.RequisitionNo,

                        requisitionDate =
                            requisition.RequisitionDate,

                        branchId =
                            requisition.BranchId,

                        branchName =
                            branch != null
                                ? branch.branch_name
                                : requisition.BranchId,

                        // Keep ID
                        requestedBy =
                            requisition.RequestedBy,

                        // Display name
                        requestedByName =
                            requestedUser != null
                                ? requestedUser.full_name
                                : requisition.RequestedBy,

                        timeRequested =
                            requisition.TimeRequested,

                        status =
                            requisition.Status,

                        // Keep ID
                        createdBy =
                            requisition.CreatedBy,

                        // Display name
                        createdByName =
                            createdUser != null
                                ? createdUser.full_name
                                : requisition.CreatedBy,

                        createdAt =
                            requisition.CreatedAt,

                        submittedBy =
                            requisition.SubmittedBy,

                        submittedAt =
                            requisition.SubmittedAt,

                        approvedBy =
                            requisition.ApprovedBy,

                        approvedAt =
                            requisition.ApprovedAt,

                        postedBy =
                            requisition.PostedBy,

                        postedAt =
                            requisition.PostedAt
                    }
                )
                .ToListAsync();

            return requisitions;
        }


    }
}