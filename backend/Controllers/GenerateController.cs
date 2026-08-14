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

    public GenerateController(ICodeGenerator codeGenerator)
    {
        _codeGenerator = codeGenerator;
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
}
