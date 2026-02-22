using DotNetEnv;
using F1Fantasy.Data;
using F1Fantasy.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

// Load environment variables from .env file in development
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" || 
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development" ||
    !Environment.GetEnvironmentVariables().Contains("ASPNETCORE_ENVIRONMENT"))
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
    }
}

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use Render.com's PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// In development, enable more detailed logging
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
}

// Override configuration with environment variables
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<F1FantasyDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connString))
    {
        throw new InvalidOperationException("Database connection string 'DefaultConnection' not found. Please configure it in appsettings.json or environment variables.");
    }
    options.UseNpgsql(connString);
});

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<F1Fantasy.Services.PaginationStateTracker>();
builder.Services.AddScoped<F1Fantasy.Repository.RaceRepository>();
builder.Services.AddScoped<F1Fantasy.Services.RaceService>();
builder.Services.AddScoped<F1Fantasy.Repository.SeasonRepository>();
builder.Services.AddScoped<F1Fantasy.Services.SeasonService>();
builder.Services.AddScoped<F1Fantasy.Repository.CircuitRepository>();
builder.Services.AddScoped<F1Fantasy.Services.CircuitService>();
builder.Services.AddScoped<F1Fantasy.Repository.ConstructorRepository>();
builder.Services.AddScoped<F1Fantasy.Services.ConstructorService>();
builder.Services.AddScoped<F1Fantasy.Repository.DriverRepository>();
builder.Services.AddScoped<F1Fantasy.Services.DriverService>();
builder.Services.AddScoped<F1Fantasy.Repository.ResultRepository>();
builder.Services.AddScoped<F1Fantasy.Services.ResultService>();
builder.Services.AddScoped<F1Fantasy.Repository.QualifyingRepository>();
builder.Services.AddScoped<F1Fantasy.Services.QualifyingService>();
builder.Services.AddScoped<F1Fantasy.Repository.PitStopRepository>();
builder.Services.AddScoped<F1Fantasy.Services.PitStopService>();
builder.Services.AddScoped<F1Fantasy.Repository.LapTimingRepository>();
builder.Services.AddScoped<F1Fantasy.Services.LapTimingService>();
builder.Services.AddScoped<F1Fantasy.Repository.DriverStandingRepository>();
builder.Services.AddScoped<F1Fantasy.Services.DriverStandingService>();
builder.Services.AddScoped<F1Fantasy.Repository.ConstructorStandingRepository>();
builder.Services.AddScoped<F1Fantasy.Services.ConstructorStandingService>();
builder.Services.AddScoped<F1Fantasy.Repository.StatusRepository>();
builder.Services.AddScoped<F1Fantasy.Services.StatusService>();

// Fantasy League Services
builder.Services.AddScoped<F1Fantasy.Repository.GroupRepository>();
builder.Services.AddScoped<F1Fantasy.Repository.PredictionRepository>();
builder.Services.AddScoped<F1Fantasy.Repository.StandingRepository>();
builder.Services.AddScoped<F1Fantasy.Services.GroupService>();
builder.Services.AddScoped<F1Fantasy.Services.PredictionService>();
builder.Services.AddScoped<F1Fantasy.Services.ScoringService>();
builder.Services.AddScoped<F1Fantasy.Services.StandingsService>();

// Auto-lock background service
builder.Services.AddHostedService<F1Fantasy.Services.AutoLockService>();

// Configure Clerk JWT Authentication
var clerkSecretKey = Environment.GetEnvironmentVariable("CLERK_SECRET_KEY");
if (string.IsNullOrEmpty(clerkSecretKey))
{
    throw new InvalidOperationException("CLERK_SECRET_KEY environment variable is not set. Please configure it in .env file.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Replace with YOUR actual Frontend API URL from Clerk Dashboard
        var frontendApiUrl = "https://above-stag-28.clerk.accounts.dev";  // ← change this!

        options.Authority = frontendApiUrl;                     // Enables discovery
        options.MetadataAddress = $"{frontendApiUrl}/.well-known/openid-configuration"; // optional but good
        options.RequireHttpsMetadata = true;                    // keep true in prod

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,                              // will use discovered issuer
            ValidateAudience = false,                           // Clerk doesn't require/validate aud by default
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Do NOT set ValidIssuer or ValidIssuers manually — let discovery handle it
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Optional: log more details on failure
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var userId = context.Principal?.FindFirst("sub")?.Value; // Clerk uses "sub" for user ID
                logger.LogInformation("Token validated successfully for user: {UserId}", userId);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",  // Vite dev server
            "http://localhost:3000",  // Alternative dev port
            "https://f1fantasy.com",  // Production domain (add your actual domain)
            "https://www.f1fantasy.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Add global exception handler middleware
app.UseMiddleware<GlobalExceptionHandler>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });
}

// HTTPS redirection disabled - Render.com handles SSL/TLS at proxy level
// app.UseHttpsRedirection();

// Enable CORS - must be before UseAuthorization
app.UseCors("AllowFrontend");

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("F1Fantasy API starting up...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Listening on port: {Port}", port);

app.Run();
