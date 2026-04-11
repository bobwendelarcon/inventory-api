using inventory_api.Data;
using inventory_api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class InventoryDisplayService
    {
        private readonly AppDbContext _context;

        public InventoryDisplayService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryDisplayDto>> GetAllAsync()
        {
            var result = await (
                from lot in _context.ProductLotNumbers
                join product in _context.Products
                    on lot.product_id equals product.product_id into productJoin
                from product in productJoin.DefaultIfEmpty()

                join branch in _context.Branches
                    on lot.branch_id equals branch.branch_id into branchJoin
                from branch in branchJoin.DefaultIfEmpty()

                select new InventoryDisplayDto
                {
                    product_id = lot.product_id,
                    description = product != null ? (product.product_description ?? "") : "",
                    uom = product != null ? (product.uom ?? "") : "",
                    pack_qty = product != null ? (int)(product.pack_qty ?? 0) : 0,
                    pack_uom = product != null ? (product.pack_uom ?? "") : "",
                    lot_no = lot.lot_no ?? "",
                    warehouse = branch != null ? (branch.branch_name ?? "") : lot.branch_id,
                    qty = (int)lot.quantity,
                    date = lot.created_at.ToString("yyyy-MM-dd"),
                    manufacturing_date = lot.manufacturing_date.HasValue
                        ? lot.manufacturing_date.Value.ToString("yyyy-MM-dd")
                        : "",
                    expiration_date = lot.expiration_date.HasValue
                        ? lot.expiration_date.Value.ToString("yyyy-MM-dd")
                        : ""
                }
            ).ToListAsync();

            return result;
        }
    }
}