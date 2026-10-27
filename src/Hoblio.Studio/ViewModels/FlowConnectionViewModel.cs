using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed class FlowConnectionViewModel
{
    public required FlowNodeViewModel Source { get; init; }
    public required FlowNodeViewModel Target { get; init; }
    public EdgePort Port { get; set; } = EdgePort.Main;
}
