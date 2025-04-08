using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Repositories;
using SmartAutoTrader.API.Services;
using Xunit;

namespace SmartAutoTrader.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _configMock = new Mock<IConfiguration>();

            _configMock.Setup(c => c["Jwt:Key"]).Returns("your-super-secret-key-with-at-least-32-characters");
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("SmartAutoTrader");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("SmartAutoTraderUsers");

            _authService = new AuthService(_userRepoMock.Object, _configMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_WithUniqueUser_ShouldCreateNewUser()
        {
            // Arrange
            _userRepoMock.Setup(r => r.ExistsAsync("test@example.com", "testuser"))
                         .ReturnsAsync(false);

            User? capturedUser = null;
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                         .Callback<User>(u => capturedUser = u)
                         .Returns(Task.CompletedTask);

            _userRepoMock.Setup(r => r.SaveChangesAsync())
                         .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.RegisterAsync(
                "testuser", "test@example.com", "Password123",
                "Test", "User", "1234567890");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("Test", result.FirstName);
            Assert.Equal("User", result.LastName);
            Assert.Equal("1234567890", result.PhoneNumber);
            Assert.True(BCrypt.Net.BCrypt.Verify("Password123", result.PasswordHash));
            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingUser_ShouldThrowException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.ExistsAsync("test@example.com", "testuser"))
                         .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _authService.RegisterAsync("testuser", "test@example.com", "Password123",
                    "Test", "User", "1234567890"));

            Assert.Equal("User with this email or username already exists.", ex.Message);
            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnTokenAndUser()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com"))
                         .ReturnsAsync(user);

            // Act
            var (token, returnedUser) = await _authService.LoginAsync("test@example.com", "Password123");

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Equal(user, returnedUser);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ShouldThrow()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetByEmailAsync("noone@example.com"))
                         .ReturnsAsync((User?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _authService.LoginAsync("noone@example.com", "Password123"));

            Assert.Equal("Invalid email or password.", ex.Message);
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ShouldThrow()
        {
            // Arrange
            var user = new User
            {
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword")
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com"))
                         .ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _authService.LoginAsync("test@example.com", "WrongPassword"));

            Assert.Equal("Invalid email or password.", ex.Message);
        }
    }
}
