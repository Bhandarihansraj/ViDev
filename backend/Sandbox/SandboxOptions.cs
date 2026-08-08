namespace ViDev.Api.Sandbox;

/// <summary>
/// Options for configuring the sandbox environment.
/// </summary>
public sealed class SandboxOptions
{
    /// <summary>
    /// The section name in the configuration file.
    /// </summary>
    public const string SectionName = "Sandbox";

    /// <summary>
    /// The execution mode, "Process" or "Podman".
    /// </summary>
    public string Mode { get; init; } = "Process";

    /// <summary>
    /// The maximum allowed time for a build in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// The maximum number of CPUs to allocate (for Podman mode).
    /// </summary>
    public int MaxCpus { get; init; } = 1;

    /// <summary>
    /// The maximum memory to allocate in megabytes (for Podman mode).
    /// </summary>
    public int MaxMemoryMb { get; init; } = 512;

    /// <summary>
    /// The container image to use for building (for Podman mode).
    /// </summary>
    public string ContainerImage { get; init; } = "mcr.microsoft.com/dotnet/sdk:10.0-alpine";
}
