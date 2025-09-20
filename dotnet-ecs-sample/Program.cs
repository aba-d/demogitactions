public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder);
        var app = builder.Build();
        Configure(app);
        app.Run();
    }

    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
    }

    public static void Configure(WebApplication app)
    {
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok("Healthy"));
    }
}
