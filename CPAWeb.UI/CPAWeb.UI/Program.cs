using CPAWeb.UI.Client.Pages;
using CPAWeb.UI.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Client-ի էջերի [Authorize]-ը դառնում է նաև սերվերի endpoint-ի metadata,
// ուստի այստեղ պարտադիր է authorization middleware-ը: Իրական ստուգումը
// կատարվում է browser-ում (AuthorizeRouteView) և API-ում ([Authorize] controller-ների վրա) —
// այս host-ը միայն ստատիկ էջն է տալիս և token չի տեսնում, քանի որ այն localStorage-ում է:
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7091/") // API-ի հասցեն
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CPAWeb.UI.Client._Imports).Assembly)
    // Էջերի HTML-ը տրվում է բոլորին — պաշտպանված են տվյալները (API), ոչ թե ստատիկ shell-ը
    .AllowAnonymous();

app.Run();
