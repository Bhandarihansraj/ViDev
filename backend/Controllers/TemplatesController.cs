using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ViDev.Api.Dtos;
using ViDev.Api.Services;

namespace ViDev.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;

    public TemplatesController(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    /// <summary>
    /// Creates a new template.
    /// </summary>
    /// <param name="request">The template data.</param>
    /// <returns>The created template.</returns>
    [HttpPost]
    public async Task<ActionResult<TemplateResponse>> Create([FromBody] CreateTemplateRequest request)
    {
        // TODO: Replace with authenticated user ID from Day 5
        var tempAuthorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var response = await _templateService.CreateAsync(request, tempAuthorId);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets a paginated list of templates, optionally filtered by tag.
    /// </summary>
    /// <param name="tag">Optional tag to filter by.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 50).</param>
    /// <returns>A list of templates.</returns>
    [HttpGet]
    public async Task<ActionResult<List<TemplateListResponse>>> GetAll([FromQuery] string? tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var response = await _templateService.GetAllAsync(tag, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Gets a single template by ID.
    /// </summary>
    /// <param name="id">The template ID.</param>
    /// <returns>The template with full AST JSON.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<TemplateResponse>> GetById(Guid id)
    {
        var response = await _templateService.GetByIdAsync(id);
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }
}
