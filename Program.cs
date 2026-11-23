using inventory_api.Data;
using inventory_api.Services;
using inventory_api.Services.Inventory;
using inventory_api.Services.Laboratory;
using inventory_api.Services.Manufacturing.Materials;
using inventory_api.Services.Purchasing;
using inventory_api.Services.Purchasing.Canvassing;
using inventory_api.Services.Purchasing.FinalReceiving;
using inventory_api.Services.Purchasing.IncomingReceiving;
using inventory_api.Services.Purchasing.PurchaseOrders;
using inventory_api.Services.Purchasing.QaQcReceiving;
using inventory_api.Services.Purchasing.QcInspections;
using inventory_api.Services.Purchasing.RawMaterialProcessing;
using inventory_api.Services.Purchasing.ReceivingReports;
using inventory_api.Services.Purchasing.SupplierEvaluations;
using inventory_api.Services.Purchasing.Suppliers;
using inventory_api.Services.Reports.PurchasingLeadTime;
using inventory_api.Services.Reports.RawMaterials;
using inventory_api.Services.Reports.TimeInMotion;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// JWT Authentication Configuration
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer missing.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT Audience missing.");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key missing.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.FromMinutes(1)
            };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                var tokenRole = context.Principal?
                    .FindFirstValue(ClaimTypes.Role);

                if (string.IsNullOrWhiteSpace(userId) ||
                    string.IsNullOrWhiteSpace(tokenRole))
                {
                    context.Fail("Invalid user identity.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();

                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.user_id == userId &&
                        !u.is_deleted);

                if (user == null)
                {
                    context.Fail("User account is inactive.");
                    return;
                }

                if (!string.Equals(
                    user.role_name,
                    tokenRole,
                    StringComparison.OrdinalIgnoreCase))
                {
                    context.Fail("User role has changed.");
                    return;
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter your JWT token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

// Register AppDbContext only once
//builder.Services.AddDbContext<AppDbContext>(options =>
//{
//    options.UseMySql(
//        connectionString,
//        ServerVersion.AutoDetect(connectionString),
//        mysqlOptions =>
//        {
//            mysqlOptions.EnableRetryOnFailure(
//                maxRetryCount: 5,
//                maxRetryDelay: TimeSpan.FromSeconds(10),
//                errorNumbersToAdd: null);
//        });
//});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

// Core services
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<InventoryTransactionService>();
builder.Services.AddScoped<BranchesService>();
builder.Services.AddScoped<ProductLotNumberService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<MrsPermissionService>();
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
builder.Services.AddScoped<IncomingReceivingService>();

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
builder.Services.AddScoped<
    QaQcReceivingInspectionService>();


// Supplier performance evaluation
builder.Services.AddScoped<SupplierEvaluationScoringService>();
builder.Services.AddScoped<SupplierEvaluationGenerationService>();
builder.Services.AddScoped<SupplierEvaluationService>();

//raw mats out
builder.Services.AddScoped<
    MaterialRequisitionService>();

//rmw processing
builder.Services.AddScoped<RmwProcessingService>();
//RR final commit
builder.Services.AddScoped<FinalReceivingService>();

//report raw material
builder.Services.AddScoped<TimeInMotionService>();
builder.Services.AddScoped<RawMaterialReportService>();

//Purchasing report
builder.Services
    .AddScoped<PurchasingLeadTimeService>();

//laboratory
builder.Services.AddScoped<LaboratoryInventoryService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseStaticFiles();

// JWT Authentication
app.UseAuthentication();

// Check user permissions
app.UseAuthorization();

app.MapControllers();

app.Run();