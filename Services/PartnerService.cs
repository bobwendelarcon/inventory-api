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

            var result = partners.Select(x =>
            {
                var agent = partners.FirstOrDefault(a => a.partner_id == x.agent_id);

                return new Dictionary<string, object>
        {
            { "partner_id", x.partner_id },
            { "partner_name", x.partner_name },
            { "address", x.address ?? "" },
            { "contact_no", x.contact ?? "" },
            { "partner_type", x.partner_type ?? "" },
            { "region", x.region ?? "" },

            { "agent_id", x.agent_id ?? "" },
            { "agent_name", agent?.partner_name ?? "" },

            { "is_deleted", x.is_deleted },
            { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
            { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
        };
            }).ToList();

            return result;
        }

        private async Task<string> GeneratePartnerIdAsync(string partnerType)
        {
            string prefix = partnerType switch
            {
                "CUSTOMER" => "C",
                "SUPPLIER" => "S",
                "AGENT" => "A",
                _ => throw new Exception("Invalid partner type.")
            };

            var latest = await _context.Partners
                .Where(x => x.partner_id.StartsWith(prefix))
                .OrderByDescending(x => x.partner_id)
                .Select(x => x.partner_id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(latest))
            {
                string numberOnly = latest.Substring(1);

                if (int.TryParse(numberOnly, out int parsed))
                {
                    nextNumber = parsed + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task AddAsync(CreatePartnerDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.partner_name))
                throw new Exception("partner_name is required.");

            if (string.IsNullOrWhiteSpace(dto.partner_type))
                throw new Exception("partner_type is required.");

            if (dto.partner_type != "SUPPLIER" &&
                dto.partner_type != "CUSTOMER" &&
                dto.partner_type != "AGENT")
            {
                throw new Exception("partner_type must be SUPPLIER, CUSTOMER, or AGENT.");
            }

            if (dto.partner_type == "CUSTOMER")
            {
                if (string.IsNullOrWhiteSpace(dto.agent_id))
                    throw new Exception("Agent is required for CUSTOMER.");

                bool agentExists = await _context.Partners.AnyAsync(x =>
                    x.partner_id == dto.agent_id &&
                    x.partner_type == "AGENT");

                if (!agentExists)
                    throw new Exception("Selected Agent not found.");
            }

            string generatedId = await GeneratePartnerIdAsync(dto.partner_type);

            var partner = new Partner
            {
                partner_id = generatedId,
                partner_name = dto.partner_name,
                address = dto.address,
                contact = dto.contact,
                partner_type = dto.partner_type,
                region = dto.region,
                agent_id = dto.partner_type == "CUSTOMER"
                    ? dto.agent_id
                    : null,
                is_deleted = dto.is_deleted,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
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

            if (dto.partner_type != "SUPPLIER" &&
                dto.partner_type != "CUSTOMER" &&
                dto.partner_type != "AGENT")
            {
                throw new Exception("partner_type must be SUPPLIER, CUSTOMER, or AGENT.");
            }

            if (dto.partner_type == "CUSTOMER")
            {
                if (string.IsNullOrWhiteSpace(dto.agent_id))
                    throw new Exception("Agent is required for CUSTOMER.");

                bool agentExists = await _context.Partners.AnyAsync(x =>
                    x.partner_id == dto.agent_id &&
                    x.partner_type == "AGENT");

                if (!agentExists)
                    throw new Exception("Selected Agent not found.");
            }

            var partner = await _context.Partners
                .FirstOrDefaultAsync(x => x.partner_id == id);

            if (partner == null)
                throw new Exception("Partner not found.");

            partner.partner_name = dto.partner_name;
            partner.address = dto.address;
            partner.contact = dto.contact;
            partner.partner_type = dto.partner_type;
            partner.region = dto.region;
            partner.agent_id = dto.partner_type == "CUSTOMER"
                ? dto.agent_id
                : null;
            partner.is_deleted = dto.is_deleted;
            partner.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task ResetAllAsync()
        {
            var partners = await _context.Partners.ToListAsync();

            _context.Partners.RemoveRange(partners);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResultDto<PartnerListDto>> GetPagedAsync(
    string? search,
    string? type,
    string? region,
    bool? isDeleted,
    string? agentId,
    int page = 1,
    int pageSize = 50,
    string sort = "partner_id_asc")
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;
            pageSize = pageSize > 100 ? 100 : pageSize;

            var query =
                from p in _context.Partners.AsNoTracking()
                join a in _context.Partners.AsNoTracking()
                    on p.agent_id equals a.partner_id into agentJoin
                from agent in agentJoin.DefaultIfEmpty()
                select new
                {
                    p.partner_id,
                    p.partner_name,
                    p.address,
                    p.contact,
                    p.partner_type,
                    p.region,
                    p.agent_id,
                    agent_name = agent != null ? agent.partner_name : "",
                    p.is_deleted,
                    p.created_at,
                    p.updated_at
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();

                query = query.Where(x =>
                    x.partner_id.Contains(s) ||
                    x.partner_name.Contains(s) ||
                    (x.address ?? "").Contains(s) ||
                    (x.contact ?? "").Contains(s) ||
                    (x.agent_name ?? "").Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(x => x.partner_type == type);
            }

            if (!string.IsNullOrWhiteSpace(region))
            {
                query = query.Where(x => x.region == region);
            }

            if (!string.IsNullOrWhiteSpace(agentId))
            {
                query = query.Where(x => x.agent_id == agentId);
            }

            if (isDeleted.HasValue)
            {
                query = query.Where(x => x.is_deleted == isDeleted.Value);
            }

            query = sort == "partner_id_desc"
                ? query.OrderByDescending(x => x.partner_id)
                : query.OrderBy(x => x.partner_id);

            int totalRecords = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PartnerListDto
                {
                    partner_id = x.partner_id,
                    partner_name = x.partner_name,
                    address = x.address ?? "",
                    contact_no = x.contact ?? "",
                    partner_type = x.partner_type ?? "",
                    region = x.region ?? "",
                    agent_id = x.agent_id ?? "",
                    agent_name = x.agent_name ?? "",
                    is_deleted = x.is_deleted,
                    created_at = x.created_at.ToString("yyyy-MM-dd HH:mm:ss"),
                    updated_at = x.updated_at.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToListAsync();

            return new PagedResultDto<PartnerListDto>
            {
                page = page,
                pageSize = pageSize,
                totalRecords = totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                data = data
            };
        }
    }
}