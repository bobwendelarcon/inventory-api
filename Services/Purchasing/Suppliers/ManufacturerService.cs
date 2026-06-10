using inventory_api.Data;
using inventory_api.DTOs.Purchasing.Suppliers.Manufacturers;
using inventory_api.Models.Purchasing.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.Suppliers
{
    public class ManufacturerService
    {
        private readonly AppDbContext _context;

        public ManufacturerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ManufacturerLookupDto>> LookupAsync(string? search)
        {
            var query = _context.Manufacturers
                .Where(x => !x.IsDeleted && x.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.ManufacturerName.Contains(search));
            }

            return await query
                .OrderBy(x => x.ManufacturerName)
                .Take(20)
                .Select(x => new ManufacturerLookupDto
                {
                    ManufacturerId = x.ManufacturerId,
                    ManufacturerName = x.ManufacturerName,
                    AccreditationStatus = x.AccreditationStatus
                })
                .ToListAsync();
        }

        public async Task<int> CreateAsync(CreateManufacturerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ManufacturerName))
                throw new Exception("Manufacturer name is required.");

            var manufacturerName = dto.ManufacturerName.Trim();

            var exists = await _context.Manufacturers
                .AnyAsync(x =>
                    !x.IsDeleted &&
                    x.ManufacturerName == manufacturerName);

            if (exists)
                throw new Exception("Manufacturer already exists.");

            var manufacturer = new Manufacturer
            {
                ManufacturerName = manufacturerName,
                AccreditationStatus = string.IsNullOrWhiteSpace(dto.AccreditationStatus)
                    ? "For Evaluation"
                    : dto.AccreditationStatus,

                AccreditationDate = dto.AccreditationDate,
                AccreditationExpiry = dto.AccreditationExpiry,
                CoaRequired = string.IsNullOrWhiteSpace(dto.CoaRequired)
                    ? "N/A"
                    : dto.CoaRequired,

                Remarks = dto.Remarks,

                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Manufacturers.Add(manufacturer);
            await _context.SaveChangesAsync();

            return manufacturer.ManufacturerId;
        }


        public async Task<ManufacturerLookupDto?> GetByIdAsync(int id)
        {
            var manufacturer = await _context.Manufacturers
                .Where(x => x.ManufacturerId == id && !x.IsDeleted)
                .Select(x => new ManufacturerLookupDto
                {
                    ManufacturerId = x.ManufacturerId,
                    ManufacturerName = x.ManufacturerName,
                    AccreditationStatus = x.AccreditationStatus
                })
                .FirstOrDefaultAsync();

            return manufacturer;
        }

        public async Task<ManufacturerDetailsDto?> GetDetailsByIdAsync(int id)
        {
            return await _context.Manufacturers
                .Where(x => x.ManufacturerId == id && !x.IsDeleted)
                .Select(x => new ManufacturerDetailsDto
                {
                    ManufacturerId = x.ManufacturerId,
                    ManufacturerName = x.ManufacturerName,
                    AccreditationStatus = x.AccreditationStatus,
                    AccreditationDate = x.AccreditationDate,
                    AccreditationExpiry = x.AccreditationExpiry,
                    CoaRequired = x.CoaRequired,
                    Remarks = x.Remarks
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(int id, UpdateManufacturerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ManufacturerName))
                throw new Exception("Manufacturer name is required.");

            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(x => x.ManufacturerId == id && !x.IsDeleted);

            if (manufacturer == null)
                return false;

            manufacturer.ManufacturerName = dto.ManufacturerName.Trim();
            manufacturer.AccreditationStatus = string.IsNullOrWhiteSpace(dto.AccreditationStatus)
                ? "For Evaluation"
                : dto.AccreditationStatus;

            manufacturer.AccreditationDate = dto.AccreditationDate;
            manufacturer.AccreditationExpiry = dto.AccreditationExpiry;

            manufacturer.CoaRequired = string.IsNullOrWhiteSpace(dto.CoaRequired)
                ? "N/A"
                : dto.CoaRequired;

            manufacturer.Remarks = dto.Remarks;
            manufacturer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}