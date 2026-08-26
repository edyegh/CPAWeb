using System.Text;
using CPAWeb.Services.Interface;
using CPAWeb.Business.Services.Services;
using CPAWeb.Data.Interface;
using CPAWeb.Data.Repository;
using Microsoft.Extensions.DependencyInjection;
using CPAWeb.API.Auth;
using CPAWeb.API.Middlewares;
using CPAWeb.Services.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection string-ի ստացում appsettings.json-ից
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("https://localhost:7286", "http://localhost:5032") // Ձեր Blazor App-ի URL-ը
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger-ում "Authorize" կոճակ՝ Bearer token-ը ձեռքով փորձարկելու համար
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the token returned by /api/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(CPAWeb.Services.Profiles.MappingProfile).Assembly));
// 2. Repository-ի և Service-ի գրանցում DI-ում
builder.Services.AddScoped<ISIDRepository>(provider => new SIDRepository(connectionString));
builder.Services.AddScoped<IUserRepository>(provider => new UserRepository(connectionString));

// Կրկնվող անունների .txt ֆայլը՝ App_Data/duplicate-names.txt
string duplicatesFilePath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "duplicate-names.txt");
builder.Services.AddSingleton<IDuplicateNameLogger>(provider => new DuplicateNameLogger(duplicatesFilePath));

builder.Services.AddScoped<ISIDService, SIDService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Ավտորիզացիա — JWT bearer token
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'Jwt' not found.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
    throw new InvalidOperationException("'Jwt:Key'-ը պետք է լինի առնվազն 32 բայթ (HMAC-SHA256).");

if (jwtOptions.ExpiryMinutes <= 0)
    throw new InvalidOperationException("'Jwt:ExpiryMinutes'-ը պետք է լինի դրական թիվ.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Claim-երի անունները չենք ձևափոխում — token-ում դրանք "name" / "role" / "email" են
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),

            // [Authorize(Roles = "Admin")]-ը կարդում է հենց "role" claim-ը
            NameClaimType = CpaClaimTypes.Name,
            RoleClaimType = CpaClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 4. Առաջին ադմինները — appsettings-ի "SeedAdmins" բաժնից (գոյություն ունեցողները չեն փոփոխվում)
await SeedAdminsAsync(app);

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

// Ադմինների ցանկը կարդում ենք կոնֆիգից և ստեղծում միայն բացակայողներին:
// Բազայի անհասանելիությունը չպետք է կանգնեցնի API-ի մեկնարկը՝ միայն գրանցում ենք warning:
static async Task SeedAdminsAsync(WebApplication app)
{
    var admins = app.Configuration.GetSection("SeedAdmins").Get<List<CreateUserDto>>();

    if (admins == null || admins.Count == 0)
        return;

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        int created = await userService.EnsureSeedAdminsAsync(admins);

        if (created > 0)
            logger.LogInformation("Ստեղծվել է {Count} ադմին օգտատեր:", created);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Ադմինների seed-ը ձախողվեց: Ստուգեք cpa_web_user աղյուսակի առկայությունը (db/003_create_web_users.sql).");
    }
}
