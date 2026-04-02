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
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. " +
            "Ensure appsettings.json or environment variables provide a valid PostgreSQL connection string.");
    }

    // Use PostgreSQL for both development and production
    options.UseNpgsql(connectionString);
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
builder.Services.Configure<AvatarOptions>(builder.Configuration.GetSection(AvatarOptions.SectionName));

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
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();
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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

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

// ── Health Check Endpoint ─────────────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithOpenApi()
    .AllowAnonymous();

// ── Database Migration Bootstrap ──────────────────────────────────────────────
// In Cloud Run, this can be enabled via environment variable APPLY_MIGRATIONS_ON_STARTUP=true
// to safely create/update schema on first deployment. Set to false after bootstrap for security.
ApplyStartupMigrationsIfEnabled(app, builder.Configuration);

app.Run();

static void ApplyStartupMigrationsIfEnabled(WebApplication app, IConfiguration configuration)
{
    if (!bool.TryParse(configuration["APPLY_MIGRATIONS_ON_STARTUP"], out var applyMigrations) || !applyMigrations)
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.Migrations");

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        // Startup should continue even if migration fails; health checks and logs expose the issue.
        logger.LogError(ex, "Error applying database migrations at startup.");
    }
}
