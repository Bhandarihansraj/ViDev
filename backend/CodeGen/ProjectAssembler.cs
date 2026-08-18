using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ViDev.Api.CodeGen.Models;
using ViDev.Api.CodeGen.Badges;

namespace ViDev.Api.CodeGen;

/// <summary>
/// Assembles a complete .NET project from generated code files.
/// </summary>
public sealed class ProjectAssembler
{
    private readonly BadgeEffectRegistry _registry;

    public ProjectAssembler(BadgeEffectRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Assembles a complete .NET project from generated code files.
    /// Returns a Dictionary<string, string> where key = relative file path, value = content.
    /// </summary>
    public Dictionary<string, string> Assemble(GeneratedCode generatedCode, string projectName)
    {
        ArgumentNullException.ThrowIfNull(generatedCode);
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be empty.", nameof(projectName));

        var result = new Dictionary<string, string>();

        var uniqueAnnotations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var services = new List<AstServiceNode>();
        if (!string.IsNullOrWhiteSpace(generatedCode.AstJson))
        {
            try
            {
                var serializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                using var document = JsonDocument.Parse(generatedCode.AstJson);
                var root = document.RootElement;
                IEnumerable<JsonElement> nodes = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray() : new[] { root };
                foreach (var node in nodes)
                {
                    if (node.TryGetProperty("annotations", out var annProp) && annProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ann in annProp.EnumerateArray())
                        {
                            var a = ann.GetString();
                            if (!string.IsNullOrEmpty(a)) uniqueAnnotations.Add(a);
                        }
                    }

                    if (node.TryGetProperty("methods", out var methodsProp) && methodsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var method in methodsProp.EnumerateArray())
                        {
                            if (method.TryGetProperty("annotations", out var mAnnProp) && mAnnProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var ann in mAnnProp.EnumerateArray())
                                {
                                    var a = ann.GetString();
                                    if (!string.IsNullOrEmpty(a)) uniqueAnnotations.Add(a);
                                }
                            }
                        }
                    }

                    if (node.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "Service")
                    {
                        var serviceNode = node.Deserialize<AstServiceNode>(serializeOptions);
                        if (serviceNode != null)
                        {
                            services.Add(serviceNode);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        var effects = _registry.GetAllEffects(uniqueAnnotations).OrderBy(e => e.ProgramCsOrder).ToList();

        // 1. .csproj
        var packages = effects.SelectMany(e => e.NuGetPackages).Distinct().ToList();
        string packageRefs = string.Join("\n", packages.Select(p => $"    <PackageReference Include=\"{p}\" Version=\"10.0.*\" />"));
        string itemGroup = packages.Count > 0 ? $"\n  <ItemGroup>\n{packageRefs}\n  </ItemGroup>" : "";

        string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>{itemGroup}
</Project>";
        result[$"{projectName}/{projectName}.csproj"] = csprojContent;

        // 2. appsettings.json
        var appSettingsKeys = effects.SelectMany(e => e.AppSettingsKeys).Distinct().ToList();
        var extraSettings = "";
        if (appSettingsKeys.Contains("Jwt:Secret"))
        {
            extraSettings = @",
  ""Jwt"": {
    ""Secret"": ""super-secret-key-replace-me-in-prod"",
    ""Issuer"": ""GeneratedApi"",
    ""Audience"": ""GeneratedApiClient""
  }";
        }

        string appSettingsContent = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""" + extraSettings + "\n}";
        result[$"{projectName}/appsettings.json"] = appSettingsContent;

        // 3. Folders for generated files
        var hasControllers = false;
        var hasServices = false;
        var hasEntities = false;

        foreach (var kvp in generatedCode.Files)
        {
            var fileName = kvp.Key;
            var content = kvp.Value;

            if (fileName.EndsWith("Controller.cs"))
            {
                result[$"{projectName}/Controllers/{fileName}"] = content;
                hasControllers = true;
            }
            else if (content.Contains("namespace Generated.Entities"))
            {
                result[$"{projectName}/Entities/{fileName}"] = content;
                hasEntities = true;
            }
            else
            {
                result[$"{projectName}/Services/{fileName}"] = content;
                hasServices = true;
            }
        }

        // 4. Program.cs
        var usingDirectives = new HashSet<string>();
        foreach (var effect in effects)
        {
            foreach (var u in effect.UsingDirectives) usingDirectives.Add(u);
        }

        var programBuilder = new StringBuilder();
        foreach (var u in usingDirectives) programBuilder.AppendLine($"using {u};");

        if (hasControllers) programBuilder.AppendLine("using Generated.Controllers;");
        if (hasServices) programBuilder.AppendLine("using Generated.Services;");
        if (hasEntities) programBuilder.AppendLine("using Generated.Entities;");
        if (hasControllers || hasServices || hasEntities || usingDirectives.Count > 0) programBuilder.AppendLine();

        programBuilder.AppendLine("var builder = WebApplication.CreateBuilder(args);");
        programBuilder.AppendLine("builder.Services.AddControllers();");
        
        var builderStmts = new List<string>();
        var appStmts = new List<string>();

        foreach (var effect in effects)
        {
            bool isApp = false;
            foreach (var stmt in effect.ProgramCsStatements)
            {
                if (stmt.Contains("app.") || stmt.StartsWith("// Add after app.Build()")) isApp = true;
                
                if (isApp) appStmts.Add(stmt);
                else builderStmts.Add(stmt);
            }
        }

        foreach (var stmt in builderStmts) programBuilder.AppendLine(stmt);

        foreach (var service in services)
        {
            var lifetime = service.Lifetime ?? "Scoped";
            if (lifetime != "Scoped" && lifetime != "Transient" && lifetime != "Singleton")
            {
                lifetime = "Scoped";
            }
            programBuilder.AppendLine($"builder.Services.Add{lifetime}<I{service.Name}, {service.Name}>();");
        }

        programBuilder.AppendLine("var app = builder.Build();");
        
        foreach (var stmt in appStmts)
        {
            if (stmt.StartsWith("// Add after app.Build()")) continue;
            programBuilder.AppendLine(stmt);
        }

        programBuilder.AppendLine("app.MapControllers();");
        programBuilder.AppendLine("app.Run();");

        result[$"{projectName}/Program.cs"] = programBuilder.ToString();

        return result;
    }
}
