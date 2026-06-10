using inventory_api.Data;
using inventory_api.DTOs.Purchasing.Suppliers;
using inventory_api.Models.Purchasing.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.Suppliers
{
    public class SupplierService
    {
        private readonly AppDbContext _context;

        public SupplierService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetPagedAsync(
            string? search,
            string? status,
            string? supplierType,
            int page,
            int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = _context.Suppliers
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.SupplierCode.Contains(search) ||
                    x.SupplierName.Contains(search) ||
                    (x.ContactPerson != null && x.ContactPerson.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                bool isActive = status == "Active";
                query = query.Where(x => x.IsActive == isActive);
            }

            if (!string.IsNullOrWhiteSpace(supplierType) && supplierType != "All")
            {
                query = query.Where(x => x.SupplierType == supplierType);
            }

            var totalRecords = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.SupplierId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SupplierListDto
                {
                    SupplierId = x.SupplierId,
                    SupplierCode = x.SupplierCode,
                    SupplierName = x.SupplierName,
                    SupplierType = x.SupplierType,
                    ContactPerson = x.ContactPerson,
                    PaymentTerms = x.PaymentTerms,
                    LeadTimeDays = x.LeadTimeDays,
                    IsPreferred = x.IsPreferred,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return new
            {
                totalRecords,
                page,
                pageSize,
                data
            };
        }

        public async Task<SupplierDetailsDto?> GetByIdAsync(int id)
        {
            return await _context.Suppliers
                .Where(x => x.SupplierId == id && !x.IsDeleted)
                .Select(x => new SupplierDetailsDto
                {
                    SupplierId = x.SupplierId,
                    SupplierCode = x.SupplierCode,
                    SupplierName = x.SupplierName,
                    SupplierType = x.SupplierType,

                    ContactPerson = x.ContactPerson,
                    ContactNumber = x.ContactNumber,
                    EmailAddress = x.EmailAddress,
                    Address = x.Address,

                    PaymentTerms = x.PaymentTerms,
                    LeadTimeDays = x.LeadTimeDays,
                    Currency = x.Currency,

                    IsPreferred = x.IsPreferred,
                    Remarks = x.Remarks,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreateAsync(CreateSupplierDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SupplierName))
                throw new Exception("Supplier name is required.");

            if (string.IsNullOrWhiteSpace(dto.SupplierType))
                throw new Exception("Supplier type is required.");

            var supplier = new Supplier
            {
                SupplierCode = await GenerateSupplierCodeAsync(),
                SupplierName = dto.SupplierName.Trim(),
                SupplierType = dto.SupplierType.Trim(),

                ContactPerson = dto.ContactPerson,
                ContactNumber = dto.ContactNumber,
                EmailAddress = dto.EmailAddress,
                Address = dto.Address,

                PaymentTerms = dto.PaymentTerms,
                LeadTimeDays = dto.LeadTimeDays,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "PHP" : dto.Currency,

                IsPreferred = dto.IsPreferred,
                Remarks = dto.Remarks,

                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            if (supplier.SupplierType.Equals("Manufacturer", StringComparison.OrdinalIgnoreCase))
            {
                var existingManufacturer = await _context.Manufacturers
                    .FirstOrDefaultAsync(x =>
                        x.ManufacturerName == supplier.SupplierName &&
                        !x.IsDeleted);

                int manufacturerId;

                if (existingManufacturer == null)
                {
                    var manufacturer = new Manufacturer
                    {
                        ManufacturerName = supplier.SupplierName,
                        AccreditationStatus = "For Evaluation",
                        CoaRequired = "N/A",
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Manufacturers.Add(manufacturer);
                    await _context.SaveChangesAsync();

                    manufacturerId = manufacturer.ManufacturerId;
                }
                else
                {
                    manufacturerId = existingManufacturer.ManufacturerId;
                }

                var alreadyLinked = await _context.SupplierManufacturers
                    .AnyAsync(x =>
                        x.SupplierId == supplier.SupplierId &&
                        x.ManufacturerId == manufacturerId &&
                        !x.IsDeleted);

                if (!alreadyLinked)
                {
                    var link = new SupplierManufacturer
                    {
                        SupplierId = supplier.SupplierId,
                        ManufacturerId = manufacturerId,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.SupplierManufacturers.Add(link);
                    await _context.SaveChangesAsync();
                }
            }

            return supplier.SupplierId;
        }

        public async Task<bool> UpdateAsync(int id, UpdateSupplierDto dto)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.SupplierId == id && !x.IsDeleted);

            if (supplier == null)
                return false;

            if (string.IsNullOrWhiteSpace(dto.SupplierName))
                throw new Exception("Supplier name is required.");

            if (string.IsNullOrWhiteSpace(dto.SupplierType))
                throw new Exception("Supplier type is required.");

            supplier.SupplierName = dto.SupplierName.Trim();
            supplier.SupplierType = dto.SupplierType.Trim();

            supplier.ContactPerson = dto.ContactPerson;
            supplier.ContactNumber = dto.ContactNumber;
            supplier.EmailAddress = dto.EmailAddress;
            supplier.Address = dto.Address;

            supplier.PaymentTerms = dto.PaymentTerms;
            supplier.LeadTimeDays = dto.LeadTimeDays;
            supplier.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "PHP" : dto.Currency;

            supplier.IsPreferred = dto.IsPreferred;
            supplier.Remarks = dto.Remarks;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.SupplierId == id && !x.IsDeleted);

            if (supplier == null)
                return false;

            supplier.IsDeleted = true;
            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> GenerateSupplierCodeAsync()
        {
            var codes = await _context.Suppliers
                .Select(x => x.SupplierCode)
                .ToListAsync();

            var maxNo = codes
                .Select(x =>
                {
                    if (string.IsNullOrWhiteSpace(x))
                        return 0;

                    var clean = x.Replace("SUP-", "");

                    return int.TryParse(clean, out var n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"SUP-{maxNo + 1:D4}";
        }
    }
}