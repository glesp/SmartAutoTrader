namespace SmartAutoTrader.API.Helpers
{
    using System.Security.Claims;

    public static class ClaimsHelper
    {
        public static int? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            Claim? claim = user.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out int userId) ? userId : null;
        }
    }
}