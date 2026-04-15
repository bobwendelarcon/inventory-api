using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class PartnerService
    {
        private readonly AppDbContext _context;

        public PartnerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            var partners = await _context.Partners
                .OrderBy(x => x.partner_name)
                .ToListAsync();

            return partners.Select(x => new Dictionary<string, object>
            {
                { "partner_id", x.partner_id },
                { "partner_name", x.partner_name },
                { "address", x.address ?? "" },
                { "contact_no", x.contact ?? "" },
                { "partner_type", x.partner_type ?? "" },
                { "is_deleted", x.is_deleted },
                { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
                { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
            }).ToList();
        }

        public async Task AddAsync(CreatePartnerDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.partner_id))
                throw new Exception("partner_id is required.");

            if (string.IsNullOrWhiteSpace(dto.partner_name))
                throw new Exception("partner_name is required.");

            bool exists = await _context.Partners.AnyAsync(x => x.partner_id == dto.partner_id);

            if (exists)
                throw new Exception("Partner already exists.");

            var partner = new Partner
            {
                partner_id = dto.partner_id,
                partner_name = dto.partner_name,
                address = dto.address,
                contact = dto.contact,
                partner_type = dto.partner_type,
                is_deleted = dto.is_deleted,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            _context.Partners.Add(partner);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(string id, CreatePartnerDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(id))
                throw new Exception("partner_id is required.");

            if (string.IsNullOrWhiteSpace(dto.partner_name))
                throw new Exception("partner_name is required.");

            if (string.IsNullOrWhiteSpace(dto.partner_type))
                throw new Exception("partner_type is required.");

            if (dto.partner_type != "SUPPLIER" && dto.partner_type != "CUSTOMER")
                throw new Exception("partner_type must be SUPPLIER or CUSTOMER.");

            var partner = await _context.Partners.FirstOrDefaultAsync(x => x.partner_id == id);

            if (partner == null)
                throw new Exception("Partner not found.");

            partner.partner_name = dto.partner_name;
            partner.address = dto.address;
            partner.contact = dto.contact;
            partner.partner_type = dto.partner_type;
            partner.is_deleted = dto.is_deleted;
            partner.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task ResetAllAsync()
        {
            var partners = await _context.Partners.ToListAsync();

            _context.Partners.RemoveRange(partners);
            await _context.SaveChangesAsync();
        }
    }
}