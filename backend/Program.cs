using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ViDev.Api.CodeGen;
using ViDev.Api.CodeGen.Badges;
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
builder.Services.AddSingleton<BadgeEffectRegistry>();
builder.Services.AddSingleton<ProjectAssembler>();
builder.Services.AddSingleton<ICodeGenerator, CodeGenerationService>();
builder.Services.AddScoped<ICompileAndPackageService, CompileAndPackageService>();
builder.Services.AddSingleton<ViDev.Api.CodeGen.Wiring.IWiringValidator, ViDev.Api.CodeGen.Wiring.WiringValidator>();
builder.Services.AddSingleton<ViDev.Api.CodeGen.Wiring.IWireTransformEngine, ViDev.Api.CodeGen.Wiring.WireTransformEngine>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// CORS — allow React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

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
builder.Services.AddDbContext<ViDevDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ViDevDb")
    )
);

// Compile Sandbox
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
    // Swagger UI at /swagger
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ViDev API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("DevCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

