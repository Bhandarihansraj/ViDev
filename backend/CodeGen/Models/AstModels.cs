using System.Text.Json.Serialization;

namespace ViDev.Api.CodeGen.Models;

public sealed record AstControllerNode(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("annotations")] List<string> Annotations,
    [property: JsonPropertyName("routePrefix")] string RoutePrefix,
    [property: JsonPropertyName("methods")] List<AstMethodModel> Methods,
    [property: JsonPropertyName("sockets")] List<AstSocketModel> Sockets
);

public sealed record AstMethodModel(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("verb")] string? Verb,
    [property: JsonPropertyName("route")] string? Route,
    [property: JsonPropertyName("annotations")] List<string> Annotations,
    [property: JsonPropertyName("parameters")] List<AstParameterModel> Parameters,
    [property: JsonPropertyName("returnType")] string ReturnType,
    [property: JsonPropertyName("body")] List<AstBodyStatement> Body
);

public sealed record AstParameterModel(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("fromBody")] bool FromBody = false,
    [property: JsonPropertyName("fromRoute")] bool FromRoute = false,
    [property: JsonPropertyName("fromQuery")] bool FromQuery = false
);

public sealed record AstBodyStatement(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("service")] string? Service = null,
    [property: JsonPropertyName("method")] string? Method = null,
    [property: JsonPropertyName("value")] string? Value = null
);

public sealed record AstSocketModel(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("dataType")] string DataType,
    [property: JsonPropertyName("targetField")] string TargetField
);
