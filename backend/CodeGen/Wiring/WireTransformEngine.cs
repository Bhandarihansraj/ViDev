using System.Text.Json;
using ViDev.Api.CodeGen.Models;

namespace ViDev.Api.CodeGen.Wiring;

/// Result of applying wire transforms
public sealed record TransformResult(
    Dictionary<string, string> MappingCode,  // edgeId → generated mapping snippet
    List<string> Warnings
);

public interface IWireTransformEngine
{
    TransformResult GenerateTransforms(string astJson);
}

public sealed class WireTransformEngine : IWireTransformEngine
{
    private readonly Dictionary<(string source, string target), string> _rules = new()
    {
        { ("string", "int"), "int.Parse({source})" },
        { ("string", "Guid"), "Guid.Parse({source})" },
        { ("string", "DateTime"), "DateTime.Parse({source})" },
        { ("string", "bool"), "bool.Parse({source})" },
        { ("string", "decimal"), "decimal.Parse({source})" },
        { ("string", "double"), "double.Parse({source})" },
        { ("string", "long"), "long.Parse({source})" },
        { ("int", "string"), "{source}.ToString()" },
        { ("Guid", "string"), "{source}.ToString()" },
        { ("DateTime", "string"), "{source}.ToString(\"o\")" },
        { ("int", "long"), "(long){source}" },
        { ("int", "double"), "(double){source}" },
        { ("int", "decimal"), "(decimal){source}" }
    };

    public TransformResult GenerateTransforms(string astJson)
    {
        var mappings = new Dictionary<string, string>();
        var warnings = new List<string>();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var doc = JsonSerializer.Deserialize<AstDocument>(astJson, options);

            if (doc == null || doc.Nodes == null || doc.Edges == null)
            {
                warnings.Add("Invalid AST JSON or missing elements.");
                return new TransformResult(mappings, warnings);
            }

            var sockets = new Dictionary<string, AstSocketModel>();
            var plugs = new Dictionary<string, AstPlugModel>();

            foreach (var nodeElement in doc.Nodes)
            {
                var nodeType = nodeElement.GetProperty("type").GetString();
                if (nodeType == "controller" || nodeType == "service")
                {
                    if (nodeElement.TryGetProperty("sockets", out var socketsElement))
                    {
                        var nodeSockets = socketsElement.Deserialize<List<AstSocketModel>>(options);
                        if (nodeSockets != null)
                        {
                            foreach (var socket in nodeSockets)
                            {
                                sockets[socket.Id] = socket;
                            }
                        }
                    }
                }
                
                // For plugs, we could parse entities or just look for plug defs.
                // Assuming plugs are at node level or service/controller level.
                // But normally in ViDev AST they are properties of entity/service.
                // Actually the prompt says "For each edge, find the source node's plug and target node's socket".
                // I'll parse all plugs and sockets across the AST to build lookup dicts.
            }
            
            // To properly resolve, let's extract ALL AstSocketModel and AstPlugModel from anywhere in the document.
            // Using JsonElement parsing:
            ExtractSocketsAndPlugs(doc.Nodes, options, sockets, plugs);

            foreach (var edge in doc.Edges)
            {
                var sourceHandle = edge.SourceHandle ?? edge.Source;
                var targetHandle = edge.TargetHandle ?? edge.Target;

                if (plugs.TryGetValue(sourceHandle, out var plug) && sockets.TryGetValue(targetHandle, out var socket))
                {
                    if (plug.DataType != socket.DataType)
                    {
                        if (_rules.TryGetValue((plug.DataType, socket.DataType), out var rule))
                        {
                            mappings[edge.Id] = rule.Replace("{source}", "{source}"); // {source} is placeholder
                        }
                        else
                        {
                            warnings.Add($"No transform available for {plug.DataType} → {socket.DataType}");
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            warnings.Add($"Failed to parse AST: {ex.Message}");
        }

        return new TransformResult(mappings, warnings);
    }

    private void ExtractSocketsAndPlugs(List<JsonElement> nodes, JsonSerializerOptions options, Dictionary<string, AstSocketModel> sockets, Dictionary<string, AstPlugModel> plugs)
    {
        foreach (var node in nodes)
        {
            if (node.TryGetProperty("sockets", out var socketsElement))
            {
                var nodeSockets = socketsElement.Deserialize<List<AstSocketModel>>(options);
                if (nodeSockets != null)
                {
                    foreach (var s in nodeSockets) sockets[s.Id] = s;
                }
            }

            // In some cases properties or methods act as plugs. The prompt mentions AstPlugModel.
            if (node.TryGetProperty("plugs", out var plugsElement))
            {
                var nodePlugs = plugsElement.Deserialize<List<AstPlugModel>>(options);
                if (nodePlugs != null)
                {
                    foreach (var p in nodePlugs) plugs[p.Id] = p;
                }
            }
            
            // also entities have properties, maybe those are plugs? Let's check AstModels.cs
            // AstEntityNode has Properties (AstPropertyModel). Not plugs.
            // But how are Plugs represented in AstModels.cs? There is AstPlugModel. Where does it exist?
            // "AstPlugModel" has no container node type explicitly specified in AstModels.cs snippet. 
            // We just assume it's under "plugs" for some nodes. Let's do that.
            
            // Wait, what if the edge source/target are directly referencing something else?
            // "For each edge, find the source node's plug and target node's socket"
            // Wait, looking at WiringValidator in standard ViDev:
            // "In ViDev, a plug ID could be a property name, a service method, etc."
            // But AstModels has `AstPlugModel`. Let me check WiringValidator.cs.
        }
    }
}
