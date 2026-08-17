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
            {
                throw new InvalidOperationException(
                    "Branch is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.CreatedBy))
            {
                throw new InvalidOperationException(
                    "Created by is required.");
            }

            if (dto.Lines == null ||
                dto.Lines.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one material is required.");
            }

            var now = DateTime.UtcNow;

            var requisitionNo =
                await GenerateRequisitionNoAsync();

            var requisition =
                new MaterialRequisition
                {
                    RequisitionNo =
                        requisitionNo,

                    BranchId =
                        dto.BranchId.Trim(),

                    RequisitionDate =
                        dto.RequisitionDate == default
                            ? now
                            : dto.RequisitionDate,

                    RequestedBy =
                        string.IsNullOrWhiteSpace(dto.RequestedBy)
                            ? null
                            : dto.RequestedBy.Trim(),

                    TimeRequested =
                        dto.TimeRequested,

                    Status =
                        "DRAFT",

                    Remarks =
                        string.IsNullOrWhiteSpace(dto.Remarks)
                            ? null
                            : dto.Remarks.Trim(),

                    CreatedBy =
                        dto.CreatedBy.Trim(),

                    CreatedAt =
                        now
                };

            foreach (var lineDto in dto.Lines)
            {
                if (lineDto.MaterialId <= 0)
                {
                    throw new InvalidOperationException(
                        "Invalid material.");
                }

                if (lineDto.RequestedQuantity <= 0)
                {
                    throw new InvalidOperationException(
                        "Requested quantity must be greater than zero.");
                }

                var material =
                    await _context.Materials
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.material_id ==
                            lineDto.MaterialId);

                if (material == null)
                {
                    throw new KeyNotFoundException(
                        $"Material {lineDto.MaterialId} was not found.");
                }

                MaterialRequisitionLine line;

                if (material.is_lot_tracked)
                {
                    if (!lineDto.MaterialLotId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"{material.material_name} requires a lot.");
                    }

                    var lot =
                        await _context.MaterialLotNumbers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.material_lot_id ==
                                    lineDto.MaterialLotId.Value &&
                                x.material_id ==
                                    lineDto.MaterialId &&
                                x.branch_id ==
                                    dto.BranchId &&
                                x.is_active);

                    if (lot == null)
                    {
                        throw new InvalidOperationException(
                            $"Selected lot for {material.material_name} was not found in the selected branch.");
                    }

                    line =
                        new MaterialRequisitionLine
                        {
                            MaterialId =
                                material.material_id,

                            MaterialLotId =
                                lot.material_lot_id,

                            LotNo =
                                lot.lot_no,

                            RequestedQuantity =
                                lineDto.RequestedQuantity,

                            ActualQuantity =
                                null,

                            Uom =
                                material.uom,

                            ExpirationDate =
                                lot.expiration_date,

                            Remarks =
                                lineDto.Remarks,

                            CreatedAt =
                                now
                        };
                }
                else
                {
                    var inventoryLot =
                        await _context.MaterialLotNumbers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.material_id ==
                                    lineDto.MaterialId &&
                                x.branch_id ==
                                    dto.BranchId &&
                                x.lot_no ==
                                    $"NON-LOT-MAT-{lineDto.MaterialId}" &&
                                x.is_active);

                    if (inventoryLot == null)
                    {
                        throw new InvalidOperationException(
                            $"No inventory was found for {material.material_name} in the selected branch.");
                    }

                    line =
                        new MaterialRequisitionLine
                        {
                            MaterialId =
                                material.material_id,

                            MaterialLotId =
                                inventoryLot.material_lot_id,

                            LotNo =
                                inventoryLot.lot_no,

                            RequestedQuantity =
                                lineDto.RequestedQuantity,

                            ActualQuantity =
                                null,

                            Uom =
                                material.uom,

                            ExpirationDate =
                                null,

                            Remarks =
                                lineDto.Remarks,

                            CreatedAt =
                                now
                        };
                }

                requisition.Lines.Add(line);
            }

            _context.MaterialRequisitions.Add(
                requisition);

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


        public async Task<MaterialRequisitionDetailsDto?>
    GetByIdAsync(int id)
        {
            var requisition =
                await _context.MaterialRequisitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.RequisitionId == id);

            if (requisition == null)
            {
                return null;
            }

            var branchName =
                await _context.Branches
                    .AsNoTracking()
                    .Where(x =>
                        x.branch_id ==
                        requisition.BranchId)
                    .Select(x =>
                        x.branch_name)
                    .FirstOrDefaultAsync();

            var lines =
                await (
                    from line in
                        _context.MaterialRequisitionLines
                            .AsNoTracking()

                    join material in
                        _context.Materials
                            .AsNoTracking()
                        on line.MaterialId
                        equals material.material_id

                    join lot in
                        _context.MaterialLotNumbers
                            .AsNoTracking()
                        on line.MaterialLotId
                        equals lot.material_lot_id
                        into lotJoin

                    from lot in
                        lotJoin.DefaultIfEmpty()

                    where
                        line.RequisitionId == id

                    orderby
                        line.RequisitionLineId

                    select new
                    {
                        Line = line,
                        Material = material,
                        Lot = lot
                    }
                )
                .ToListAsync();

            var result =
                new MaterialRequisitionDetailsDto
                {
                    RequisitionId =
                        requisition.RequisitionId,

                    RequisitionNo =
                        requisition.RequisitionNo,

                    BranchId =
                        requisition.BranchId,

                    BranchName =
                        branchName ??
                        requisition.BranchId,

                    RequisitionDate =
                        requisition.RequisitionDate,

                    RequestedBy =
                        requisition.RequestedBy,

                    ReleasedBy =
                        requisition.ReleasedBy,

                    ReceivedBy =
                        requisition.ReceivedBy,

                    VerifiedBy =
                        requisition.VerifiedBy,

                    TimeRequested =
                        requisition.TimeRequested,

                    TimeServed =
                        requisition.TimeServed,

                    Status =
                        requisition.Status,

                    Remarks =
                        requisition.Remarks,

                    CreatedBy =
    requisition.CreatedBy,

                    SubmittedBy =
    requisition.SubmittedBy,

                    SubmittedAt =
    requisition.SubmittedAt,

                    ApprovedBy =
    requisition.ApprovedBy,

                    ApprovedAt =
    requisition.ApprovedAt,

                    ApprovalRemarks =
    requisition.ApprovalRemarks,

                    RejectedBy =
    requisition.RejectedBy,

                    RejectedAt =
    requisition.RejectedAt,

                    RejectionReason =
    requisition.RejectionReason,

                    PostedBy =
    requisition.PostedBy,

                    CreatedAt =
                        requisition.CreatedAt,

                    PostedAt =
                        requisition.PostedAt
                };

            foreach (var record in lines)
            {
                var line =
                    record.Line;

                var material =
                    record.Material;

                var lot =
                    record.Lot;

                result.Lines.Add(
                    new MaterialRequisitionDetailsLineDto
                    {
                        RequisitionLineId =
                            line.RequisitionLineId,

                        MaterialId =
                            line.MaterialId,

                        MaterialCode =
                            material.material_code,

                        MaterialName =
                            material.material_name,

                        MaterialLotId =
                            line.MaterialLotId,

                        LotNo =
                            line.LotNo,

                        LotDisplay =
                            material.is_lot_tracked
                                ? line.LotNo ?? "—"
                                : "Not Lot Tracked",

                        ExpirationDate =
                            line.ExpirationDate,

                        RequestedQuantity =
                            line.RequestedQuantity,

                        ActualQuantity =
                            line.ActualQuantity,

                        AvailableQuantity =
                            lot != null
                                ? lot.quantity
                                : 0,

                        Uom =
                            line.Uom,

                        Remarks =
                            line.Remarks,

                        IsLotTracked =
                            material.is_lot_tracked
                    });
            }

            return result;
        }


        public async Task ReleaseAsync(
    int requisitionId,
    PostMaterialRequisitionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ReleasedBy))
                throw new InvalidOperationException(
                    "Released By is required.");

            if (string.IsNullOrWhiteSpace(dto.ReceivedBy))
                throw new InvalidOperationException(
                    "Received By is required.");

            if (string.IsNullOrWhiteSpace(dto.VerifiedBy))
                throw new InvalidOperationException(
                    "Verified By is required.");

            if (string.IsNullOrWhiteSpace(dto.PostedBy))
                throw new InvalidOperationException(
                    "Posted By is required.");

            if (dto.Lines == null ||
                dto.Lines.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one material line is required.");
            }

            var requisition =
                await _context.MaterialRequisitions
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.RequisitionId == requisitionId);

            if (requisition == null)
            {
                throw new KeyNotFoundException(
                    "Material requisition was not found.");
            }

            if (!string.Equals(
        requisition.Status,
        "APPROVED",
        StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only APPROVED requisitions can be released.");
            }

            var now =
                DateTime.UtcNow;

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var lineDto in dto.Lines)
                {
                    var line =
                        requisition.Lines
                            .FirstOrDefault(x =>
                                x.RequisitionLineId ==
                                lineDto.RequisitionLineId);

                    if (line == null)
                    {
                        throw new InvalidOperationException(
                            $"Requisition line {lineDto.RequisitionLineId} was not found.");
                    }

                    if (lineDto.ActualQuantity <= 0)
                    {
                        throw new InvalidOperationException(
                            "Actual quantity must be greater than zero.");
                    }

                    var material =
                        await _context.Materials
                            .FirstOrDefaultAsync(x =>
                                x.material_id ==
                                line.MaterialId);

                    if (material == null)
                    {
                        throw new KeyNotFoundException(
                            $"Material {line.MaterialId} was not found.");
                    }

                    if (!line.MaterialLotId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"Inventory lot for {material.material_name} was not found.");
                    }

                    var inventoryLot =
                        await _context.MaterialLotNumbers
                            .FirstOrDefaultAsync(x =>
                                x.material_lot_id ==
                                line.MaterialLotId.Value &&
                                x.material_id ==
                                line.MaterialId &&
                                x.branch_id ==
                                requisition.BranchId &&
                                x.is_active);

                    if (inventoryLot == null)
                    {
                        throw new InvalidOperationException(
                            $"Inventory lot for {material.material_name} is no longer available.");
                    }

                    if (lineDto.ActualQuantity >
                        inventoryLot.quantity)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for {material.material_name}. " +
                            $"Available: {inventoryLot.quantity:0.####} {inventoryLot.uom}.");
                    }

                    inventoryLot.quantity -=
                        lineDto.ActualQuantity;

                    inventoryLot.updated_at =
                        now;

                    line.ActualQuantity =
                        lineDto.ActualQuantity;

                    line.UpdatedAt =
                        now;

                    var inventoryTransaction =
                        new MaterialInventoryTransaction
                        {
                            material_id =
                                line.MaterialId,

                            branch_id =
                                requisition.BranchId,

                            lot_no =
                                inventoryLot.lot_no,

                            transaction_type =
                                "MATERIAL_RELEASE",

                            quantity =
                                -lineDto.ActualQuantity,

                            uom =
                                inventoryLot.uom,

                            supplier_id =
                                inventoryLot.supplier_id,

                            reference_type =
                                "MATERIAL_REQUISITION",

                            reference_id =
                                requisition.RequisitionId,

                            reference_no =
                                requisition.RequisitionNo,

                            remarks =
                                line.Remarks,

                            encoded_by =
                                dto.PostedBy,

                            transaction_date =
                                now,

                            created_at =
                                now
                        };

                    _context.MaterialInventoryTransactions.Add(
                        inventoryTransaction);
                }

                requisition.ReleasedBy =
                    dto.ReleasedBy.Trim();

                requisition.ReceivedBy =
                    dto.ReceivedBy.Trim();

                requisition.VerifiedBy =
                    dto.VerifiedBy.Trim();

                requisition.TimeServed =
                    dto.TimeServed ?? now;

                requisition.Status =
     "RELEASED";

                requisition.PostedBy =
                    dto.PostedBy.Trim();

                requisition.PostedAt =
                    now;

                requisition.UpdatedAt =
                    now;

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
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
                "FOR_APPROVAL";

            requisition.SubmittedBy =
                submittedBy;

            requisition.SubmittedAt =
                DateTime.UtcNow;

            requisition.UpdatedAt =
                DateTime.UtcNow;

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
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException(
                    "Rejection reason is required.");
            }

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
                    "Only requisitions FOR APPROVAL can be rejected.");
            }

            requisition.Status =
                "REJECTED";

            requisition.RejectedBy =
                rejectedBy;

            requisition.RejectedAt =
                DateTime.UtcNow;

            requisition.RejectionReason =
                reason.Trim();

            requisition.UpdatedAt =
                DateTime.UtcNow;

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