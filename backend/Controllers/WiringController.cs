using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViDev.Api.CodeGen.Wiring;
using ViDev.Api.Dtos;

namespace ViDev.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class WiringController : ControllerBase
{
    private readonly IWiringValidator _validator;
    private readonly IWireTransformEngine _transformEngine;

    public WiringController(IWiringValidator validator, IWireTransformEngine transformEngine)
    {
        _validator = validator;
        _transformEngine = transformEngine;
    }

    [HttpPost("validate")]
    public ActionResult<WiringValidationResult> Validate([FromBody] ValidateWiringRequest request)
    {
        var result = _validator.Validate(request.AstJson);
        return Ok(result);
    }

    // POST /api/wiring/transforms
    // Returns the auto-generated mapping code for type mismatches
    [HttpPost("transforms")]
    public ActionResult<TransformResult> GetTransforms([FromBody] ValidateWiringRequest request)
    {
        var result = _transformEngine.GenerateTransforms(request.AstJson);
        return Ok(result);
    }
}
