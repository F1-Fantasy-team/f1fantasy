var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
