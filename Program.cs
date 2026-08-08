using inventory_api.Data;
using inventory_api.Services;
using inventory_api.Services.Inventory;
using inventory_api.Services.Manufacturing.Materials;
using inventory_api.Services.Purchasing;
using inventory_api.Services.Purchasing.Canvassing;
using inventory_api.Services.Purchasing.PurchaseOrders;
using inventory_api.Services.Purchasing.QcInspections;
using inventory_api.Services.Purchasing.ReceivingReports;
using inventory_api.Services.Purchasing.SupplierEvaluations;
using inventory_api.Services.Purchasing.Suppliers;

using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

// Register AppDbContext only once
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

// Core services
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
builder.Services.AddScoped<RawMaterialInventoryService>();

// Manufacturing
builder.Services.AddScoped<MaterialCategoryService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<MaterialSubCategoryService>();

// Purchasing
builder.Services.AddScoped<MprfService>();

// Suppliers
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<ManufacturerService>();
builder.Services.AddScoped<SupplierMaterialService>();
builder.Services.AddScoped<SupplierManufacturerService>();

// Canvassing
builder.Services.AddScoped<CanvassingService>();

// Purchase orders
builder.Services.AddScoped<PurchaseOrderService>();

// Receiving
builder.Services.AddScoped<ReceivingReportService>();

// QA/QC
builder.Services.AddScoped<QcInspectionService>();


// Supplier performance evaluation
builder.Services.AddScoped<SupplierEvaluationScoringService>();
builder.Services.AddScoped<SupplierEvaluationGenerationService>();
builder.Services.AddScoped<SupplierEvaluationService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();

app.Run();