var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application services
builder.Services.AddScoped<AspNetCoreWebApi.Repositories.IProductRepository, AspNetCoreWebApi.Repositories.InMemoryProductRepository>();
builder.Services.AddScoped<AspNetCoreWebApi.Services.IInventoryService, AspNetCoreWebApi.Services.InventoryService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
