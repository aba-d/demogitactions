using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

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

    // 🔴 High severity vulnerability: SQL Injection
    [HttpGet("insecure")]
        public IActionResult InsecureQuery(string userInput)
        {
            // ❌ Vulnerable: SQL injection style string concatenation
            string query = $"SELECT * FROM Users WHERE Name = '{userInput}'";
            return Ok("Insecure query: " + query);
        }

    // 🟡 Medium severity vulnerability: Hardcoded secret
    [HttpGet("secret")]
    public IActionResult HardcodedSecret()
    {
        // ❌ Vulnerable: hardcoded secret
        string apiKey = "SuperSecretKey123!";
        return Ok("Using API key: " + apiKey);
    }
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
