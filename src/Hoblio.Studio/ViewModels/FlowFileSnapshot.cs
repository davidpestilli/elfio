using System.Text.Json;
using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed class FlowFileSnapshot
{
    public required FlowGraph Graph { get; set; }
    public Dictionary<string, JsonElement> Variables { get; set; } = new(StringComparer.Ordinal);
}
