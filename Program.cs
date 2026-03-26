using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string firebaseKeyPath = Environment.GetEnvironmentVariable("FIREBASE_KEY_PATH");

if (string.IsNullOrEmpty(firebaseKeyPath))
{
    firebaseKeyPath = "firebase-key.json";
}

if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseKeyPath)
    });
}

Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseKeyPath);


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

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();