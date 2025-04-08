using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using SmartAutoTrader.Models;
using SmartAutoTrader.Services;
using System.Linq.Expressions;
using SmartAutoTrader.Repositories;

namespace SmartAutoTrader.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Mock IUserRepository
            _userRepositoryMock = new Mock<IUserRepository>();

            // Mock UserManager
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Mock IConfiguration
            _configMock = new Mock<IConfiguration>();
            var section = new Mock<IConfigurationSection>();
            section.Setup(s => s.Value).Returns("test_secret_key_that_is_long_enough_for_hs256_algorithm");
            _configMock.Setup(c => c.GetSection("JwtSettings:SecretKey")).Returns(section.Object);

            // Create AuthService with mocked dependencies
            _authService = new AuthService(
                _userManagerMock.Object,
                _userRepositoryMock.Object,
                _configMock.Object);
        }

        [Fact]
        public async Task Register_WithValidUser_ShouldSucceed()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };

            _userRepositoryMock
                .Setup(repo => repo.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);

            _userManagerMock
                .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.Register(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("User registered successfully", result.Message);
            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password), Times.Once);
        }

        [Fact]
        public async Task Register_WithExistingEmail_ShouldFail()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "existing@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };

            _userRepositoryMock
                .Setup(repo => repo.FindByEmailAsync(request.Email))
                .ReturnsAsync(new ApplicationUser { Email = request.Email });

            // Act
            var result = await _authService.Register(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User with this email already exists", result.Message);
            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var user = new ApplicationUser
            {
                Id = "user123",
                Email = request.Email,
                FirstName = "Test",
                LastName = "User"
            };

            _userRepositoryMock
                .Setup(repo => repo.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(um => um.CheckPasswordAsync(user, request.Password))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.Login(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Login successful", result.Message);
            Assert.NotEmpty(result.Token);
        }

        [Fact]
        public async Task Login_WithInvalidEmail_ShouldFail()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _userRepositoryMock
                .Setup(repo => repo.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _authService.Login(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ShouldFail()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            var user = new ApplicationUser
            {
                Email = request.Email,
                FirstName = "Test",
                LastName = "User"
            };

            _userRepositoryMock
                .Setup(repo => repo.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(um => um.CheckPasswordAsync(user, request.Password))
                .ReturnsAsync(false);

            // Act
            var result = await _authService.Login(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid password", result.Message);
            Assert.Null(result.Token);
        }
    }
}
