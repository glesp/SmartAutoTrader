using System.Security.Claims;

namespace SmartAutoTrader.API.Helpers
{
    public static class ClaimsHelper
    {
        public static int? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            Claim? claim = user.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out int userId) ? userId : null;
        }
    }
}
