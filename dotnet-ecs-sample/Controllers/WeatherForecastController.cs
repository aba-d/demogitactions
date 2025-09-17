using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
// using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace dotnet_ecs_sample.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        });
    }

    // [HttpGet("sql-injection")]
    // public IActionResult SqlInjection(string userInput)
    // {
    //     // ❌ Vulnerable: concatenating user input into SQL command
    //     var cmd = new SqlCommand(
    //         "SELECT * FROM Users WHERE Name = '" + userInput + "'"
    //     );

    //     return Ok("Executed insecure SQL command with input: " + userInput);
    // }

    // // ⚠️ Medium severity: Command Injection
    // [HttpGet("command-injection")]
    // public IActionResult CommandInjection(string userInput)
    // {
    //     // ❌ Vulnerable: passing user input into system command
    //     Process.Start("bash", "-c \"echo " + userInput + "\"");

    //     return Ok("Executed insecure command with input: " + userInput);
    // }
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
