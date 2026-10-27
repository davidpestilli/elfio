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
}
