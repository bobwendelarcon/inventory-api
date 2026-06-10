using inventory_api.Data;
using inventory_api.Services;
using inventory_api.Services.Manufacturing.Materials;
using inventory_api.Services.Purchasing;
using inventory_api.Services.Purchasing.Canvassing;
using inventory_api.Services.Purchasing.PurchaseOrders;
using inventory_api.Services.Purchasing.QcInspections;
using inventory_api.Services.Purchasing.ReceivingReports;
using inventory_api.Services.Purchasing.Suppliers;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

// Register only converted services
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<InventoryTransactionService>();
builder.Services.AddScoped<BranchesService>();
builder.Services.AddScoped<ProductLotNumberService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PartnerService>();
builder.Services.AddScoped<InventoryDisplayService>();
builder.Services.AddScoped<DailyOrderService>();
builder.Services.AddScoped<DeliveryChecklistService>();
builder.Services.AddScoped<ChecklistOutService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ProductToProduceService>();
builder.Services.AddScoped<ReturnService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ProductToProduceService>();


//manufacturing
builder.Services.AddScoped<MaterialCategoryService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<MaterialSubCategoryService>();

//purchasing
builder.Services.AddScoped<MprfService>();
//purchasing-supplier
builder.Services.AddScoped<SupplierService>();
//purchasing-manufacturer
builder.Services.AddScoped<ManufacturerService>();
//purchasing - supplier - supplier material service
builder.Services.AddScoped<SupplierMaterialService>();
// msupplier manufacturer
builder.Services.AddScoped<SupplierManufacturerService>();
//canvassing
builder.Services.AddScoped<CanvassingService>();
//PO
builder.Services.AddScoped<PurchaseOrderService>();
//receiving
builder.Services.AddScoped<ReceivingReportService>();
//qaqc
builder.Services.AddScoped<QcInspectionService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();