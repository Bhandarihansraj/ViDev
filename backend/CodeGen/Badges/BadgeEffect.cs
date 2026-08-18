namespace ViDev.Api.CodeGen.Badges;

/// <summary>
/// What a badge requires beyond just a C# attribute
/// </summary>
public sealed record BadgeEffect(
    string BadgeName,
    List<string> NuGetPackages,           // packages to add to .csproj
    List<string> UsingDirectives,          // usings to add
    List<string> ProgramCsStatements,      // DI/middleware lines for Program.cs
    List<string> AppSettingsKeys,          // config keys to add to appsettings.json
    int ProgramCsOrder                     // ordering: Auth setup before UseAuth
);
