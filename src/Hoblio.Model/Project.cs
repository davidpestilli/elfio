using System.Text.Json;

namespace Hoblio.Model;

public sealed class FlowProject
{
    public required FlowManifest Manifest { get; set; }
    public required FlowGraph Flow { get; set; }
    public Dictionary<string, JsonElement> Variables { get; set; } = new(StringComparer.Ordinal);
}

public sealed class FlowManifest
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? EntryFlow { get; init; }
    public Dictionary<string, ManifestFlowEntry>? Flows { get; init; }
}

public sealed class ManifestFlowEntry
{
    public required string Path { get; init; }
    public List<string>? Inputs { get; init; }
    public List<string>? Outputs { get; init; }
    public string? Description { get; init; }
}
