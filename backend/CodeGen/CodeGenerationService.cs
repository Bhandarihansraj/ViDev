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
    private readonly ServiceGenerator _serviceGenerator;
    private readonly EntityGenerator _entityGenerator;
    private readonly ProjectAssembler _projectAssembler;

    public CodeGenerationService(ILogger<CodeGenerationService> logger, ProjectAssembler projectAssembler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectAssembler = projectAssembler ?? throw new ArgumentNullException(nameof(projectAssembler));
        _controllerGenerator = new ControllerGenerator();
        _serviceGenerator = new ServiceGenerator();
        _entityGenerator = new EntityGenerator();
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
                return Task.FromResult(new GeneratedCode(files, warnings, astJson));
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

                if (nodeElement.TryGetProperty("type", out var typeProp))
                {
                    var typeStr = typeProp.GetString();
                    if (typeStr == "Controller")
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
                    else if (typeStr == "Service")
                    {
                        try
                        {
                            var serviceNode = nodeElement.Deserialize<AstServiceNode>(serializeOptions);
                            if (serviceNode != null)
                            {
                                var generatedFiles = _serviceGenerator.Generate(serviceNode);
                                foreach (var kvp in generatedFiles)
                                {
                                    files[kvp.Key] = kvp.Value;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate service");
                            warnings.Add($"Failed to generate service: {ex.Message}");
                        }
                    }
                    else if (typeStr == "Entity")
                    {
                        try
                        {
                            var entityNode = nodeElement.Deserialize<AstEntityNode>(serializeOptions);
                            if (entityNode != null)
                            {
                                var generatedCode = _entityGenerator.Generate(entityNode);
                                var fileName = $"{entityNode.Name}.cs";
                                files[fileName] = generatedCode;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate entity");
                            warnings.Add($"Failed to generate entity: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AST JSON");
            warnings.Add($"Invalid JSON format: {ex.Message}");
        }

        return Task.FromResult(new GeneratedCode(files, warnings, astJson));
    }

    public async Task<Dictionary<string, string>> GenerateProjectAsync(string astJson, string projectName, CancellationToken cancellationToken = default)
    {
        var generatedCode = await GenerateFromAstAsync(astJson, cancellationToken);
        return _projectAssembler.Assemble(generatedCode, projectName);
    }
}
