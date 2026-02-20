using DotNetEnv;
using F1Fantasy.Data;
using F1Fantasy.Middleware;
using Microsoft.EntityFrameworkCore;

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

app.UseAuthorization();

app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("F1Fantasy API starting up...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Listening on port: {Port}", port);

app.Run();
