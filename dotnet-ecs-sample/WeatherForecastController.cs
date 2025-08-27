
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace dotnet_ecs_sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new List<string> { "Sunny", "Cloudy", "Rainy" };
        }
    }
}
