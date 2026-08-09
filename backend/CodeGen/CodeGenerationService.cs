using System.Text.Json;
using Microsoft.Extensions.Logging;
using ViDev.Api.CodeGen.Models;

namespace ViDev.Api.CodeGen;

/// <summary>
/// Service for generating code from an abstract syntax tree.
/// </summary>
public sealed class CodeGenerationService : ICodeGenerator
{
    private readonly ILogger<CodeGenerationService> _logger;
    private readonly ControllerGenerator _controllerGenerator;

    public CodeGenerationService(ILogger<CodeGenerationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _controllerGenerator = new ControllerGenerator();
    }

    public Task<GeneratedCode> GenerateFromAstAsync(string astJson, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(astJson);

        var files = new Dictionary<string, string>();
        var warnings = new List<string>();

        try
        {
            using var document = JsonDocument.Parse(astJson);
            
            JsonElement root = document.RootElement;
            IEnumerable<JsonElement> nodes;

            if (root.ValueKind == JsonValueKind.Array)
            {
                nodes = root.EnumerateArray();
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                nodes = new[] { root };
            }
            else
            {
                warnings.Add("Invalid AST format. Expected Array or Object.");
                return Task.FromResult(new GeneratedCode(files, warnings));
            }

            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            foreach (var nodeElement in nodes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (nodeElement.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "Controller")
                {
                    try
                    {
                        var controllerNode = nodeElement.Deserialize<AstControllerNode>(serializeOptions);
                        if (controllerNode != null)
                        {
                            var generatedCode = _controllerGenerator.Generate(controllerNode);
                            var fileName = $"{controllerNode.Name}.cs";
                            files[fileName] = generatedCode;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate controller");
                        warnings.Add($"Failed to generate controller: {ex.Message}");
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AST JSON");
            warnings.Add($"Invalid JSON format: {ex.Message}");
        }

        return Task.FromResult(new GeneratedCode(files, warnings));
    }
}
