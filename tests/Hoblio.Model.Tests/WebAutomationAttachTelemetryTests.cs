using Hoblio.Model;

namespace Hoblio.Model.Tests;

public sealed class WebAutomationAttachTelemetryTests
{
    [Fact]
    public void Attach_chrome_telemetry_by_mode()
    {
        var byUrl = new WebAutomationAction
        {
            Operation = WebAutomationActionTypes.AttachChrome,
            AttachMode = WebAutomationEnums.AttachMode.ByUrl,
        };
        Assert.Equal("AttachToChromeByUrl", WebAutomationActionTypes.TelemetryActionId(byUrl));
        Assert.Equal("AttachToChromeByUrl", ActionCatalog.Describe(byUrl).ActionId);

        var fg = new WebAutomationAction
        {
            Operation = WebAutomationActionTypes.AttachChrome,
            AttachMode = WebAutomationEnums.AttachMode.Foreground,
        };
        Assert.Equal("AttachToForegroundChrome", WebAutomationActionTypes.TelemetryActionId(fg));
    }
}
