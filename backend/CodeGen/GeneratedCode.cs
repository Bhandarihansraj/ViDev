namespace ViDev.Api.CodeGen;

/// <summary>
/// Represents the result of code generation.
/// </summary>
public sealed record GeneratedCode(
    Dictionary<string, string> Files,
    List<string> Warnings,
    string AstJson = ""
);
