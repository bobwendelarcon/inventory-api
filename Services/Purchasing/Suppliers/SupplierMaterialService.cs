using inventory_api.Data;
using inventory_api.DTOs.Purchasing.Suppliers.Mappings;
using inventory_api.Models.Purchasing.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.Suppliers
{
    public class SupplierMaterialService
    {
        private readonly AppDbContext _context;

        public SupplierMaterialService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SupplierMaterialListDto>> GetBySupplierAsync(int supplierId)
        {
            var data = await _context.SupplierMaterials
                .Where(x => x.SupplierId == supplierId && !x.IsDeleted)
                .OrderByDescending(x => x.SupplierMaterialId)
                .Select(x => new SupplierMaterialListDto
                {
                    SupplierMaterialId = x.SupplierMaterialId,

                    MaterialId = x.MaterialId,

                    MaterialCode = _context.Materials
                        .Where(m => m.material_id == x.MaterialId)
                        .Select(m => m.material_code)
                        .FirstOrDefault() ?? "",

                    MaterialName = _context.Materials
                        .Where(m => m.material_id == x.MaterialId)
                        .Select(m => m.material_name)
                        .FirstOrDefault() ?? "",

                    ManufacturerId = x.ManufacturerId,

                    ManufacturerName = _context.Manufacturers
                        .Where(m => m.ManufacturerId == x.ManufacturerId)
                        .Select(m => m.ManufacturerName)
                        .FirstOrDefault(),

                    IsPreferred = x.IsPreferred,
                    Remarks = x.Remarks
                })
                .ToListAsync();

            return data;
        }

        public async Task<int> CreateAsync(CreateSupplierMaterialDto dto)
        {
            if (dto.SupplierId <= 0)
                throw new Exception("Supplier is required.");

            if (dto.MaterialId <= 0)
                throw new Exception("Material is required.");

            var supplierExists = await _context.Suppliers
                .AnyAsync(x => x.SupplierId == dto.SupplierId && !x.IsDeleted);

            if (!supplierExists)
                throw new Exception("Supplier not found.");

            var materialExists = await _context.Materials
                .AnyAsync(x => x.material_id == dto.MaterialId && !x.is_deleted);

            if (!materialExists)
                throw new Exception("Material not found.");

            if (dto.ManufacturerId.HasValue)
            {
                var manufacturerExists = await _context.Manufacturers
                    .AnyAsync(x =>
                        x.ManufacturerId == dto.ManufacturerId.Value &&
                        !x.IsDeleted);

                if (!manufacturerExists)
                    throw new Exception("Manufacturer not found.");
            }

            var existingMapping = await _context.SupplierMaterials
                .FirstOrDefaultAsync(x =>
                    x.SupplierId == dto.SupplierId &&
                    x.MaterialId == dto.MaterialId &&
                    x.ManufacturerId == dto.ManufacturerId);

            if (existingMapping != null)
            {
                if (!existingMapping.IsDeleted)
                    throw new Exception("This supplier material mapping already exists.");

                existingMapping.IsDeleted = false;
                existingMapping.IsActive = true;
                existingMapping.IsPreferred = dto.IsPreferred;
                existingMapping.Remarks = dto.Remarks;
                existingMapping.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return existingMapping.SupplierMaterialId;
            }

            var mapping = new SupplierMaterial
            {
                SupplierId = dto.SupplierId,
                MaterialId = dto.MaterialId,
                ManufacturerId = dto.ManufacturerId,

                IsPreferred = dto.IsPreferred,
                Remarks = dto.Remarks,

                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SupplierMaterials.Add(mapping);
            await _context.SaveChangesAsync();

            return mapping.SupplierMaterialId;
        }

        public async Task<bool> DeleteAsync(int supplierMaterialId)
        {
            var mapping = await _context.SupplierMaterials
                .FirstOrDefaultAsync(x =>
                    x.SupplierMaterialId == supplierMaterialId &&
                    !x.IsDeleted);

            if (mapping == null)
                return false;

            mapping.IsDeleted = true;
            mapping.IsActive = false;
            mapping.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}