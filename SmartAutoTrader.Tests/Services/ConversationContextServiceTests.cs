using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Repositories;
using SmartAutoTrader.API.Services;
using Xunit;

namespace SmartAutoTrader.Tests.Services
{
    public class ConversationContextServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IChatRepository> _chatRepoMock = new();
        private readonly Mock<ILogger<ConversationContextService>> _loggerMock = new();
        private readonly ConversationContextService _service;

        public ConversationContextServiceTests()
        {
            _service = new ConversationContextService(
                _userRepoMock.Object,
                _chatRepoMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetOrCreateContextAsync_ReturnsCachedContext_WhenSessionExists()
        {
            // Arrange
            int userId = 1;
            var existingSession = new ConversationSession
            {
                UserId = userId,
                SessionContext = "{\"MessageCount\":3}",
                LastInteractionAt = DateTime.UtcNow
            };

            _chatRepoMock.Setup(r => r.GetRecentSessionAsync(userId, It.IsAny<TimeSpan>()))
                         .ReturnsAsync(existingSession);

            // Act
            var context = await _service.GetOrCreateContextAsync(userId);

            // Assert
            Assert.NotNull(context);
            Assert.Equal(3, context.MessageCount);
        }

        [Fact]
        public async Task GetOrCreateContextAsync_CreatesNewContext_WhenNoSessionExists()
        {
            // Arrange
            int userId = 2;
            _chatRepoMock.Setup(r => r.GetRecentSessionAsync(userId, It.IsAny<TimeSpan>()))
                         .ReturnsAsync((ConversationSession)null);

            _chatRepoMock.Setup(r => r.AddSessionAsync(It.IsAny<ConversationSession>()))
                         .Returns(Task.CompletedTask);

            _chatRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var context = await _service.GetOrCreateContextAsync(userId);

            // Assert
            Assert.NotNull(context);
            Assert.Equal(0, context.MessageCount);
            Assert.NotEmpty(context.ModelUsed);
        }

        [Fact]
        public async Task UpdateContextAsync_SavesUpdatedContext()
        {
            // Arrange
            int userId = 3;
            var session = new ConversationSession { UserId = userId };

            _chatRepoMock.Setup(r => r.GetRecentSessionAsync(userId, It.IsAny<TimeSpan>()))
                         .ReturnsAsync(session);

            _chatRepoMock.Setup(r => r.UpdateSessionAsync(session)).Returns(Task.CompletedTask);
            _chatRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var updatedContext = new ConversationContext { MessageCount = 5 };

            // Act
            await _service.UpdateContextAsync(userId, updatedContext);

            // Assert
            _chatRepoMock.Verify(r => r.UpdateSessionAsync(It.IsAny<ConversationSession>()), Times.Once);
            _chatRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task StartNewSessionAsync_CreatesAndSavesSession()
        {
            // Arrange
            int userId = 4;
            _chatRepoMock.Setup(r => r.AddSessionAsync(It.IsAny<ConversationSession>()))
                         .Returns(Task.CompletedTask);

            _chatRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var newSession = await _service.StartNewSessionAsync(userId);

            // Assert
            Assert.NotNull(newSession);
            Assert.Equal(userId, newSession.UserId);
            _chatRepoMock.Verify(r => r.AddSessionAsync(It.IsAny<ConversationSession>()), Times.Once);
            _chatRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}