using CPAWeb.Services.Interface;
using CPAWeb.Business.Services.Services;
using CPAWeb.Data.Interface;
using CPAWeb.Data.Repository;
using Microsoft.Extensions.DependencyInjection;
using CPAWeb.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection string-ի ստացում appsettings.json-ից
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(CPAWeb.Services.Profiles.MappingProfile).Assembly));
// 2. Repository-ի և Service-ի գրանցում DI-ում
builder.Services.AddScoped<ISIDRepository>(provider => new SIDRepository(connectionString));
builder.Services.AddScoped<ISIDService, SIDService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();