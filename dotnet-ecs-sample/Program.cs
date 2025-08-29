var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

var app = builder.Build();

// Map controllers
app.MapControllers();

// Add a simple health endpoint for ECS
app.MapGet("/health", () => Results.Ok("Healthy"));

// Run the app
app.Run();
