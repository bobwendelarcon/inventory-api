using inventory_api.Data;
using inventory_api.DTOs.Purchasing.Suppliers.SupplierManufacturers;
using inventory_api.Models.Purchasing.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.Suppliers
{
    public class SupplierManufacturerService
    {
        private readonly AppDbContext _context;

        public SupplierManufacturerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SupplierManufacturerListDto>> GetBySupplierAsync(int supplierId)
        {
            return await _context.SupplierManufacturers
                .Where(x => x.SupplierId == supplierId && !x.IsDeleted)
                .OrderByDescending(x => x.SupplierManufacturerId)
                .Select(x => new SupplierManufacturerListDto
                {
                    SupplierManufacturerId = x.SupplierManufacturerId,
                    SupplierId = x.SupplierId,
                    ManufacturerId = x.ManufacturerId,

                    ManufacturerName = _context.Manufacturers
                        .Where(m => m.ManufacturerId == x.ManufacturerId)
                        .Select(m => m.ManufacturerName)
                        .FirstOrDefault() ?? "",

                    AccreditationStatus = _context.Manufacturers
                        .Where(m => m.ManufacturerId == x.ManufacturerId)
                        .Select(m => m.AccreditationStatus)
                        .FirstOrDefault() ?? "",

                    CoaRequired = _context.Manufacturers
                        .Where(m => m.ManufacturerId == x.ManufacturerId)
                        .Select(m => m.CoaRequired)
                        .FirstOrDefault() ?? "N/A"
                })
                .ToListAsync();
        }

        public async Task<int> CreateAsync(CreateSupplierManufacturerDto dto)
        {
            if (dto.SupplierId <= 0)
                throw new Exception("Supplier is required.");

            if (dto.ManufacturerId <= 0)
                throw new Exception("Manufacturer is required.");

            var supplierExists = await _context.Suppliers
                .AnyAsync(x => x.SupplierId == dto.SupplierId && !x.IsDeleted);

            if (!supplierExists)
                throw new Exception("Supplier not found.");

            var manufacturerExists = await _context.Manufacturers
                .AnyAsync(x => x.ManufacturerId == dto.ManufacturerId && !x.IsDeleted);

            if (!manufacturerExists)
                throw new Exception("Manufacturer not found.");

            var existingMapping = await _context.SupplierManufacturers
                .FirstOrDefaultAsync(x =>
                    x.SupplierId == dto.SupplierId &&
                    x.ManufacturerId == dto.ManufacturerId);

            if (existingMapping != null)
            {
                if (!existingMapping.IsDeleted)
                    throw new Exception("Manufacturer already linked to this supplier.");

                existingMapping.IsDeleted = false;
                existingMapping.IsActive = true;
                existingMapping.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return existingMapping.SupplierManufacturerId;
            }

            var mapping = new SupplierManufacturer
            {
                SupplierId = dto.SupplierId,
                ManufacturerId = dto.ManufacturerId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SupplierManufacturers.Add(mapping);
            await _context.SaveChangesAsync();

            return mapping.SupplierManufacturerId;
        }

        public async Task<bool> DeleteAsync(int supplierManufacturerId)
        {
            var mapping = await _context.SupplierManufacturers
                .FirstOrDefaultAsync(x =>
                    x.SupplierManufacturerId == supplierManufacturerId &&
                    !x.IsDeleted);

            if (mapping == null)
                return false;

            // Check if manufacturer is being used by supplier materials
            var usedInMaterials = await _context.SupplierMaterials
                .AnyAsync(x =>
                    x.SupplierId == mapping.SupplierId &&
                    x.ManufacturerId == mapping.ManufacturerId &&
                    !x.IsDeleted);

            if (usedInMaterials)
                throw new Exception(
                    "Cannot remove manufacturer because it is used in linked materials. Remove the linked materials first.");

            mapping.IsDeleted = true;
            mapping.IsActive = false;
            mapping.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}