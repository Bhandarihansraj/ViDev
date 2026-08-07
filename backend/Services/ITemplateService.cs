using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViDev.Api.Dtos;

namespace ViDev.Api.Services;

public interface ITemplateService
{
    Task<TemplateResponse> CreateAsync(CreateTemplateRequest request, Guid authorId);
    Task<List<TemplateListResponse>> GetAllAsync(string? tagFilter, int page, int pageSize);
    Task<TemplateResponse?> GetByIdAsync(Guid id);
}
