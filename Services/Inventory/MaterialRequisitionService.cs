using inventory_api.Data;
using inventory_api.DTOs.Inventory.MaterialRequisitions;
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
    }
}