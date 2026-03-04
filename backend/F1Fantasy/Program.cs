using DotNetEnv;
using F1Fantasy.Data;
using F1Fantasy.Middleware;
using F1Fantasy.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

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
    
    // Limit request body size to prevent abuse (10MB max)
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    
    // Limit concurrent connections to prevent resource exhaustion
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
    
    // Request timeout to kill long-running requests (2 minutes)
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(120);
    
    // Limit header size to prevent header-based DoS
    options.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32KB
    options.Limits.MaxRequestLineSize = 8 * 1024; // 8KB
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});
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
    
    // Force optimal connection pooling parameters
    // Remove any existing pooling params and add our own
    connString = System.Text.RegularExpressions.Regex.Replace(
        connString, 
        @"(Pooling|Minimum Pool Size|Maximum Pool Size|Connection Idle Lifetime|Connection Pruning Interval)\s*=\s*[^;]*;?", 
        "", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    
    var poolingParams = "Pooling=true;Minimum Pool Size=10;Maximum Pool Size=100;Connection Idle Lifetime=60;Connection Pruning Interval=5";
    connString = connString.TrimEnd(';') + ";" + poolingParams;
    
    options.UseNpgsql(connString, npgsqlOptions =>
    {
        // Set command timeout to prevent long-running queries (30 seconds)
        npgsqlOptions.CommandTimeout(30);
        
        // Enable retry on failure (helpful for transient network issues on Render)
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
    
    // Disable query tracking by default for read-only queries (performance)
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    
    // Log slow queries in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add DbContextFactory for services that need concurrent database access
builder.Services.AddDbContextFactory<F1FantasyDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connString))
    {
        throw new InvalidOperationException("Database connection string 'DefaultConnection' not found. Please configure it in appsettings.json or environment variables.");
    }
    
    // Force optimal connection pooling parameters
    connString = System.Text.RegularExpressions.Regex.Replace(
        connString, 
        @"(Pooling|Minimum Pool Size|Maximum Pool Size|Connection Idle Lifetime|Connection Pruning Interval)\s*=\s*[^;]*;?", 
        "", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    
    var poolingParams = "Pooling=true;Minimum Pool Size=10;Maximum Pool Size=100;Connection Idle Lifetime=60;Connection Pruning Interval=5";
    connString = connString.TrimEnd(';') + ";" + poolingParams;
    
    options.UseNpgsql(connString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache(); // For caching Clerk user data and other frequently accessed data
builder.Services.AddSingleton<F1Fantasy.Services.PaginationStateTracker>();
builder.Services.AddScoped<F1Fantasy.Repository.DataFetchMetadataRepository>();
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
builder.Services.AddScoped<F1Fantasy.Services.ClerkService>();

// Rate limiting and security services
builder.Services.AddSingleton<IIpBlacklistService, IpBlacklistService>();
builder.Services.AddSingleton<RateLimitViolationMonitor>();

// Auto-lock background service
builder.Services.AddHostedService<F1Fantasy.Services.AutoLockService>();

// Validate Clerk secret key (required for ClerkService backend API calls)
var clerkSecretKey = Environment.GetEnvironmentVariable("CLERK_SECRET_KEY");
if (string.IsNullOrEmpty(clerkSecretKey))
{
    throw new InvalidOperationException("CLERK_SECRET_KEY environment variable is not set. Required for Clerk Backend API.");
}

// Configure Clerk JWT Authentication
// Accept tokens from multiple Clerk instances
var clerkUrls = new[]
{
    "https://clerk.f1fantasy.no",              // Production
    "https://above-stag-28.clerk.accounts.dev" // Development
};

// Register MultiClerkConfigurationManager as singleton using DI
builder.Services.AddSingleton<F1Fantasy.Services.MultiClerkConfigurationManager>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<F1Fantasy.Services.MultiClerkConfigurationManager>>();
    var httpClient = httpClientFactory.CreateClient("ClerkConfiguration");
    
    // Configure timeout for OIDC configuration requests
    httpClient.Timeout = TimeSpan.FromSeconds(30);
    
    return new F1Fantasy.Services.MultiClerkConfigurationManager(
        clerkUrls,
        httpClient,
        logger
    );
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<F1Fantasy.Services.MultiClerkConfigurationManager>((options, configManager) =>
    {
        options.ConfigurationManager = configManager;
        options.RequireHttpsMetadata = true;

        // Pre-fetch configuration to get valid issuers
        var configuration = configManager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
        var validIssuers = configManager.GetValidIssuers();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            // Use issuers derived from discovery documents (normalized)
            ValidIssuers = validIssuers,
            ValidateAudience = false,  // Clerk doesn't use audience validation
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

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
                var userId = context.Principal?.FindFirst("sub")?.Value;
                var issuer = context.Principal?.FindFirst("iss")?.Value;
                logger.LogInformation("Token validated for user {UserId} from {Issuer}", userId, issuer);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Global fallback - applies to all endpoints not specifically configured
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queueing - reject immediately when limit exceeded
            });
    });

    // Policy for read operations (GET requests)
    options.AddPolicy("read", context =>
    {
        var userId = context.User.FindFirst("sub")?.Value ?? 
                     context.Connection.RemoteIpAddress?.ToString() ?? 
                     "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Policy for write operations (POST/PUT/DELETE)
    options.AddPolicy("write", context =>
    {
        var userId = context.User.FindFirst("sub")?.Value ?? 
                     context.Connection.RemoteIpAddress?.ToString() ?? 
                     "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Policy for admin operations
    options.AddPolicy("admin", context =>
    {
        var userId = context.User.FindFirst("sub")?.Value ?? 
                     context.Connection.RemoteIpAddress?.ToString() ?? 
                     "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Configure rejection response with violation tracking
    options.ConfigureRateLimitRejection();
});

// Add response compression to reduce bandwidth costs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = new[]
    {
        "application/json",
        "application/xml",
        "text/plain",
        "text/html",
        "text/css",
        "application/javascript"
    };
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // Balance speed vs compression
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Add response caching to reduce repeated requests
builder.Services.AddResponseCaching();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
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
            "https://f1fantasy-1.onrender.com",
            "https://f1fantasy.no",
            "https://www.f1fantasy.no"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Add request context middleware first to attach correlation id and total request timing
app.UseMiddleware<RequestContextLoggingMiddleware>();

// Add response compression early in pipeline (before other middleware)
app.UseResponseCompression();

// Add cache headers to reduce repeated requests
app.UseMiddleware<CacheHeaderMiddleware>();

// Add IP blacklist middleware - MUST be early in pipeline
app.UseMiddleware<IpBlacklistMiddleware>();

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

// Enable response caching to reduce duplicate requests
app.UseResponseCaching();

// Enable rate limiting - must be after CORS and before authentication
app.UseRateLimiter();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("F1Fantasy API starting up...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Listening on port: {Port}", port);

// Prewarm database connection pool
logger.LogInformation("Prewarming database connection pool...");
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<F1FantasyDbContext>();
    try
    {
        // Execute a simple query to open initial connections in the pool
        var canConnect = await dbContext.Database.CanConnectAsync();
        logger.LogInformation("Database connection pool prewarmed successfully. CanConnect: {CanConnect}", canConnect);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to prewarm database connection pool: {Message}", ex.Message);
    }
}

app.Run();
