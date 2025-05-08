using System;
using System.Collections.Generic;
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
        private readonly Mock<IRoleRepository> _roleRepoMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _roleRepoMock = new Mock<IRoleRepository>();
            _configMock = new Mock<IConfiguration>();

            _configMock.Setup(c => c["Jwt:Key"]).Returns("your-super-secret-key-with-at-least-32-characters");
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("SmartAutoTrader");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("SmartAutoTraderUsers");

            // Set up role repository mock for user role assignment
            _roleRepoMock.Setup(r => r.GetRoleByNameAsync("User"))
                .ReturnsAsync(new Role { Id = 2, Name = "User" });
            _roleRepoMock.Setup(r => r.AssignRoleToUserAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _roleRepoMock.Setup(r => r.GetUserRolesAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "User" });

            _authService = new AuthService(
                _userRepoMock.Object,
                _roleRepoMock.Object,
                _configMock.Object);
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
            // Verify role assignment
            _roleRepoMock.Verify(r => r.AssignRoleToUserAsync(It.IsAny<int>(), 2), Times.Once);
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
            _roleRepoMock.Verify(r => r.AssignRoleToUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
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
            _roleRepoMock.Verify(r => r.GetUserRolesAsync(user.Id), Times.Once);
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

        [Fact]
        public async Task GetUserRolesAsync_ShouldReturnRolesList()
        {
            // Arrange
            int userId = 1;
            var expectedRoles = new List<string> { "User", "Admin" };

            _roleRepoMock.Setup(r => r.GetUserRolesAsync(userId))
                .ReturnsAsync(expectedRoles);

            // Act
            var roles = await _authService.GetUserRolesAsync(userId);

            // Assert
            Assert.Equal(expectedRoles, roles);
            _roleRepoMock.Verify(r => r.GetUserRolesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task AssignRoleToUserAsync_WithValidRole_ShouldAssignRole()
        {
            // Arrange
            int userId = 1;
            string roleName = "Admin";
            var role = new Role { Id = 1, Name = roleName };

            _roleRepoMock.Setup(r => r.GetRoleByNameAsync(roleName))
                .ReturnsAsync(role);

            // Act
            await _authService.AssignRoleToUserAsync(userId, roleName);

            // Assert
            _roleRepoMock.Verify(r => r.GetRoleByNameAsync(roleName), Times.Once);
            _roleRepoMock.Verify(r => r.AssignRoleToUserAsync(userId, role.Id), Times.Once);
        }

        [Fact]
        public async Task AssignRoleToUserAsync_WithInvalidRole_ShouldThrow()
        {
            // Arrange
            int userId = 1;
            string roleName = "NonExistentRole";

            _roleRepoMock.Setup(r => r.GetRoleByNameAsync(roleName))
                .ReturnsAsync((Role)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _authService.AssignRoleToUserAsync(userId, roleName));

            Assert.Equal($"Role '{roleName}' not found.", ex.Message);
            _roleRepoMock.Verify(r => r.GetRoleByNameAsync(roleName), Times.Once);
            _roleRepoMock.Verify(r => r.AssignRoleToUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
    }
}