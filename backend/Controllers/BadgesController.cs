using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ViDev.Api.CodeGen.Badges;
using ViDev.Api.Dtos;

namespace ViDev.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BadgesController : ControllerBase
{
    private readonly BadgeEffectRegistry _registry;

    public BadgesController(BadgeEffectRegistry registry)
    {
        _registry = registry;
    }

    [HttpGet]
    public ActionResult<List<BadgeInfoDto>> GetAll()
    {
        var badges = new List<BadgeInfoDto>
        {
            new("JWT", new List<string> { "Microsoft.AspNetCore.Authentication.JwtBearer" }, "Adds JWT Bearer authentication with configurable issuer, audience, and secret"),
            new("Authorize", new List<string>(), "Requires authentication for this controller or method"),
            new("AllowAnonymous", new List<string>(), "Allows unauthenticated access to this method"),
            new("ValidateModel", new List<string> { "FluentValidation.AspNetCore" }, "Adds FluentValidation auto-validation for request models"),
            new("ApiController", new List<string>(), "Marks this class as an API controller with automatic model binding"),
            new("Route", new List<string>(), "Configures the route prefix for this controller")
        };
        return Ok(badges);
    }

    [HttpGet("{name}")]
    public ActionResult<BadgeInfoDto> GetByName(string name)
    {
        var effect = _registry.GetEffect(name);
        if (effect == null)
            return NotFound();

        var desc = name switch
        {
            "JWT" => "Adds JWT Bearer authentication with configurable issuer, audience, and secret",
            "Authorize" => "Requires authentication for this controller or method",
            "AllowAnonymous" => "Allows unauthenticated access to this method",
            "ValidateModel" => "Adds FluentValidation auto-validation for request models",
            "ApiController" => "Marks this class as an API controller with automatic model binding",
            "Route" => "Configures the route prefix for this controller",
            _ => ""
        };

        return Ok(new BadgeInfoDto(effect.BadgeName, effect.NuGetPackages, desc));
    }
}
