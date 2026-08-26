using System.Net.Http;
using CPAWeb.UI.Client.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 1. Ավտորիզացիա — token-ը localStorage-ում, վիճակը՝ AuthenticationStateProvider-ում
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());

// 2. HttpClient, որը կապված է ձեր API-ի BASE URL-ին և ավտոմատ ավելացնում է Bearer token-ը
builder.Services.AddScoped(sp =>
{
    var handler = new AuthHeaderHandler(
        sp.GetRequiredService<TokenStorage>(),
        sp.GetRequiredService<JwtAuthenticationStateProvider>)
    {
        InnerHandler = new HttpClientHandler()
    };

    return new HttpClient(handler)
    {
        // Փոխարինեք API-ի իրական URL-ով և Port-ով (Swagger-ից կարող եք վերցնել)
        BaseAddress = new Uri("https://localhost:7091/")
    };
});

builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
