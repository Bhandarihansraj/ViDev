using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViDev.Api.Sandbox;

namespace ViDev.Api.CodeGen;

public interface ICompileAndPackageService
{
    Task<CompileAndPackageResult> GenerateCompileAndPackageAsync(
        string astJson, string projectName, CancellationToken cancellationToken = default);
}

public sealed record CompileAndPackageResult(
    bool CompileSuccess,
    string CompileOutput,
    string CompileErrors,
    long CompileElapsedMs,
    bool TimedOut,
    byte[]? ZipBytes,
    Dictionary<string, string> GeneratedFiles
);

public sealed class CompileAndPackageService : ICompileAndPackageService
{
    private readonly ICodeGenerator _codeGenerator;
    private readonly ICompileSandbox _compileSandbox;
    private readonly ILogger<CompileAndPackageService> _logger;

    public CompileAndPackageService(
        ICodeGenerator codeGenerator,
        ICompileSandbox compileSandbox,
        ILogger<CompileAndPackageService> logger)
    {
        _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        _compileSandbox = compileSandbox ?? throw new ArgumentNullException(nameof(compileSandbox));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CompileAndPackageResult> GenerateCompileAndPackageAsync(
        string astJson, string projectName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(astJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        var generatedFiles = await _codeGenerator.GenerateProjectAsync(astJson, projectName, cancellationToken);
        string tempBaseDir = Path.Combine(Path.GetTempPath(), "videv", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempBaseDir);

        try
        {
            foreach (var kvp in generatedFiles)
            {
                var filePath = Path.Combine(tempBaseDir, kvp.Key);
                var fileDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                }
                await File.WriteAllTextAsync(filePath, kvp.Value, cancellationToken);
            }

            string globalJsonContent = @"{
  ""sdk"": {
    ""version"": ""8.0.0"",
    ""rollForward"": ""latestMajor""
  }
}";
            await File.WriteAllTextAsync(Path.Combine(tempBaseDir, "global.json"), globalJsonContent, cancellationToken);

            string projectSubDir = Path.Combine(tempBaseDir, projectName);

            var restoreResult = await RunDotnetRestoreAsync(projectSubDir, cancellationToken);
            if (!restoreResult.Success)
            {
                _logger.LogWarning("Dotnet restore failed for {ProjectName}. Output: {Output}, Errors: {Errors}", projectName, restoreResult.Output, restoreResult.Errors);
                return new CompileAndPackageResult(
                    CompileSuccess: false,
                    CompileOutput: restoreResult.Output,
                    CompileErrors: restoreResult.Errors,
                    CompileElapsedMs: restoreResult.ElapsedMs,
                    TimedOut: restoreResult.TimedOut,
                    ZipBytes: null,
                    GeneratedFiles: generatedFiles
                );
            }

            var compileResult = await _compileSandbox.CompileAsync(projectSubDir, cancellationToken);
            byte[]? zipBytes = null;

            if (compileResult.Success)
            {
                string tempZipPath = tempBaseDir + ".zip";
                try
                {
                    ZipFile.CreateFromDirectory(projectSubDir, tempZipPath);
                    zipBytes = await File.ReadAllBytesAsync(tempZipPath, cancellationToken);
                }
                finally
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                }
            }

            return new CompileAndPackageResult(
                CompileSuccess: compileResult.Success,
                CompileOutput: compileResult.Output,
                CompileErrors: compileResult.Errors,
                CompileElapsedMs: compileResult.ElapsedMs,
                TimedOut: compileResult.TimedOut,
                ZipBytes: zipBytes,
                GeneratedFiles: generatedFiles
            );
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempBaseDir))
                {
                    Directory.Delete(tempBaseDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up temp directory {TempDir}", tempBaseDir);
            }
        }
    }

    private async Task<(bool Success, string Output, string Errors, long ElapsedMs, bool TimedOut)> RunDotnetRestoreAsync(string projectDirectory, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "restore",
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        
        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            var output = await outputTask;
            var error = await errorTask;
            sw.Stop();

            return (process.ExitCode == 0, output, error, sw.ElapsedMilliseconds, false);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch { }
            return (false, "", "Restore timed out.", sw.ElapsedMilliseconds, true);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, "", ex.Message, sw.ElapsedMilliseconds, false);
        }
    }
}
