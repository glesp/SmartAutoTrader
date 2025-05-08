using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAutoTrader.API.Enums;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Repositories;
using SmartAutoTrader.API.Services;
using Xunit;

namespace SmartAutoTrader.Tests.Services
{
    public class OpenRouterRecommendationServiceTests
    {
        private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<ILogger<OpenRouterRecommendationService>> _loggerMock = new();
        private readonly HttpClient _httpClient = new();
        private readonly OpenRouterRecommendationService _service;

        public OpenRouterRecommendationServiceTests()
        {
            _service = new OpenRouterRecommendationService(
                _vehicleRepoMock.Object,
                _configMock.Object,
                _loggerMock.Object,
                _httpClient);
        }

        [Fact]
        public async Task GetRecommendationsAsync_ReturnsFilteredVehicles_WhenMatchesExist()
        {
            // Arrange
            var parameters = new RecommendationParameters
            {
                PreferredMakes = new List<string> { "Toyota" },
                MaxResults = 3
            };

            var vehicles = new List<Vehicle>
            {
                new Vehicle { Make = "Toyota", Model = "Camry", DateListed = DateTime.UtcNow.AddDays(-1), Status = VehicleStatus.Available },
                new Vehicle { Make = "Toyota", Model = "Corolla", DateListed = DateTime.UtcNow.AddDays(-2), Status = VehicleStatus.Available },
                new Vehicle { Make = "Toyota", Model = "RAV4", DateListed = DateTime.UtcNow, Status = VehicleStatus.Available },
                new Vehicle { Make = "Honda", Model = "Civic", DateListed = DateTime.UtcNow, Status = VehicleStatus.Available }
            };

            _vehicleRepoMock.Setup(repo => repo.SearchAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles.Where(v => v.Make == "Toyota").ToList());

            // Act
            var result = await _service.GetRecommendationsAsync(1, parameters);

            // Assert
            Assert.Equal(3, result.Count());
            Assert.All(result, v => Assert.Equal("Toyota", v.Make));
        }

        [Fact]
        public async Task GetRecommendationsAsync_ReturnsEmptyList_WhenNoMatchesExist()
        {
            // Arrange
            var parameters = new RecommendationParameters
            {
                PreferredMakes = new List<string> { "Ferrari" }
            };

            _vehicleRepoMock.Setup(repo => repo.SearchAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(new List<Vehicle>());

            // Act
            var result = await _service.GetRecommendationsAsync(1, parameters);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRecommendationsAsync_HandlesExceptions_Gracefully()
        {
            // Arrange
            var parameters = new RecommendationParameters();

            _vehicleRepoMock.Setup(repo => repo.SearchAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.GetRecommendationsAsync(1, parameters);

            // Assert
            Assert.Empty(result);
            _loggerMock.Verify(x =>
                    x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, _) => true),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

        }

        [Fact]
        public async Task GetRecommendationsAsync_RespectsMaxResults_Limit()
        {
            // Arrange
            var parameters = new RecommendationParameters { MaxResults = 2 };

            var vehicles = Enumerable.Range(1, 10)
                .Select(i => new Vehicle { Make = "Toyota", Model = $"Model{i}", DateListed = DateTime.UtcNow.AddDays(-i), Status = VehicleStatus.Available })
                .ToList();

            _vehicleRepoMock.Setup(repo => repo.SearchAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);

            // Act
            var result = await _service.GetRecommendationsAsync(1, parameters);

            // Assert
            Assert.Equal(2, result.Count());
        }
    }
}