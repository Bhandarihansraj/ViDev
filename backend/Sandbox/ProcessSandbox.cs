using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViDev.Api.Sandbox;

/// <summary>
/// A sandbox implementation that runs the .NET CLI locally as a child process.
/// Not suitable for untrusted code in production.
/// </summary>
public sealed class ProcessSandbox : ICompileSandbox
{
    private readonly ILogger<ProcessSandbox> _logger;
    private readonly SandboxOptions _options;
    private const string DotnetFileName = "dotnet";
    private const string DotnetArguments = "build --no-restore --nologo";

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessSandbox"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sandbox options.</param>
    /// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
    public ProcessSandbox(ILogger<ProcessSandbox> logger, IOptions<SandboxOptions> options)
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

        _logger.LogInformation("Starting local build in {ProjectDirectory}", projectDirectory);

        var stopwatch = Stopwatch.StartNew();
        
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = DotnetFileName,
            Arguments = DotnetArguments,
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
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
            _logger.LogInformation("Compile {Status} in {ElapsedMs}ms for {ProjectDirectory}", 
                success ? "Succeeded" : "Failed", stopwatch.ElapsedMilliseconds, projectDirectory);

            return new CompileResult(
                Success: success,
                Output: output,
                Errors: error,
                ElapsedMs: stopwatch.ElapsedMilliseconds,
                TimedOut: false,
                ExitCode: process.ExitCode
            );
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Compile timed out after {ElapsedMs}ms for {ProjectDirectory}", stopwatch.ElapsedMilliseconds, projectDirectory);
            
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill timed out process");
            }

            return new CompileResult(
                Success: false,
                Output: string.Empty,
                Errors: "Compilation timed out.",
                ElapsedMs: stopwatch.ElapsedMilliseconds,
                TimedOut: true,
                ExitCode: -1
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Compile failed with exception in {ElapsedMs}ms for {ProjectDirectory}", stopwatch.ElapsedMilliseconds, projectDirectory);
            throw;
        }
    }
}
