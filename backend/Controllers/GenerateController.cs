using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViDev.Api.CodeGen;
using ViDev.Api.Dtos;

namespace ViDev.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GenerateController : ControllerBase
{
    private readonly ICodeGenerator _codeGenerator;
    private readonly ICompileAndPackageService _compileService;

    public GenerateController(ICodeGenerator codeGenerator, ICompileAndPackageService compileService)
    {
        _codeGenerator = codeGenerator;
        _compileService = compileService;
    }

    // POST /api/generate
    [HttpPost]
    public async Task<ActionResult<Dictionary<string, string>>> Generate([FromBody] GenerateRequest request)
    {
        var files = await _codeGenerator.GenerateProjectAsync(request.AstJson, request.ProjectName, HttpContext.RequestAborted);
        return Ok(files);
    }

    // POST /api/generate/preview
    [HttpPost("preview")]
    public async Task<ActionResult<List<string>>> Preview([FromBody] GenerateRequest request)
    {
        var files = await _codeGenerator.GenerateProjectAsync(request.AstJson, request.ProjectName, HttpContext.RequestAborted);
        return Ok(files.Keys.ToList());
    }

    // POST /api/generate/compile
    // Generates, compiles in sandbox, returns compile result (no ZIP)
    [HttpPost("compile")]
    public async Task<ActionResult<CompileResultDto>> Compile([FromBody] GenerateRequest request)
    {
        var result = await _compileService.GenerateCompileAndPackageAsync(request.AstJson, request.ProjectName, HttpContext.RequestAborted);
        var dto = new CompileResultDto(
            result.CompileSuccess,
            result.CompileOutput,
            result.CompileErrors,
            result.CompileElapsedMs,
            result.TimedOut,
            result.GeneratedFiles.Keys.ToList()
        );
        return Ok(dto);
    }

    // POST /api/generate/download  
    // Generates, compiles, returns ZIP file if compile succeeds
    [HttpPost("download")]
    public async Task<IActionResult> Download([FromBody] GenerateRequest request)
    {
        var result = await _compileService.GenerateCompileAndPackageAsync(request.AstJson, request.ProjectName, HttpContext.RequestAborted);
        if (result.CompileSuccess && result.ZipBytes != null)
        {
            return File(result.ZipBytes, "application/zip", $"{request.ProjectName}.zip");
        }

        var dto = new CompileResultDto(
            result.CompileSuccess,
            result.CompileOutput,
            result.CompileErrors,
            result.CompileElapsedMs,
            result.TimedOut,
            result.GeneratedFiles.Keys.ToList()
        );
        return UnprocessableEntity(dto);
    }
}
