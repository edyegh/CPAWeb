using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace CPAWeb.UI.Client.Auth
{
    // Ամեն API հարցմանը ավելացնում է "Authorization: Bearer <token>" վերնագիրը,
    // իսկ 401-ի դեպքում մաքրում է ժամկետանց/անվավեր token-ը, որպեսզի
    // AuthorizeRouteView-ը օգտատիրոջը ինքնաբերաբար վերադարձնի մուտքի էջ:
    public class AuthHeaderHandler : DelegatingHandler
    {
        private const string LoginPath = "/api/auth/login";

        private readonly TokenStorage _tokenStorage;
        private readonly Func<JwtAuthenticationStateProvider> _authStateProvider;

        public AuthHeaderHandler(TokenStorage tokenStorage, Func<JwtAuthenticationStateProvider> authStateProvider)
        {
            _tokenStorage = tokenStorage;
            _authStateProvider = authStateProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenStorage.GetTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Մուտքի հարցման 401-ը սովորական "սխալ գաղտնաբառ" է, ելք չենք անում
            if (response.StatusCode == HttpStatusCode.Unauthorized && !IsLoginRequest(request))
            {
                await _authStateProvider().SignOutAsync();
            }

            return response;
        }

        private static bool IsLoginRequest(HttpRequestMessage request)
        {
            return request.RequestUri != null
                && request.RequestUri.AbsolutePath.EndsWith(LoginPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
