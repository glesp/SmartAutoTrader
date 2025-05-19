using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartAutoTrader.Tests.Helpers
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Create test identity with admin role
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"), // User ID
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, "Admin") // Include Admin role for privileged endpoints
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            // Return successful authentication result
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}