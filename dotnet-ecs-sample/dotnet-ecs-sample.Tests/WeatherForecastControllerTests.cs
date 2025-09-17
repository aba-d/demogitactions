
using dotnet_ecs_sample.Controllers;
using System.Linq;
using Xunit;

namespace dotnet_ecs_sample.Tests
{
    public class WeatherForecastControllerTests
    {
        [Fact]
        public void Get_ReturnsFiveForecasts()
        {
            var controller = new WeatherForecastController();
            var result = controller.Get();
            Assert.Equal(5, result.Count());
        }

        [Fact]
        public void Get_ReturnsForecastsWithValidTemperature()
        {
            var controller = new WeatherForecastController();
            var result = controller.Get();
            Assert.All(result, forecast =>
                Assert.InRange(forecast.TemperatureC, -20, 55));
        }
    }
}
