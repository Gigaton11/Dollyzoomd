using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using DollyZoomd.Data;
using DollyZoomd.External;
using DollyZoomd.External.Interfaces;
using DollyZoomd.Middleware;
using DollyZoomd.Options;
using DollyZoomd.Repositories;
using DollyZoomd.Repositories.Interfaces;
using DollyZoomd.Services;
using DollyZoomd.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Use SQLite for development
        options.UseSqlite("Data Source=dollyzoomd.db");
    }
    else
    {
        // Use PostgreSQL for production
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT settings are not configured.");

if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be set and at least 32 characters.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtOptions.Issuer,
            ValidAudience            = jwtOptions.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Discover Configuration ────────────────────────────────────────────────────
builder.Services.Configure<DiscoverOptions>(builder.Configuration.GetSection(DiscoverOptions.SectionName));

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITvMazeClient, TvMazeClient>();
builder.Services.AddScoped<IRottenTomatoesClient, RottenTomatoesClient>();
builder.Services.AddScoped<IShowService, ShowService>();
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<IFavoritesRepository, FavoritesRepository>();
builder.Services.AddScoped<IFavoritesService, FavoritesService>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IDiscoverRepository, DiscoverRepository>();
builder.Services.AddScoped<IDiscoverService, DiscoverService>();
builder.Services.AddHostedService<PopularShowsRefreshService>();

// ── HTTP Client (TVMaze) ──────────────────────────────────────────────────────
builder.Services.AddHttpClient("TVMaze", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["TVMaze:BaseUrl"]
        ?? "https://api.tvmaze.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("RottenTomatoes", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; DollyZoomd/1.0)");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Controllers + OpenAPI (Scalar UI) ────────────────────────────────────────
builder.Services.AddControllers();

// JWT Bearer security scheme will be wired here in Phase 3 (Auth).
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "DollyZoomd API";
    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
            context.Context.Response.Headers.Expires = "0";
        }
    });
}
else
{
    app.UseStaticFiles();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
