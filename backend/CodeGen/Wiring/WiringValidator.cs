using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ViDev.Api.CodeGen.Models;

namespace ViDev.Api.CodeGen.Wiring;

public enum WiringSeverity { Error, Warning, Info }

public sealed record WiringIssue(
    WiringSeverity Severity,
    string Code,
    string Message,
    string? NodeId,
    string? FieldId
);

public sealed record WiringValidationResult(
    bool IsValid,
    List<WiringIssue> Issues
);

public interface IWiringValidator
{
    WiringValidationResult Validate(string astJson);
}

public sealed class WiringValidator : IWiringValidator
{
    private readonly ILogger<WiringValidator> _logger;

    public WiringValidator(ILogger<WiringValidator> logger)
    {
        _logger = logger;
    }

    public WiringValidationResult Validate(string astJson)
    {
        var issues = new List<WiringIssue>();

        if (string.IsNullOrWhiteSpace(astJson))
        {
            return new WiringValidationResult(true, issues);
        }

        AstDocument? document = null;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            document = JsonSerializer.Deserialize<AstDocument>(astJson, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AST document for wiring validation");
            issues.Add(new WiringIssue(WiringSeverity.Error, "INVALID_AST", "Could not parse the AST document.", null, null));
            return new WiringValidationResult(false, issues);
        }

        if (document?.Nodes == null)
        {
            return new WiringValidationResult(true, issues);
        }

        var edges = document.Edges ?? new List<AstEdge>();
        var nodes = new Dictionary<string, JsonElement>();

        foreach (var node in document.Nodes)
        {
            if (node.TryGetProperty("id", out var idProp))
            {
                nodes[idProp.GetString()!] = node;
            }
        }

        var edgeSet = new HashSet<string>();
        var nodeLifetimes = new Dictionary<string, string>();
        var allSockets = new Dictionary<string, AstSocketModel>();
        var allPlugs = new Dictionary<string, AstPlugModel>();
        var nodeSockets = new Dictionary<string, List<string>>();

        var optionsModel = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var kvp in nodes)
        {
            var nodeId = kvp.Key;
            var nodeElement = kvp.Value;
            var t = nodeElement.TryGetProperty("type", out var tProp) ? tProp.GetString() : null;

            if (t == "Controller")
            {
                nodeLifetimes[nodeId] = "Transient";
            }
            else if (t == "Service")
            {
                nodeLifetimes[nodeId] = nodeElement.TryGetProperty("lifetime", out var lProp) ? lProp.GetString() ?? "Transient" : "Transient";
            }
            else
            {
                nodeLifetimes[nodeId] = "Transient";
            }

            nodeSockets[nodeId] = new List<string>();

            if (nodeElement.TryGetProperty("sockets", out var socketsProp) && socketsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in socketsProp.EnumerateArray())
                {
                    try {
                        var socket = s.Deserialize<AstSocketModel>(optionsModel);
                        if (socket != null && socket.Id != null)
                        {
                            allSockets[socket.Id] = socket;
                            nodeSockets[nodeId].Add(socket.Id);
                        }
                    } catch {}
                }
            }

            if (nodeElement.TryGetProperty("plugs", out var plugsProp) && plugsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in plugsProp.EnumerateArray())
                {
                    try {
                        var plug = p.Deserialize<AstPlugModel>(optionsModel);
                        if (plug != null && plug.Id != null)
                        {
                            allPlugs[plug.Id] = plug;
                        }
                    } catch {}
                }
            }
        }

        // Rule 4: ORPHAN_NODE
        var nodesWithEdges = new HashSet<string>();
        foreach (var edge in edges)
        {
            if (edge.Source != null) nodesWithEdges.Add(edge.Source);
            if (edge.Target != null) nodesWithEdges.Add(edge.Target);
        }

        foreach (var nodeId in nodes.Keys)
        {
            if (!nodesWithEdges.Contains(nodeId))
            {
                issues.Add(new WiringIssue(WiringSeverity.Warning, "ORPHAN_NODE", $"Node '{nodeId}' is not connected to any other node.", nodeId, null));
            }
        }

        var socketToIncomingEdge = new Dictionary<string, AstEdge>();
        var graph = new Dictionary<string, List<string>>();
        foreach (var nodeId in nodes.Keys)
        {
            graph[nodeId] = new List<string>();
        }

        foreach (var edge in edges)
        {
            if (edge.Source == null || edge.Target == null) continue;

            // Rule 5: DUPLICATE_EDGE
            var edgeKey = $"{edge.Source}->{edge.Target}:{edge.SourceHandle}->{edge.TargetHandle}";
            if (!edgeSet.Add(edgeKey))
            {
                issues.Add(new WiringIssue(WiringSeverity.Warning, "DUPLICATE_EDGE", $"Duplicate edge found between source '{edge.Source}' and target '{edge.Target}'.", edge.Source, null));
            }

            // Rule 6: MISSING_NODE_REF
            if (!nodes.ContainsKey(edge.Source))
            {
                issues.Add(new WiringIssue(WiringSeverity.Error, "MISSING_NODE_REF", $"Edge references missing source node '{edge.Source}'.", edge.Source, null));
                continue;
            }
            if (!nodes.ContainsKey(edge.Target))
            {
                issues.Add(new WiringIssue(WiringSeverity.Error, "MISSING_NODE_REF", $"Edge references missing target node '{edge.Target}'.", edge.Target, null));
                continue;
            }

            graph[edge.Source].Add(edge.Target);

            if (edge.TargetHandle != null)
            {
                if (!socketToIncomingEdge.ContainsKey(edge.TargetHandle))
                {
                    socketToIncomingEdge[edge.TargetHandle] = edge;
                }
            }

            // Rule 2: TYPE_MISMATCH
            if (edge.SourceHandle != null && edge.TargetHandle != null)
            {
                if (allPlugs.TryGetValue(edge.SourceHandle, out var plug) && allSockets.TryGetValue(edge.TargetHandle, out var socket))
                {
                    if (plug.DataType != socket.DataType)
                    {
                        issues.Add(new WiringIssue(WiringSeverity.Error, "TYPE_MISMATCH", $"Type mismatch: Plug provides '{plug.DataType}' but Socket requires '{socket.DataType}'.", edge.Target, edge.TargetHandle));
                    }
                }
            }

            // Rule 7: LIFETIME_VIOLATION
            var targetLifetime = nodeLifetimes.TryGetValue(edge.Target, out var tl) ? tl : "Transient";
            var sourceLifetime = nodeLifetimes.TryGetValue(edge.Source, out var sl) ? sl : "Transient";

            if (targetLifetime == "Singleton" && (sourceLifetime == "Scoped" || sourceLifetime == "Transient"))
            {
                issues.Add(new WiringIssue(WiringSeverity.Error, "LIFETIME_VIOLATION", $"Singleton node '{edge.Target}' cannot depend on {sourceLifetime} node '{edge.Source}'.", edge.Target, null));
            }
        }

        // Rule 1: UNCONNECTED_SOCKET
        foreach (var kvp in nodeSockets)
        {
            var nodeId = kvp.Key;
            foreach (var socketId in kvp.Value)
            {
                if (!socketToIncomingEdge.ContainsKey(socketId))
                {
                    issues.Add(new WiringIssue(WiringSeverity.Warning, "UNCONNECTED_SOCKET", $"Socket '{socketId}' has no incoming connection.", nodeId, socketId));
                }
            }
        }

        // Rule 3: CIRCULAR_DEPENDENCY
        var visited = new Dictionary<string, int>();
        foreach (var node in nodes.Keys)
        {
            visited[node] = 0;
        }

        bool DfsCycle(string node)
        {
            visited[node] = 1;
            foreach (var neighbor in graph[node])
            {
                if (visited[neighbor] == 1)
                {
                    issues.Add(new WiringIssue(WiringSeverity.Error, "CIRCULAR_DEPENDENCY", $"Circular dependency detected involving node '{neighbor}'.", neighbor, null));
                    return true;
                }
                if (visited[neighbor] == 0)
                {
                    if (DfsCycle(neighbor)) return true;
                }
            }
            visited[node] = 2;
            return false;
        }

        foreach (var node in nodes.Keys)
        {
            if (visited[node] == 0)
            {
                DfsCycle(node);
            }
        }

        var sortedIssues = issues.OrderBy(i => i.Severity).ToList();
        var isValid = !sortedIssues.Any(i => i.Severity == WiringSeverity.Error);

        return new WiringValidationResult(isValid, sortedIssues);
    }
}
