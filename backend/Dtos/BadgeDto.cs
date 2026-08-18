using System.Collections.Generic;

namespace ViDev.Api.Dtos;

public sealed record BadgeInfoDto(
    string Name,
    List<string> NuGetPackages,
    string Description
);
