using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;

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
}
// 🔴 High severity vulnerability: SQL Injection
    [HttpGet("insecure")]
    public IActionResult InsecureQuery(string city)
    {
        // ❌ Vulnerable: concatenating user input directly into SQL
        string query = "SELECT * FROM Weather WHERE City = '" + city + "'";

        using (SqlConnection connection = new SqlConnection("Server=.;Database=TestDb;Trusted_Connection=True;"))
        {
            SqlCommand command = new SqlCommand(query, connection);
            connection.Open();
            var reader = command.ExecuteReader();
            return Ok("Executed insecure query for city: " + city);
        }
    }

    // 🟡 Medium severity vulnerability: Hardcoded secret
    [HttpGet("secret")]
    public IActionResult HardcodedSecret()
    {
        // ❌ Vulnerable: hardcoded secret
        string apiKey = "SuperSecretKey123!";
        return Ok("Using API key: " + apiKey);
    }

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
