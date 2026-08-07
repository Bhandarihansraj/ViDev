using Microsoft.EntityFrameworkCore;
using ViDev.Api.Data;
using ViDev.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<ITemplateService, TemplateService>();

// EF Core + PostgreSQL
// Connection string comes from environment variable or appsettings.
// SECURITY.md §8: Never hardcode credentials. Use env vars in production.
builder.Services.AddDbContext<ViDevDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ViDevDb")
    )
);

// ---------------------------------------------------------------------------
// App Pipeline
// ---------------------------------------------------------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
