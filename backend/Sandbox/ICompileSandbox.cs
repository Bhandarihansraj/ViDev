using System.Threading;
using System.Threading.Tasks;

namespace ViDev.Api.Sandbox;

/// <summary>
/// Interface for sandbox compilation runners.
/// </summary>
public interface ICompileSandbox
{
    /// <summary>
    /// Compiles the project asynchronously in the given directory.
    /// </summary>
    /// <param name="projectDirectory">The directory containing the project to compile.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The result of the compilation.</returns>
    Task<CompileResult> CompileAsync(string projectDirectory, CancellationToken cancellationToken = default);
}
