namespace ViDev.Api.Sandbox;

/// <summary>
/// Represents the result of a compilation run.
/// </summary>
/// <param name="Success">Indicates whether the build succeeded.</param>
/// <param name="Output">The standard output from the build process.</param>
/// <param name="Errors">The standard error from the build process.</param>
/// <param name="ElapsedMs">The wall-clock time elapsed during the build in milliseconds.</param>
/// <param name="TimedOut">Indicates whether the process exceeded the configured timeout.</param>
/// <param name="ExitCode">The exit code of the build process.</param>
public sealed record CompileResult(
    bool Success,
    string Output,
    string Errors,
    long ElapsedMs,
    bool TimedOut,
    int ExitCode
);
