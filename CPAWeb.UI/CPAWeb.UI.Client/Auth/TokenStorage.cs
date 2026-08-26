using Microsoft.JSInterop;

namespace CPAWeb.UI.Client.Auth
{
    // JWT-ն պահվում է browser-ի localStorage-ում, որպեսզի էջը թարմացնելուց հետո
    // մուտքը չկորչի: Կարդալը cache-ավորվում է՝ ամեն հարցման վրա JS interop չկանչելու համար:
    public class TokenStorage
    {
        private const string TokenKey = "cpaweb.auth.token";

        private readonly IJSRuntime _jsRuntime;

        private string? _cachedToken;
        private bool _isLoaded;

        public TokenStorage(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async ValueTask<string?> GetTokenAsync()
        {
            if (!_isLoaded)
            {
                try
                {
                    _cachedToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
                }
                catch (JSException)
                {
                    // localStorage-ը կարող է անհասանելի լինել (private mode) — համարում ենք չմուտք գործած
                    _cachedToken = null;
                }

                _isLoaded = true;
            }

            return _cachedToken;
        }

        public async ValueTask SetTokenAsync(string token)
        {
            _cachedToken = token;
            _isLoaded = true;

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        }

        public async ValueTask ClearTokenAsync()
        {
            _cachedToken = null;
            _isLoaded = true;

            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        }
    }
}
