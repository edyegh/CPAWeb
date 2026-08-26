using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace CPAWeb.UI.Client.Auth
{
    // Token-ը կարդում ենք localStorage-ից և դրա claim-երից կառուցում ClaimsPrincipal:
    // Ստորագրությունը այստեղ չի ստուգվում — դա անում է API-ն ամեն հարցման ժամանակ.
    // client-ի կողմի ստուգումը միայն UI-ի (օր. "users" կոճակի) համար է:
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly ClaimsPrincipal Anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        private readonly TokenStorage _tokenStorage;

        public JwtAuthenticationStateProvider(TokenStorage tokenStorage)
        {
            _tokenStorage = tokenStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _tokenStorage.GetTokenAsync();
            return new AuthenticationState(BuildPrincipal(token));
        }

        // Հաջող մուտքից հետո
        public async Task SignInAsync(string token)
        {
            await _tokenStorage.SetTokenAsync(token);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(token))));
        }

        // Ելք, ինչպես նաև 401-ի դեպքում (ժամկետանց token)
        public async Task SignOutAsync()
        {
            await _tokenStorage.ClearTokenAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
        }

        private static ClaimsPrincipal BuildPrincipal(string? token)
        {
            var payload = ParsePayload(token);

            if (payload == null)
                return Anonymous;

            // Ժամկետանց token-ը համարժեք է չմուտք գործած վիճակի
            if (payload.TryGetValue("exp", out var expElement) &&
                expElement.ValueKind == JsonValueKind.Number &&
                expElement.TryGetInt64(out var exp) &&
                DateTimeOffset.FromUnixTimeSeconds(exp) <= DateTimeOffset.UtcNow)
            {
                return Anonymous;
            }

            var claims = new List<Claim>();

            foreach (var pair in payload)
            {
                if (pair.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in pair.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(pair.Key, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(pair.Key, pair.Value.ToString()));
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt", CpaClaims.Name, CpaClaims.Role);
            return new ClaimsPrincipal(identity);
        }

        private static Dictionary<string, JsonElement>? ParsePayload(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var parts = token.Split('.');

            if (parts.Length != 3)
                return null;

            try
            {
                var json = Convert.FromBase64String(PadBase64Url(parts[1]));
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            }
            catch (Exception ex) when (ex is FormatException || ex is JsonException)
            {
                return null;
            }
        }

        // base64url -> base64 ('-' , '_' և բացակայող '=' լրացումը)
        private static string PadBase64Url(string value)
        {
            var builder = value.Replace('-', '+').Replace('_', '/');

            return (builder.Length % 4) switch
            {
                2 => builder + "==",
                3 => builder + "=",
                0 => builder,
                _ => throw new FormatException("invalid base64url string.")
            };
        }
    }
}
