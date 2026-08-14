namespace ViDev.Api.CodeGen;

/// <summary>
/// Service interface for generating code from AST.
/// </summary>
public interface ICodeGenerator
{
    Task<GeneratedCode> GenerateFromAstAsync(string astJson, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GenerateProjectAsync(string astJson, string projectName, CancellationToken cancellationToken = default);
}
