using System.Windows.Media;

namespace Hoblio.Studio;

/// <summary>Cores e ícones da paleta alinhados ao canvas (flow-canvas/styles.css).</summary>
public static class PaletteVisuals
{
    public sealed record Visual(string Glyph, Color Accent);

    public static Visual ForKey(string key)
    {
        var k = key.Trim();
        if (k.StartsWith("node:", StringComparison.OrdinalIgnoreCase))
            k = k["node:".Length..];
        if (k.StartsWith("action:", StringComparison.OrdinalIgnoreCase))
            k = k["action:".Length..];
        if (k.StartsWith("ui:", StringComparison.OrdinalIgnoreCase))
            k = k["ui:".Length..];

        return k.ToLowerInvariant() switch
        {
            "start" => new Visual("▶", Parse("#059669")),
            "end" => new Visual("■", Parse("#DC2626")),
            "if" => new Visual("?", Parse("#7C3AED")),
            "switch" => new Visual("⇄", Parse("#7C3AED")),
            "case" => new Visual("⎇", Parse("#7C3AED")),
            "case_default" => new Visual("↓", Parse("#7C3AED")),
            "for_each" or "while_loop" or "repeat" => new Visual("↻", Parse("#0D9488")),
            "next_loop" or "exit_loop" => new Visual("⤴", Parse("#D97706")),
            "log" => new Visual("≡", Parse("#64748B")),
            "no_op" => new Visual("○", Parse("#94A3B8")),
            "delay" => new Visual("⏱", Parse("#64748B")),
            "http_get" => new Visual("⇄", Parse("#2563EB")),
            "show_message" or "show_get_user_input" or "show_select_from_list" => new Visual("💬", Parse("#8B5CF6")),
            "file_write" or "file_append" or "file_read" => new Visual("📄", Parse("#475569")),
            "browser_navigate" or "browser_click" or "browser_wait_navigation" or "browser_fill"
                or "browser_wait_for" or "browser_get_text" => new Visual("🌐", Parse("#2563EB")),
            "set_variable" or "increase_variable" or "decrease_variable" or "list_clear" or "list_append"
                => new Visual("{}", Parse("#0F766E")),
            "run_flow" => new Visual("⎇", Parse("#4F46E5")),
            "create_subflow" => new Visual("+", Parse("#4F46E5")),
            _ when k.StartsWith("excel_", StringComparison.Ordinal) => new Visual("📊", Parse("#217346")),
            _ => new Visual("•", Parse("#CBD5E1")),
        };
    }

    public static Brush AccentBrush(string key)
    {
        var brush = new SolidColorBrush(ForKey(key).Accent);
        brush.Freeze();
        return brush;
    }

    static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;
}
