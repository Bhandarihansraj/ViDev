using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViDev.Api.Sandbox;

/// <summary>
/// A production-grade sandbox that runs <c>dotnet build</c> inside a Podman container
/// with full network isolation, CPU/memory limits, and read-only filesystem.
/// Per SECURITY.md §5: untrusted AST must never compile on the API host.
/// </summary>
public sealed class PodmanSandbox : ICompileSandbox
{
    private readonly ILogger<PodmanSandbox> _logger;
    private readonly SandboxOptions _options;

    private const string PodmanExecutable = "podman";
    private const string BuildCommand = "dotnet build /src --no-restore --nologo";
    private const int ContainerNameLength = 12;

    /// <summary>
    /// Initializes a new instance of the <see cref="PodmanSandbox"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sandbox configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
    public PodmanSandbox(ILogger<PodmanSandbox> logger, IOptions<SandboxOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<CompileResult> CompileAsync(string projectDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

        if (!Directory.Exists(projectDirectory))
        {
            throw new DirectoryNotFoundException($"Project directory not found: {projectDirectory}");
        }

        var containerName = $"videv-compile-{Guid.NewGuid():N}"[..ContainerNameLength];
        var absolutePath = Path.GetFullPath(projectDirectory);

        _logger.LogInformation(
            "Starting Podman build in container {ContainerName} for {ProjectDirectory}",
            containerName, absolutePath);

        var podmanArgs = string.Join(" ",
            "run", "--rm",
            $"--name {containerName}",
            "--network none",
            "--read-only",
            "--tmpfs /tmp:rw,size=64m",
            $"--cpus={_options.MaxCpus}",
            $"--memory={_options.MaxMemoryMb}m",
            $"-v \"{absolutePath}\":/src:ro",
            _options.ContainerImage,
            BuildCommand);

        var stopwatch = Stopwatch.StartNew();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = PodmanExecutable,
            Arguments = podmanArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = processStartInfo };

        try
        {
            process.Start();

            var readOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var readErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(linkedCts.Token);

            var output = await readOutputTask;
            var error = await readErrorTask;
            stopwatch.Stop();

            var success = process.ExitCode == 0;
            _logger.LogInformation(
                "Podman compile {Status} in {ElapsedMs}ms — container {ContainerName}",
                success ? "Succeeded" : "Failed", stopwatch.ElapsedMilliseconds, containerName);

            return new CompileResult(
                Success: success,
                Output: output,
                Errors: error,
                ElapsedMs: stopwatch.ElapsedMilliseconds,
                TimedOut: false,
                ExitCode: process.ExitCode);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Podman compile timed out after {ElapsedMs}ms — killing container {ContainerName}",
                stopwatch.ElapsedMilliseconds, containerName);

            await KillContainerAsync(containerName);

            return new CompileResult(
                Success: false,
                Output: string.Empty,
                Errors: $"Compilation timed out after {_options.TimeoutSeconds}s.",
                ElapsedMs: stopwatch.ElapsedMilliseconds,
                TimedOut: true,
                ExitCode: -1);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Podman compile failed with exception in {ElapsedMs}ms — container {ContainerName}",
                stopwatch.ElapsedMilliseconds, containerName);
            throw;
        }
    }

    /// <summary>
    /// Forcefully kills and removes a Podman container by name.
    /// </summary>
    private async Task KillContainerAsync(string containerName)
    {
        try
        {
            await RunPodmanCommandAsync($"kill {containerName}");
            await RunPodmanCommandAsync($"rm -f {containerName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to kill/remove container {ContainerName}", containerName);
        }
    }

    /// <summary>
    /// Runs a Podman CLI command and waits for it to complete (5s timeout).
    /// </summary>
    private static async Task RunPodmanCommandAsync(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = PodmanExecutable,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });

        if (process is not null)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(cts.Token);
        }
    }
}
