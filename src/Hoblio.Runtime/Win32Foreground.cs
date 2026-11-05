using System.Runtime.InteropServices;
using System.Text;

namespace Hoblio.Runtime;

static class Win32Foreground
{
    const int MaxTitle = 512;

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    /// <summary>Título da janela em primeiro plano se for Chrome/Edge (classe Chrome_WidgetWin_1).</summary>
    public static string? TryGetChromiumForegroundTitle()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return null;

        var className = new StringBuilder(256);
        _ = GetClassNameW(hwnd, className, className.Capacity);
        if (!className.ToString().Equals("Chrome_WidgetWin_1", StringComparison.Ordinal))
            return null;

        var title = new StringBuilder(MaxTitle);
        if (GetWindowTextW(hwnd, title, MaxTitle) <= 0)
            return null;

        var raw = title.ToString().Trim();
        return string.IsNullOrEmpty(raw) ? null : raw;
    }

    /// <summary>Remove sufixos típicos « - Google Chrome » / « - Microsoft Edge ».</summary>
    public static string NormalizeBrowserWindowTitle(string windowTitle)
    {
        var t = windowTitle.Trim();
        foreach (var suffix in new[]
        {
            " - Google Chrome",
            " - Microsoft Edge",
            " — Google Chrome",
        })
        {
            if (t.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return t[..^suffix.Length].Trim();
        }
        return t;
    }
}
