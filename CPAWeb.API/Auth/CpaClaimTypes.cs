namespace CPAWeb.API.Auth
{
    // Կարճ claim անուններ — նույն բանալիներով է token-ը կարդում նաև Blazor client-ը
    // (CPAWeb.UI.Client/Auth/JwtAuthenticationStateProvider.cs):
    // ClaimTypes.*-ի երկար URI-ները միտումնավոր չենք օգտագործում:
    public static class CpaClaimTypes
    {
        public const string UserId = "sub";
        public const string Name = "name";
        public const string Email = "email";
        public const string Role = "role";
    }
}
