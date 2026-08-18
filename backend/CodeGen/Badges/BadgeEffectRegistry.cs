using System;
using System.Collections.Generic;
using System.Linq;

namespace ViDev.Api.CodeGen.Badges;

public sealed class BadgeEffectRegistry
{
    private static readonly Dictionary<string, BadgeEffect> Effects = new(StringComparer.OrdinalIgnoreCase)
    {
        ["JWT"] = new BadgeEffect(
            BadgeName: "JWT",
            NuGetPackages: new() { "Microsoft.AspNetCore.Authentication.JwtBearer" },
            UsingDirectives: new() { "System.Text", "Microsoft.AspNetCore.Authentication.JwtBearer", "Microsoft.IdentityModel.Tokens" },
            ProgramCsStatements: new()
            {
                "builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)",
                "    .AddJwtBearer(options => {",
                "        options.TokenValidationParameters = new TokenValidationParameters",
                "        {",
                "            ValidateIssuer = true,",
                "            ValidateAudience = true,",
                "            ValidateLifetime = true,",
                "            ValidateIssuerSigningKey = true,",
                "            ValidIssuer = builder.Configuration[\"Jwt:Issuer\"],",
                "            ValidAudience = builder.Configuration[\"Jwt:Audience\"],",
                "            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration[\"Jwt:Secret\"]!))",
                "        };",
                "    });",
                "",
                "// Add after app.Build():",
                "app.UseAuthentication();",
                "app.UseAuthorization();"
            },
            AppSettingsKeys: new() { "Jwt:Secret", "Jwt:Issuer", "Jwt:Audience" },
            ProgramCsOrder: 10
        ),

        ["Authorize"] = new BadgeEffect(
            BadgeName: "Authorize",
            NuGetPackages: new(),
            UsingDirectives: new() { "Microsoft.AspNetCore.Authorization" },
            ProgramCsStatements: new() { "app.UseAuthorization();" },
            AppSettingsKeys: new(),
            ProgramCsOrder: 20
        ),

        ["ValidateModel"] = new BadgeEffect(
            BadgeName: "ValidateModel",
            NuGetPackages: new() { "FluentValidation.AspNetCore" },
            UsingDirectives: new() { "FluentValidation", "FluentValidation.AspNetCore" },
            ProgramCsStatements: new() { "builder.Services.AddFluentValidationAutoValidation();" },
            AppSettingsKeys: new(),
            ProgramCsOrder: 5
        ),

        ["ApiController"] = new BadgeEffect(
            BadgeName: "ApiController",
            NuGetPackages: new(),
            UsingDirectives: new() { "Microsoft.AspNetCore.Mvc" },
            ProgramCsStatements: new(),
            AppSettingsKeys: new(),
            ProgramCsOrder: 0
        ),

        ["AllowAnonymous"] = new BadgeEffect(
            BadgeName: "AllowAnonymous",
            NuGetPackages: new(),
            UsingDirectives: new() { "Microsoft.AspNetCore.Authorization" },
            ProgramCsStatements: new(),
            AppSettingsKeys: new(),
            ProgramCsOrder: 0
        ),

        ["Route"] = new BadgeEffect(
            BadgeName: "Route",
            NuGetPackages: new(),
            UsingDirectives: new() { "Microsoft.AspNetCore.Mvc" },
            ProgramCsStatements: new(),
            AppSettingsKeys: new(),
            ProgramCsOrder: 0
        )
    };

    public BadgeEffect? GetEffect(string badgeName) => Effects.GetValueOrDefault(badgeName);
    
    public IReadOnlyList<BadgeEffect> GetAllEffects(IEnumerable<string> badgeNames) =>
        badgeNames.Select(b => GetEffect(b)).Where(e => e != null).ToList()!;
}
