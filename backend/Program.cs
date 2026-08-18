using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ViDev.Api.CodeGen;
using ViDev.Api.Data;
using ViDev.Api.Sandbox;
using ViDev.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ProjectAssembler>();
builder.Services.AddSingleton<ICodeGenerator, CodeGenerationService>();
builder.Services.AddScoped<ICompileAndPackageService, CompileAndPackageService>();
builder.Services.AddSingleton<ViDev.Api.CodeGen.Wiring.IWiringValidator, ViDev.Api.CodeGen.Wiring.WiringValidator>();
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

// Compile Sandbox — SECURITY.md §5: isolation before Roslyn runs
builder.Services.Configure<SandboxOptions>(
    builder.Configuration.GetSection(SandboxOptions.SectionName));

var sandboxMode = builder.Configuration.GetValue<string>("Sandbox:Mode") ?? "Process";
if (sandboxMode.Equals("Podman", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<ICompileSandbox, PodmanSandbox>();
else
    builder.Services.AddSingleton<ICompileSandbox, ProcessSandbox>();

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
