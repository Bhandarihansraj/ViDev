using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ViDev.Api.CodeGen.Models;

namespace ViDev.Api.CodeGen;

/// <summary>
/// Assembles a complete .NET project from generated code files.
/// </summary>
public sealed class ProjectAssembler
{
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

        // 1. .csproj
        string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";
        result[$"{projectName}/{projectName}.csproj"] = csprojContent;

        // 2. appsettings.json
        string appSettingsContent = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""
}";
        result[$"{projectName}/appsettings.json"] = appSettingsContent;

        // Parse AST to find Services
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
                // Everything else goes to Services/
                result[$"{projectName}/Services/{fileName}"] = content;
                hasServices = true;
            }
        }

        // 4. Program.cs
        var programBuilder = new StringBuilder();
        if (hasControllers) programBuilder.AppendLine("using Generated.Controllers;");
        if (hasServices) programBuilder.AppendLine("using Generated.Services;");
        if (hasEntities) programBuilder.AppendLine("using Generated.Entities;");
        if (hasControllers || hasServices || hasEntities) programBuilder.AppendLine();

        programBuilder.AppendLine("var builder = WebApplication.CreateBuilder(args);");
        programBuilder.AppendLine("builder.Services.AddControllers();");
        
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
        programBuilder.AppendLine("app.MapControllers();");
        programBuilder.AppendLine("app.Run();");

        result[$"{projectName}/Program.cs"] = programBuilder.ToString();

        return result;
    }
}
