namespace CPAWeb.UI.Client.Auth
{
    // Նույն կարճ անունները, որոնցով API-ն կառուցում է token-ը
    // (CPAWeb.API/Auth/CpaClaimTypes.cs)
    public static class CpaClaims
    {
        public const string UserId = "sub";
        public const string Name = "name";
        public const string Email = "email";
        public const string Role = "role";

        public const string AdminRole = "Admin";
        public const string UserRole = "User";
    }
}
