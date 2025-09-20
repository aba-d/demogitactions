using dotnet_ecs_sample.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace dotnet_ecs_sample.Tests
{
    public class WeatherForecastControllerTests
    {
        // --- Positive tests ---
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

        [Fact]
        public void Get_ReturnsForecastsWithFutureDates()
        {
            var controller = new WeatherForecastController();
            var result = controller.Get();
            Assert.All(result, forecast =>
                Assert.True(forecast.Date >= System.DateOnly.FromDateTime(System.DateTime.Now)));
        }

        [Fact]
        public void Get_ForecastsAreNotNull()
        {
            var controller = new WeatherForecastController();
            var result = controller.Get();
            Assert.All(result, forecast =>
                Assert.NotNull(forecast.Summary));
        }

        // --- Example negative test (edge scenario) ---
        [Fact]
        public void Get_ForecastsTemperatureOutOfRange_Fails()
        {
            var controller = new WeatherForecastController();
            var result = controller.Get();

            // This test ensures no temperature is outside expected range
            Assert.DoesNotContain(result, f => f.TemperatureC < -20 || f.TemperatureC > 55);
        }

        // SQL Injection endpoint
        [Fact]
        public void SqlInjection_ReturnsExpectedMessage()
        {
            var controller = new WeatherForecastController();
            var input = "testUser";
            var result = controller.SqlInjection(input) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Contains(input, result.Value.ToString());
        }

        // Command Injection endpoint

        [Fact]
        public void CommandInjection_ReturnsExpectedMessage()
        {
            var controller = new WeatherForecastController();
            var input = "hello";
            var result = controller.CommandInjection(input) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Contains(input, result.Value.ToString());
        }

    }
}
