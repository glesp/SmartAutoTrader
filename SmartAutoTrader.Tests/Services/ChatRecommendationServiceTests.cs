
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Repositories;
using SmartAutoTrader.API.Services;
using Xunit;

namespace SmartAutoTrader.Tests.Services
{
    public class ChatRecommendationServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IChatRepository> _chatRepoMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<ILogger<ChatRecommendationService>> _loggerMock = new();
        private readonly Mock<IAIRecommendationService> _recommendationServiceMock = new();
        private readonly Mock<IConversationContextService> _contextServiceMock = new();
        private readonly HttpClient _httpClient;
        private readonly ChatRecommendationService _service;

        public ChatRecommendationServiceTests()
        {
            var httpHandlerMock = new Mock<HttpMessageHandler>();
            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"preferredMakes\":[\"Toyota\"],\"preferredFuelTypes\":[\"Petrol\"],\"preferredVehicleTypes\":[\"SUV\"]}")
                });

            _httpClient = new HttpClient(httpHandlerMock.Object);

            _configMock.Setup(c => c["Services:ParameterExtraction:Endpoint"])
                       .Returns("http://fake-endpoint/extract_parameters");

            _service = new ChatRecommendationService(
                _userRepoMock.Object,
                _chatRepoMock.Object,
                _configMock.Object,
                _loggerMock.Object,
                _httpClient,
                _recommendationServiceMock.Object,
                _contextServiceMock.Object
            );
        }

        [Fact]
        public async Task ProcessMessageAsync_ReturnsClarification_WhenRetrieverSuggestionExists()
        {
            // Arrange
            var message = new ChatMessage { Content = "I'm not sure", ConversationId = "123" };
            var userId = 1;
            var context = new ConversationContext { 
                ModelUsed = "test", 
                MessageCount = 1,
                CurrentParameters = new RecommendationParameters(),
                ShownVehicleIds = new List<int>()
            };

            var user = new User { Id = userId };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.GetFavoritesWithVehiclesAsync(userId)).ReturnsAsync(new List<UserFavorite>());
            _userRepoMock.Setup(r => r.GetRecentBrowsingHistoryWithVehiclesAsync(userId, 5)).ReturnsAsync(new List<BrowsingHistory>());
            _contextServiceMock.Setup(c => c.GetOrCreateContextAsync(userId)).ReturnsAsync(context);
            _contextServiceMock.Setup(c => c.UpdateContextAsync(userId, It.IsAny<ConversationContext>()))
                               .Returns(Task.CompletedTask);
            _chatRepoMock.Setup(c => c.AddChatHistoryAsync(It.IsAny<ChatHistory>()))
                         .Returns(Task.CompletedTask);
            _chatRepoMock.Setup(c => c.SaveChangesAsync())
                         .Returns(Task.CompletedTask);
            
            _recommendationServiceMock.Setup(r => r.GetRecommendationsAsync(userId, It.IsAny<RecommendationParameters>()))
                                      .ReturnsAsync(new List<Vehicle>());

            // Add retriever suggestion
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"retrieverSuggestion\":\"Could you clarify your request?\",\"preferredMakes\":[],\"preferredFuelTypes\":[],\"preferredVehicleTypes\":[]}")
                });

            var httpClientWithRetriever = new HttpClient(handler.Object);
            var serviceWithRetriever = new ChatRecommendationService(
                _userRepoMock.Object,
                _chatRepoMock.Object,
                _configMock.Object,
                _loggerMock.Object,
                httpClientWithRetriever,
                _recommendationServiceMock.Object,
                _contextServiceMock.Object
            );

            // Act
            var result = await serviceWithRetriever.ProcessMessageAsync(userId, message);

            // Assert
            Assert.True(result.ClarificationNeeded);
            Assert.Equal("Could you clarify your request?", result.Message);
        }

        [Fact]
        public async Task ProcessMessageAsync_HandlesMissingUserGracefully()
        {
            // Arrange
            var userId = 999;
            var message = new ChatMessage { Content = "Find me a hybrid", ConversationId = "1" };
            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null);
            _contextServiceMock.Setup(c => c.GetOrCreateContextAsync(userId))
                               .ReturnsAsync(new ConversationContext { 
                                   ModelUsed = "default", 
                                   MessageCount = 1,
                                   CurrentParameters = new RecommendationParameters(),
                                   ShownVehicleIds = new List<int>()
                               });
            _contextServiceMock.Setup(c => c.UpdateContextAsync(userId, It.IsAny<ConversationContext>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ProcessMessageAsync(userId, message);

            // Assert
            Assert.Contains("Sorry", result.Message);
            Assert.True(result.ClarificationNeeded || result.RecommendedVehicles.Count == 0);
        }

        [Fact]
        public async Task ProcessMessageAsync_ReturnsOffTopicResponse_WhenMarkedAsOffTopic()
        {
            // Arrange
            var userId = 1;
            var message = new ChatMessage { Content = "Do you like pizza?", ConversationId = "42" };

            var user = new User { Id = userId };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.GetFavoritesWithVehiclesAsync(userId)).ReturnsAsync(new List<UserFavorite>());
            _userRepoMock.Setup(r => r.GetRecentBrowsingHistoryWithVehiclesAsync(userId, 5)).ReturnsAsync(new List<BrowsingHistory>());
            
            _contextServiceMock.Setup(c => c.GetOrCreateContextAsync(userId))
                               .ReturnsAsync(new ConversationContext { 
                                   ModelUsed = "fast", 
                                   MessageCount = 2,
                                   CurrentParameters = new RecommendationParameters(),
                                   ShownVehicleIds = new List<int>()
                               });
            _contextServiceMock.Setup(c => c.UpdateContextAsync(userId, It.IsAny<ConversationContext>()))
                               .Returns(Task.CompletedTask);
                               
            _chatRepoMock.Setup(c => c.AddChatHistoryAsync(It.IsAny<ChatHistory>()))
                         .Returns(Task.CompletedTask);
            _chatRepoMock.Setup(c => c.SaveChangesAsync())
                         .Returns(Task.CompletedTask);

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"isOffTopic\":true,\"offTopicResponse\":\"I'm here to help with cars!\",\"preferredMakes\":[],\"preferredFuelTypes\":[],\"preferredVehicleTypes\":[]}")
                });

            var offTopicHttpClient = new HttpClient(handler.Object);
            var service = new ChatRecommendationService(
                _userRepoMock.Object,
                _chatRepoMock.Object,
                _configMock.Object,
                _loggerMock.Object,
                offTopicHttpClient,
                _recommendationServiceMock.Object,
                _contextServiceMock.Object
            );

            // Act
            var result = await service.ProcessMessageAsync(userId, message);

            // Assert
            Assert.Contains("cars", result.Message);
            Assert.Empty(result.RecommendedVehicles);
        }
    }
}
