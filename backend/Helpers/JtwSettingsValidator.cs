namespace SmartAutoTrader.API.Helpers
{
    public static class JwtSettingsValidator
    {
        public static void ValidateJwtSettings(IConfiguration config)
        {
            string[] requiredKeys = new[]
            {
                "Jwt:Key",
                "Jwt:Issuer",
                "Jwt:Audience",
            };

            foreach (var key in requiredKeys)
            {
                var value = config[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException($"Missing JWT configuration key: '{key}'");
                }
            }
        }
    }
}