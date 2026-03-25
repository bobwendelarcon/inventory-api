using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_KEY");

if (string.IsNullOrEmpty(firebaseJson))
{
    throw new Exception("FIREBASE_KEY environment variable is missing.");
}

if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromJson(firebaseJson)
    });
}

builder.Services.AddSingleton(provider =>
{
    string projectId = builder.Configuration["Firebase:ProjectId"]!;
    return FirestoreDb.Create(projectId);
});

builder.Services.AddScoped<inventory_api.Services.CategoryService>();
builder.Services.AddScoped<inventory_api.Services.ProductService>();
builder.Services.AddScoped<inventory_api.Services.InventoryTransactionService>();
builder.Services.AddScoped<inventory_api.Services.BranchesService>();
builder.Services.AddScoped<inventory_api.Services.ProductLotNumberService>();
builder.Services.AddScoped<inventory_api.Services.UserService>();
builder.Services.AddScoped<inventory_api.Services.PartnerService>();
builder.Services.AddScoped<inventory_api.Services.InventoryDisplayService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();