using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 1. Ավելացնում ենք HttpClient, որը կապված է ձեր API-ի BASE URL-ին
builder.Services.AddScoped(sp => new HttpClient
{
    // Փոխարինեք API-ի իրական URL-ով և Port-ով (Swagger-ից կարող եք վերցնել)
    BaseAddress = new Uri("https://localhost:7091/")
});

await builder.Build().RunAsync();