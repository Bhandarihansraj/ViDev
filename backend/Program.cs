using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ViDev.Api.Data;
using ViDev.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"] ?? "ViDev-Dev-Secret-Key-Do-Not-Use-In-Production-2026!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "ViDev.Api",
            ValidAudience = jwtSettings["Audience"] ?? "ViDev.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
