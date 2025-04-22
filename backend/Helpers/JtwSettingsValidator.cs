namespace SmartAutoTrader.API.Helpers
{
    public static class JwtSettingsValidator
    {
        public static void ValidateJwtSettings(IConfiguration config)
        {
            string[] requiredKeys =
            [
                "Jwt:Key",
                "Jwt:Issuer",
                "Jwt:Audience"
            ];

            foreach (string key in requiredKeys)
            {
                string? value = config[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException($"Missing JWT configuration key: '{key}'");
                }
            }
        }
    }
}