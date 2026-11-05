namespace Hoblio.Studio;

/// <summary>Spawn grid aligned with Hoblio Rust Studio (<c>spawnPosition</c> in useFlowEditor).</summary>
static class PaletteSpawn
{
    const int Cols = 4;
    const double ColWidth = 200;
    const double ControlBaseY = 240;
    const double ActionBaseY = 400;
    const double RowStep = 120;
    const double OriginX = 200;

    static int _controlIndex;
    static int _actionIndex;

    public static void Reset()
    {
        _controlIndex = 0;
        _actionIndex = 0;
    }

    public static (double X, double Y) Next(string paletteKey)
    {
        if (string.Equals(paletteKey, "node:end", StringComparison.Ordinal))
            return (720, 120);

        var control = IsControlKey(paletteKey);
        var idx = control ? _controlIndex++ : _actionIndex++;
        var col = idx % Cols;
        var row = idx / Cols;
        var baseY = control ? ControlBaseY : ActionBaseY;
        return (OriginX + col * ColWidth, baseY + row * RowStep);
    }

    static bool IsControlKey(string paletteKey) => paletteKey switch
    {
        "if" or "switch" or "case" or "case_default" or "for_each" or "while_loop" or "repeat" or "next_loop" or "exit_loop" => true,
        _ when paletteKey.StartsWith("node:", StringComparison.Ordinal) => false,
        _ when paletteKey.StartsWith("ui:", StringComparison.Ordinal) => false,
        _ when paletteKey.StartsWith("action:", StringComparison.Ordinal) => false,
        _ => false,
    };
}
