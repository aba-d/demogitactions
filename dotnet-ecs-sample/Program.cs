var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// ✅ Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI();

// Map controllers
app.MapControllers();

// Add a simple health endpoint for ECS
app.MapGet("/health", () => Results.Ok("Healthy"));

// Run the app
app.Run();
