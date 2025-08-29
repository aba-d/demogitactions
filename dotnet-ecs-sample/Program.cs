
// var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddControllers();
// var app = builder.Build();
// app.MapControllers();
// app.Run();


var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Map controllers (your existing APIs)
app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
