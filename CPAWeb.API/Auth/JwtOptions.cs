namespace CPAWeb.API.Auth
{
    // appsettings.json -> "Jwt" բաժինը
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        // Ստորագրման բանալին — production-ում պահել user secrets-ում կամ միջավայրի փոփոխականում
        public string Key { get; set; } = string.Empty;

        public string Issuer { get; set; } = "CPAWeb.API";

        public string Audience { get; set; } = "CPAWeb.UI";

        public int ExpiryMinutes { get; set; } = 480;
    }
}
