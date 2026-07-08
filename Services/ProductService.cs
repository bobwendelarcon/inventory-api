using ClosedXML.Excel;
using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetAllAsync(
           int page = 1,
           int pageSize = 50,
           string? search = null,
           string? categoryId = null,
           bool? status = null,
           string? source = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();

                query = query.Where(x =>
                    x.product_id.ToLower().Contains(keyword) ||
                    x.product_name.ToLower().Contains(keyword) ||
                    (x.product_sku ?? "").ToLower().Contains(keyword) ||
                    (x.product_description ?? "").ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(categoryId))
                query = query.Where(x => x.catg_id == categoryId);

            if (status.HasValue)
                query = query.Where(x => x.is_deleted == status.Value);

            if (!string.IsNullOrWhiteSpace(source))
                query = query.Where(x => x.product_source == source);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.product_name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new Dictionary<string, object>
                {
            { "product_id", x.product_id },
            { "product_sku", x.product_sku ?? "" },
            { "product_name", x.product_name },
            { "product_description", x.product_description ?? "" },
            { "product_price", x.product_price },
            { "uom", x.uom ?? "" },
            { "pack_uom", x.pack_uom ?? "" },
            { "pack_qty", x.pack_qty ?? 0 },
            { "stock_level", x.stock_level },
            { "catg_id", x.catg_id ?? "" },
            { "is_deleted", x.is_deleted },
            { "product_source", x.product_source ?? "OWN" },
            { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
            { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
                })
                .ToListAsync();

            return new
            {
                items,
                page,
                pageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
            };
        }
        public async Task<Dictionary<string, object>?> GetByBarcodeAsync(string product_sku)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.product_sku == product_sku && !x.is_deleted);

            if (product == null)
                return null;

            return new Dictionary<string, object>
            {
                { "product_id", product.product_id },
                { "product_sku", product.product_sku ?? "" },
                { "product_name", product.product_name },
                { "product_description", product.product_description ?? "" },
                { "product_price", product.product_price },
                { "uom", product.uom ?? "" },
                { "pack_uom", product.pack_uom ?? "" },
                { "pack_qty", product.pack_qty },
                { "stock_level", product.stock_level },
                { "catg_id", product.catg_id ?? "" },
                { "is_deleted", product.is_deleted },

                { "created_at", product.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
                { "updated_at", product.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }

        //auto generated
        public async Task AddAsync(CreateProductDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.product_name))
                throw new Exception("product_name is required.");

            string newProductId = await GenerateProductIdAsync();

            bool exists = await _context.Products.AnyAsync(x => x.product_id == newProductId);

            if (exists)
                throw new Exception("Generated Product ID already exists. Please try again.");

            var product = new Product
            {
                product_id = newProductId,
                product_sku = dto.product_sku,
                product_name = dto.product_name.Trim(),
                product_description = dto.product_description,
                product_price = dto.product_price,
                uom = dto.uom,
                product_source = string.IsNullOrWhiteSpace(dto.product_source)
                    ? "OWN"
                    : dto.product_source,
                pack_uom = dto.pack_uom,
                pack_qty = dto.pack_qty,
                stock_level = dto.stock_level,
                catg_id = dto.catg_id,
                is_deleted = false,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }
        private async Task<string> GenerateProductIdAsync()
        {
            var lastProduct = await _context.Products
                .Where(x => x.product_id.StartsWith("prod"))
                .OrderByDescending(x => x.product_id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastProduct != null)
            {
                string numberPart = lastProduct.product_id.Replace("prod", "");

                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"prod{nextNumber:D4}";
        }



        //    public async Task AddAsync(CreateProductDto dto)
        //    {
        //        if (dto == null)
        //            throw new Exception("Invalid request.");

        //        if (string.IsNullOrWhiteSpace(dto.product_id))
        //            throw new Exception("product_id is required.");

        //        if (string.IsNullOrWhiteSpace(dto.product_name))
        //            throw new Exception("product_name is required.");

        //        bool exists = await _context.Products.AnyAsync(x => x.product_id == dto.product_id);

        //        if (exists)
        //            throw new Exception("Product already exists.");

        //        var product = new Product
        //        {
        //            product_id = dto.product_id,
        //            product_sku = dto.product_sku,
        //            product_name = dto.product_name,
        //            product_description = dto.product_description,
        //            product_price = dto.product_price,
        //            uom = dto.uom,
        //            product_source = string.IsNullOrWhiteSpace(dto.product_source)
        //? "OWN"
        //: dto.product_source,
        //            pack_uom = dto.pack_uom,
        //            pack_qty = dto.pack_qty,
        //            stock_level = dto.stock_level,
        //            catg_id = dto.catg_id,
        //            is_deleted = false,
        //            created_at = DateTime.UtcNow,
        //            updated_at = DateTime.UtcNow
        //        };

        //        _context.Products.Add(product);
        //        await _context.SaveChangesAsync();
        //    }

        public async Task UpdateAsync(string id, CreateProductDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(id))
                throw new Exception("Product id is required.");

            if (string.IsNullOrWhiteSpace(dto.product_name))
                throw new Exception("product_name is required.");

            var product = await _context.Products.FirstOrDefaultAsync(x => x.product_id == id);

            if (product == null)
                throw new Exception("Product not found.");

            product.product_sku = dto.product_sku;
            product.product_name = dto.product_name;
            product.product_description = dto.product_description;
            product.product_price = dto.product_price;
            product.uom = dto.uom;
            product.pack_uom = dto.pack_uom;
            product.pack_qty = dto.pack_qty;
            product.product_source = string.IsNullOrWhiteSpace(dto.product_source)
    ? product.product_source ?? "OWN"
    : dto.product_source;
            product.stock_level = dto.stock_level;
            product.catg_id = dto.catg_id;
            product.is_deleted = dto.is_deleted;
            product.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(string productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.product_id == productId);

            if (product == null)
                return false;

            product.is_deleted = true;
            product.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateStatusAsync(string productId, bool isDeleted)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.product_id == productId);

            if (product == null)
                throw new Exception("Product not found.");

            product.is_deleted = isDeleted;
            product.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<object> GetProductStockLotsAsync(string productId)
        {
            var lots = await (
                from lot in _context.ProductLotNumbers
                join p in _context.Products
                    on lot.product_id equals p.product_id
                join b in _context.Branches
                    on lot.branch_id equals b.branch_id into branchJoin
                from b in branchJoin.DefaultIfEmpty()
                where lot.product_id == productId
                      && lot.quantity > 0
                      && !lot.is_deleted
                orderby b.branch_name, lot.lot_no
                select new
                {
                    lot_no = lot.lot_no,
                    quantity = lot.quantity,
                    uom = p.uom ?? "",
                    pack_qty = p.pack_qty ?? 0,
                    pack_uom = p.pack_uom ?? "",
                    location = b != null ? b.branch_name : lot.branch_id
                })
                .ToListAsync();

            var product = await (
       from p in _context.Products
       join c in _context.Categories
           on p.catg_id equals c.catg_id into catJoin
       from c in catJoin.DefaultIfEmpty()
       where p.product_id == productId
       select new
       {
           p.product_id,
           p.product_name,
           p.product_description,
           category_name = c != null ? c.catg_name : ""
       }
   ).FirstOrDefaultAsync();

            return new
            {
                canInactivate = lots.Count == 0,
                product,
                lots
            };


        }
        public async Task ResetAllAsync()
        {
            var products = await _context.Products.ToListAsync();

            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ProductLookupDto>> GetProductsLookupAsync(string? categoryId, string? search)
        {
            var query =
                from p in _context.Products
                join c in _context.Categories
                    on p.catg_id equals c.catg_id
                where !p.is_deleted
                select new
                {
                    Product = p,
                    CategoryName = c.catg_name
                };

            if (!string.IsNullOrWhiteSpace(categoryId))
                query = query.Where(x => x.Product.catg_id == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.Product.product_name.Contains(search) ||
                    x.Product.product_description.Contains(search));
            }

            return await query
                .OrderBy(x => x.Product.product_name)
                .Select(x => new ProductLookupDto
                {
                    ProductId = x.Product.product_id,
                    ProductName = x.Product.product_name,
                    ProductDescription = x.Product.product_description,

                    CategoryId = x.Product.catg_id,
                    CategoryName = x.CategoryName,

                    Uom = x.Product.uom,
                    PackUom = x.Product.pack_uom,
                    PackQty = x.Product.pack_qty
                })
                .ToListAsync();
        }

        // import module for product excel

        public async Task<object> PreviewImportAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("Excel file is required.");

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imports", "products");
            Directory.CreateDirectory(folder);

            var fileToken = $"{Guid.NewGuid()}.xlsx";
            var filePath = Path.Combine(folder, fileToken);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = new List<ProductImportPreviewDto>();

            using var workbook = new XLWorkbook(filePath);

            foreach (var sheet in workbook.Worksheets)
            {
                var sheetName = sheet.Name.Trim();

                var category = await _context.Categories
                    .FirstOrDefaultAsync(x =>
                        !x.is_deleted &&
                        x.catg_name.ToLower().Trim() == sheetName.ToLower());

                int productCount = sheet.RowsUsed()
                    .Skip(1)
                    .Count(row => !string.IsNullOrWhiteSpace(row.Cell(1).GetString()));

                result.Add(new ProductImportPreviewDto
                {
                    SheetName = sheetName,
                    CategoryExists = category != null,
                    CategoryId = category?.catg_id,
                    ProductCount = productCount
                });
            }

            return new
            {
                fileToken,
                sheets = result
            };
        }

        public async Task<object> ImportSelectedSheetsAsync(ProductImportRequestDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.FileToken))
                throw new Exception("File token is required.");

            if (dto.SelectedSheets == null || !dto.SelectedSheets.Any())
                throw new Exception("Please select at least one sheet.");

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "imports",
                "products",
                dto.FileToken
            );

            if (!File.Exists(filePath))
                throw new Exception("Uploaded Excel file not found. Please upload again.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            int importedCount = 0;
            int skippedCount = 0;

            try
            {
                using var workbook = new XLWorkbook(filePath);

                var lastProduct = await _context.Products
                    .Where(x => x.product_id.StartsWith("prod"))
                    .OrderByDescending(x => x.product_id)
                    .FirstOrDefaultAsync();

                int nextProductNumber = 1;

                if (lastProduct != null)
                {
                    var numberPart = lastProduct.product_id.Replace("prod", "");

                    if (int.TryParse(numberPart, out int lastNumber))
                        nextProductNumber = lastNumber + 1;
                }

                foreach (var selectedSheet in dto.SelectedSheets)
                {
                    var sheet = workbook.Worksheets
                        .FirstOrDefault(x => x.Name.Trim().ToLower() == selectedSheet.Trim().ToLower());

                    if (sheet == null)
                        throw new Exception($"Sheet not found: {selectedSheet}");

                    string sheetName = sheet.Name.Trim();

                    var category = await _context.Categories
                        .FirstOrDefaultAsync(x =>
                            !x.is_deleted &&
                            x.catg_name.ToLower().Trim() == sheetName.ToLower());

                    if (category == null)
                        throw new Exception($"Category not found for sheet: {sheetName}");

                    foreach (var row in sheet.RowsUsed().Skip(1))
                    {
                        string genericName = row.Cell(1).GetString().Trim();
                        string brandName = row.Cell(2).GetString().Trim();
                        string client = row.Cell(3).GetString().Trim();

                        string productName = genericName;

                        if (string.IsNullOrWhiteSpace(productName))
                        {
                            skippedCount++;
                            continue;
                        }

                        string description = brandName;

                        if (!string.IsNullOrWhiteSpace(client))
                        {
                            description = $"{brandName} ({client})";
                        }

                        bool productExists = await _context.Products.AnyAsync(x =>
                            x.catg_id == category.catg_id &&
                            x.product_name.Trim().ToLower() == productName.Trim().ToLower()
                        );

                        if (productExists)
                        {
                            skippedCount++;
                            continue;
                        }

                        string sku = row.Cell(4).GetString().Trim();
                        string uom = row.Cell(5).GetString().Trim();
                        string packUom = row.Cell(6).GetString().Trim();

                        decimal packQty = 0;
                        decimal stockLevel = 0;

                        decimal.TryParse(row.Cell(7).GetString(), out packQty);
                        decimal.TryParse(row.Cell(8).GetString(), out stockLevel);

                        string productSource = row.Cell(9).GetString().Trim();

                        if (string.IsNullOrWhiteSpace(productSource))
                            productSource = "OWN";

                        var product = new Product
                        {
                            product_id = $"prod{nextProductNumber:D4}",
                            product_sku = sku,
                            product_name = productName,
                            product_description = description,
                            product_price = 0,
                            uom = uom,
                            pack_uom = packUom,
                            pack_qty = packQty,
                            stock_level = stockLevel,
                            product_source = productSource.ToUpper(),
                            catg_id = category.catg_id,
                            is_deleted = false,
                            created_at = DateTime.UtcNow,
                            updated_at = DateTime.UtcNow
                        };

                        _context.Products.Add(product);

                        importedCount++;
                        nextProductNumber++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new
                {
                    message = "Products imported successfully.",
                    importedCount,
                    skippedCount
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


    }
}