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

    public WiringController(IWiringValidator validator)
    {
        _validator = validator;
    }

    [HttpPost("validate")]
    public ActionResult<WiringValidationResult> Validate([FromBody] ValidateWiringRequest request)
    {
        var result = _validator.Validate(request.AstJson);
        return Ok(result);
    }
}
