using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Hoblio.Model;

#pragma warning disable CA1416

namespace Hoblio.Runtime;

public sealed class WordContext : IDisposable
{
    readonly Dictionary<string, WordSession> _sessions = new(StringComparer.Ordinal);

    const int WdStatisticPages = 2;
    const int WdGoToPage = 1;
    const int WdGoToAbsolute = 1;
    const int WdCollapseEnd = 0;
    const int WdCollapseStart = 1;
    const int WdReplaceOne = 1;
    const int WdReplaceAll = 2;
    const int WdFindStop = 0;

    public void Dispose()
    {
        foreach (var session in _sessions.Values)
            session.Dispose();
        _sessions.Clear();
    }

    public string Execute(WordAction action, RunVariables vars, string? projectRoot)
    {
        return action.Type switch
        {
            WordActionTypes.Launch => Launch(action, vars, projectRoot),
            WordActionTypes.Attach => Attach(action, vars),
            WordActionTypes.Save => Save(action, vars, projectRoot),
            WordActionTypes.Close => Close(action, vars, projectRoot),
            WordActionTypes.Read => Read(action, vars),
            WordActionTypes.Write => Write(action, vars),
            WordActionTypes.InsertImage => InsertImage(action, vars, projectRoot),
            WordActionTypes.FindReplace => FindReplace(action, vars),
            _ => throw new NotImplementedException(
                $"Word action `{WordActionTypes.ModuleActionId(action.Type)}` ainda não implementada no runtime."),
        };
    }

    string Launch(WordAction action, RunVariables vars, string? projectRoot)
    {
        var instanceVar = RequireInstanceVar(action);
        EnsureWordInstalled();
        var visible = action.Visible ?? true;
        dynamic app = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application")!)
            ?? throw new InvalidOperationException("Microsoft Word não está instalado ou registrado.");
        app.Visible = visible;
        app.DisplayAlerts = 0;

        dynamic doc;
        var path = action.Path is null ? null : RunVariables.Interpolate(action.Path, vars);
        if (!string.IsNullOrWhiteSpace(path))
        {
            path = ResolvePath(path, projectRoot);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Documento Word não encontrado: `{path}`.");
            doc = app.Documents.Open(
                path,
                ReadOnly: action.ReadOnly ?? false,
                Visible: visible);
        }
        else
            doc = app.Documents.Add();

        BindSession(instanceVar, WordSession.Attach(app, doc), vars);
        return $"word_launch: instance `{instanceVar}` visible={visible}";
    }

    string Attach(WordAction action, RunVariables vars)
    {
        var instanceVar = RequireInstanceVar(action);
        var docName = action.DocumentName is null ? null : RunVariables.Interpolate(action.DocumentName, vars);
        var session = TryAttachDocument(docName)
            ?? throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(docName)
                    ? "Nenhuma instância Word em execução encontrada."
                    : $"Nenhum documento Word corresponde a `{docName}`.");
        BindSession(instanceVar, session, vars);
        return $"word_attach: instance `{instanceVar}`";
    }

    string Save(WordAction action, RunVariables vars, string? projectRoot)
    {
        var session = RequireSession(action, vars);
        dynamic doc = session.Document;
        doc.Activate();
        var prevAlerts = (int)session.App.DisplayAlerts;
        session.App.DisplayAlerts = 0;
        try
        {
            if (!string.IsNullOrWhiteSpace(action.Path))
            {
                var path = ResolvePath(RunVariables.Interpolate(action.Path!, vars), projectRoot);
                var format = SaveFormatFromPath(path);
                doc.SaveAs2(path, format);
                return $"word_save: SaveAs `{path}`";
            }

            if (string.IsNullOrEmpty((string)doc.Path))
                throw new InvalidOperationException("Documento ainda não foi salvo; informe `path` para Salvar como.");
            doc.Save();
            return "word_save: saved";
        }
        finally
        {
            session.App.DisplayAlerts = prevAlerts;
        }
    }

    string Close(WordAction action, RunVariables vars, string? projectRoot)
    {
        var instanceVar = action.Instance.Trim();
        var session = RequireSession(action, vars);
        var mode = (action.Mode ?? WordEnums.CloseMode.Close).Trim().ToLowerInvariant();
        if (mode is WordEnums.CloseMode.Save or WordEnums.CloseMode.SaveAs)
        {
            var saveAction = new WordAction
            {
                Operation = WordActionTypes.Save,
                Instance = action.Instance,
                Path = mode == WordEnums.CloseMode.SaveAs ? action.Path : null,
            };
            Save(saveAction, vars, projectRoot);
        }

        dynamic doc = session.Document;
        dynamic app = session.App;
        doc.Close(SaveChanges: false);
        ReleaseCom(doc);
        _sessions.Remove(session.Id);
        vars.Remove(instanceVar);

        if ((int)app.Documents.Count == 0)
        {
            app.DisplayAlerts = 0;
            app.Quit(SaveChanges: false);
            ReleaseCom(app);
        }

        return $"word_close: `{instanceVar}` mode={mode}";
    }

    string Read(WordAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var output = RequireOutput(action);
        var mode = (action.Mode ?? WordEnums.ReadMode.Document).Trim().ToLowerInvariant();
        var text = mode switch
        {
            WordEnums.ReadMode.Pages => ReadByPages(session, action, vars),
            WordEnums.ReadMode.Bookmark => ReadByBookmark(session, action, vars),
            _ => ReadWholeDocument(session),
        };
        vars[output] = text;
        return $"word_read: `{output}` = {Truncate(text, 80)}";
    }

    string Write(WordAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        EnsureWritable(session);
        var text = RunVariables.Interpolate(action.Value ?? "", vars);
        if (action.AppendNewLine == true)
            text += "\r";
        var mode = (action.Mode ?? WordEnums.WriteMode.End).Trim().ToLowerInvariant();
        dynamic doc = session.Document;
        switch (mode)
        {
            case WordEnums.WriteMode.Start:
                doc.Range(0, 0).Text = text;
                break;
            case WordEnums.WriteMode.End:
                doc.Range().InsertAfter(text);
                break;
            case WordEnums.WriteMode.BookmarkStart:
            case WordEnums.WriteMode.BookmarkEnd:
                WriteAtBookmark(doc, action, vars, text, atStart: mode == WordEnums.WriteMode.BookmarkStart);
                break;
            default:
                throw new InvalidOperationException($"Modo de escrita Word inválido: `{mode}`.");
        }

        return $"word_write: {text.Length} chars mode={mode}";
    }

    string InsertImage(WordAction action, RunVariables vars, string? projectRoot)
    {
        var session = RequireSession(action, vars);
        EnsureWritable(session);
        var mode = (action.Mode ?? WordEnums.InsertImageMode.DocumentEnd).Trim().ToLowerInvariant();
        var source = (action.ImageSource ?? WordEnums.ImageSource.File).Trim().ToLowerInvariant();
        var imagePath = ResolveImagePath(action, vars, projectRoot, source);
        try
        {
            dynamic doc = session.Document;
            dynamic range = mode switch
            {
                WordEnums.InsertImageMode.DocumentStart => doc.Range(0, 0),
                WordEnums.InsertImageMode.DocumentEnd => doc.Range(),
                WordEnums.InsertImageMode.BookmarkStart or WordEnums.InsertImageMode.BookmarkEnd
                    => BookmarkRange(doc, action, vars, atStart: mode == WordEnums.InsertImageMode.BookmarkStart),
                WordEnums.InsertImageMode.TextStart or WordEnums.InsertImageMode.TextEnd
                    => FindTextRange(doc, action, vars, before: mode == WordEnums.InsertImageMode.TextStart),
                _ => throw new InvalidOperationException($"Modo de imagem Word inválido: `{mode}`."),
            };
            if (mode is WordEnums.InsertImageMode.DocumentEnd or WordEnums.InsertImageMode.BookmarkEnd
                or WordEnums.InsertImageMode.TextEnd)
                range.Collapse(WdCollapseEnd);
            else
                range.Collapse(WdCollapseStart);

            range.InlineShapes.AddPicture(imagePath);
            return $"word_insert_image: mode={mode} source={source}";
        }
        finally
        {
            if (source == WordEnums.ImageSource.Clipboard && File.Exists(imagePath))
            {
                try { File.Delete(imagePath); } catch { /* temp cleanup */ }
            }
        }
    }

    string FindReplace(WordAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var findText = RunVariables.Interpolate(action.FindText ?? "", vars);
        if (string.IsNullOrEmpty(findText))
            return "word_find_replace: find_text vazio";
        var replaceText = RunVariables.Interpolate(action.ReplaceText ?? "", vars);
        dynamic doc = session.Document;
        doc.Activate();
        dynamic find = doc.Range().Find;
        find.ClearFormatting();
        find.Replacement.ClearFormatting();
        find.Text = findText;
        find.Replacement.Text = replaceText;
        find.MatchWildcards = action.MatchWildcards ?? false;
        find.MatchCase = action.MatchCase ?? false;
        find.MatchWholeWord = action.MatchEntireWord ?? false;
        find.Forward = true;
        find.Wrap = WdFindStop;
        var replaceMode = action.AllMatches == false ? WdReplaceOne : WdReplaceAll;
        find.Execute(Replace: replaceMode);
        return $"word_find_replace: all={action.AllMatches ?? true}";
    }

    static string ReadWholeDocument(WordSession session) =>
        ((string)session.Document.Range().Text).TrimEnd('\r', '\a');

    /// <summary>Interpreta especificação estilo "1, 3-5, 7" (mesma gramática de produto, parser próprio).</summary>
    static IReadOnlyList<string> ParsePageRangeSpec(string pagesSpec)
    {
        var tokens = new List<string>();
        foreach (var segment in pagesSpec.Split(','))
        {
            var token = segment.Trim();
            if (token.Length == 0)
                continue;

            var dash = token.IndexOf('-', StringComparison.Ordinal);
            if (dash < 0)
            {
                if (!IsPageNumberToken(token))
                    throw new InvalidOperationException($"Formato de páginas inválido: `{pagesSpec}`.");
                tokens.Add(token);
                continue;
            }

            var left = token[..dash].Trim();
            var right = token[(dash + 1)..].Trim();
            if (left.Length == 0 || right.Length == 0 || token.IndexOf('-', dash + 1) >= 0
                || !IsPageNumberToken(left) || !IsPageNumberToken(right))
                throw new InvalidOperationException($"Formato de páginas inválido: `{pagesSpec}`.");
            tokens.Add($"{left}-{right}");
        }

        if (tokens.Count == 0)
            throw new InvalidOperationException($"Formato de páginas inválido: `{pagesSpec}`.");
        return tokens;
    }

    static bool IsPageNumberToken(string token)
    {
        if (token.Length == 0)
            return false;
        foreach (var ch in token)
        {
            if (!char.IsAsciiDigit(ch))
                return false;
        }
        return true;
    }

    string ReadByPages(WordSession session, WordAction action, RunVariables vars)
    {
        var pagesSpec = RunVariables.Interpolate(action.Pages ?? "", vars);
        if (string.IsNullOrWhiteSpace(pagesSpec))
            throw new InvalidOperationException("word_read: informe `pages` (ex.: 1-3,5).");
        var pageTokens = ParsePageRangeSpec(pagesSpec);

        dynamic doc = session.Document;
        var pageCount = (int)doc.ComputeStatistics(WdStatisticPages);
        var sb = new StringBuilder();
        foreach (var token in pageTokens)
        {
            if (token.Contains('-', StringComparison.Ordinal))
            {
                var parts = token.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var from = int.Parse(parts[0], CultureInfo.InvariantCulture);
                var to = int.Parse(parts[1], CultureInfo.InvariantCulture);
                if (from > to)
                    (from, to) = (to, from);
                for (var p = from; p <= to; p++)
                    AppendPageText(doc, pageCount, p, sb);
            }
            else
            {
                var page = int.Parse(token, CultureInfo.InvariantCulture);
                AppendPageText(doc, pageCount, page, sb);
            }
        }

        return sb.ToString().TrimEnd('\r', '\a');
    }

    static void AppendPageText(dynamic doc, int pageCount, int page, StringBuilder sb)
    {
        if (page < 1 || page > pageCount)
            throw new InvalidOperationException($"Página {page} fora do intervalo (1-{pageCount}).");
        sb.Append(ExtractPageText(doc, page));
    }

    static string ExtractPageText(dynamic doc, int page)
    {
        dynamic start = doc.GoTo(WdGoToPage, WdGoToAbsolute, Count: page);
        dynamic end = page < (int)doc.ComputeStatistics(WdStatisticPages)
            ? doc.GoTo(WdGoToPage, WdGoToAbsolute, Count: page + 1)
            : doc.Range();
        var startPos = (int)start.Start;
        var endPos = page < (int)doc.ComputeStatistics(WdStatisticPages) ? (int)end.Start : (int)end.End;
        return ((string)doc.Range(startPos, endPos).Text).TrimEnd('\r', '\a');
    }

    static string ReadByBookmark(WordSession session, WordAction action, RunVariables vars)
    {
        var name = RunVariables.Interpolate(action.Bookmark ?? "", vars);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("word_read: informe `bookmark`.");
        dynamic doc = session.Document;
        if (!doc.Bookmarks.Exists(name))
            throw new InvalidOperationException($"Marcador não encontrado: `{name}`.");
        return ((string)doc.Bookmarks[name].Range.Text).TrimEnd('\r', '\a');
    }

    static void WriteAtBookmark(dynamic doc, WordAction action, RunVariables vars, string text, bool atStart)
    {
        var name = RunVariables.Interpolate(action.Bookmark ?? "", vars);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("word_write: informe `bookmark`.");
        if (!doc.Bookmarks.Exists(name))
            throw new InvalidOperationException($"Marcador não encontrado: `{name}`.");
        dynamic bm = doc.Bookmarks[name];
        if (bm.Empty)
        {
            doc.Range(bm.Start, bm.Start).Text = text;
            return;
        }

        dynamic range = atStart
            ? doc.Range(bm.Start, bm.Start)
            : bm.Range;
        if (atStart)
            range.Text = text;
        else
        {
            range.Collapse(WdCollapseEnd);
            range.InsertAfter(text);
        }
    }

    static dynamic BookmarkRange(dynamic doc, WordAction action, RunVariables vars, bool atStart)
    {
        var name = RunVariables.Interpolate(action.Bookmark ?? "", vars);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("word_insert_image: informe `bookmark`.");
        if (!doc.Bookmarks.Exists(name))
            throw new InvalidOperationException($"Marcador não encontrado: `{name}`.");
        dynamic bm = doc.Bookmarks[name];
        if (bm.Empty)
            return doc.Range(bm.Start, bm.Start);
        return atStart ? doc.Range(bm.Start, bm.Start) : bm.Range;
    }

    static dynamic FindTextRange(dynamic doc, WordAction action, RunVariables vars, bool before)
    {
        var text = RunVariables.Interpolate(action.TextToFind ?? "", vars);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("word_insert_image: informe `text_to_find`.");
        dynamic searchRange = doc.Content;
        searchRange.Find.ClearFormatting();
        searchRange.Find.Text = text;
        searchRange.Find.Forward = true;
        searchRange.Find.Wrap = WdFindStop;
        if (!(bool)searchRange.Find.Execute())
            throw new InvalidOperationException($"Texto não encontrado: `{text}`.");
        return before
            ? doc.Range((int)searchRange.Start, (int)searchRange.Start)
            : searchRange;
    }

    static string ResolveImagePath(WordAction action, RunVariables vars, string? projectRoot, string source)
    {
        if (source == WordEnums.ImageSource.Clipboard)
        {
            if (!Clipboard.ContainsImage())
                throw new InvalidOperationException("Área de transferência não contém imagem.");
            var image = Clipboard.GetImage();
            if (image is null)
                throw new InvalidOperationException("Área de transferência não contém imagem.");
            var temp = Path.Combine(Path.GetTempPath(), $"elfio-word-{Guid.NewGuid():N}.png");
            SaveBitmapSource(image, temp);
            return temp;
        }

        var path = RunVariables.Interpolate(action.Path ?? "", vars);
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("word_insert_image: informe `path` da imagem.");
        path = ResolvePath(path, projectRoot);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Imagem não encontrada: `{path}`.");
        return path;
    }

    WordSession? TryAttachDocument(string? documentName)
    {
        WordSession? TryFromApp(dynamic app)
        {
            if (app is null)
                return null;
            try
            {
                if (string.IsNullOrWhiteSpace(documentName))
                {
                    if ((int)app.Documents.Count == 0)
                        return null;
                    dynamic doc = app.ActiveDocument;
                    return WordSession.Attach(app, doc);
                }

                var rooted = Path.IsPathRooted(documentName);
                var fileStem = rooted ? "" : Path.GetFileNameWithoutExtension(documentName);
                foreach (dynamic doc in app.Documents)
                {
                    var full = (string)doc.FullName;
                    if (rooted && string.Equals(full, documentName, StringComparison.OrdinalIgnoreCase))
                    {
                        doc.Activate();
                        return WordSession.Attach(app, doc);
                    }

                    var stem = Path.GetFileNameWithoutExtension(full);
                    if (string.Equals(full, documentName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(stem, documentName, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrEmpty(fileStem) && string.Equals(stem, fileStem, StringComparison.OrdinalIgnoreCase)))
                    {
                        doc.Activate();
                        return WordSession.Attach(app, doc);
                    }
                }
            }
            catch
            {
                // try next process
            }

            return null;
        }

        try
        {
            var active = ComActiveObject.Get("Word.Application");
            var fromActive = TryFromApp(active);
            if (fromActive is not null)
                return fromActive;
        }
        catch
        {
            // fall through to WINWORD scan
        }

        foreach (var process in Process.GetProcessesByName("WINWORD"))
        {
            try
            {
                var hwnd = process.MainWindowHandle;
                if (hwnd == IntPtr.Zero)
                    continue;
                if (!WordComInterop.TryGetApplication(hwnd, out var app) || app is null)
                    continue;
                var session = TryFromApp(app);
                if (session is not null)
                    return session;
            }
            catch
            {
                // next process
            }
        }

        return null;
    }

    void BindSession(string instanceVar, WordSession session, RunVariables vars)
    {
        _sessions[session.Id] = session;
        vars[instanceVar] = session.Id;
    }

    WordSession RequireSession(WordAction action, RunVariables vars) =>
        RequireSessionForVariable(action.Instance.Trim(), vars);

    WordSession RequireSessionForVariable(string instanceVar, RunVariables vars)
    {
        if (string.IsNullOrEmpty(instanceVar))
            throw new InvalidOperationException("Word: variável de instância vazia.");
        if (!vars.TryGetValue(instanceVar, out var sessionId) || string.IsNullOrWhiteSpace(sessionId))
            throw new InvalidOperationException($"Variável `{instanceVar}` não contém sessão Word. Use Iniciar ou Anexar antes.");
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new InvalidOperationException($"Sessão Word `{sessionId}` expirou ou não existe.");
        return session;
    }

    static string RequireInstanceVar(WordAction action)
    {
        var name = action.Instance.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Word: variável de instância vazia.");
        return name;
    }

    static string RequireOutput(WordAction action)
    {
        var name = action.OutputVariable?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Word: `output_variable` é obrigatória.");
        return name;
    }

    static void EnsureWordInstalled()
    {
        if (Type.GetTypeFromProgID("Word.Application") is null)
            throw new InvalidOperationException("Microsoft Word não está instalado ou registrado.");
    }

    static void EnsureWritable(WordSession session)
    {
        if ((bool)session.Document.ReadOnly)
            throw new InvalidOperationException($"Documento `{session.Document.Name}` está somente leitura.");
    }

    static int SaveFormatFromPath(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".doc" => 0,
            ".docx" => 16,
            ".pdf" => 17,
            ".rtf" => 6,
            ".txt" => 4,
            _ => 16,
        };

    static string ResolvePath(string path, string? projectRoot)
    {
        if (Path.IsPathRooted(path) || string.IsNullOrWhiteSpace(projectRoot))
            return path;
        return Path.GetFullPath(Path.Combine(projectRoot, path));
    }

    static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";

    static void ReleaseCom(object? com)
    {
        if (com is null)
            return;
        try { Marshal.ReleaseComObject(com); } catch { /* ignore */ }
    }

    static void SaveBitmapSource(BitmapSource source, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    sealed class WordSession : IDisposable
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public dynamic App { get; }
        public dynamic Document { get; }

        WordSession(dynamic app, dynamic document)
        {
            App = app;
            Document = document;
        }

        public static WordSession Attach(dynamic app, dynamic document) => new(app, document);

        public void Dispose()
        {
            try
            {
                if (Document is not null)
                    Marshal.ReleaseComObject(Document);
            }
            catch { /* ignore */ }

            try
            {
                if (App is not null)
                    Marshal.ReleaseComObject(App);
            }
            catch { /* ignore */ }
        }
    }

    static class ComActiveObject
    {
        public static object Get(string progId)
        {
            var type = Type.GetTypeFromProgID(progId)
                ?? throw new InvalidOperationException($"ProgID não registrado: {progId}");
            var clsid = type.GUID;
            GetActiveObject(ref clsid, IntPtr.Zero, out var obj);
            return obj;
        }

        [DllImport("oleaut32.dll", PreserveSig = false)]
        static extern void GetActiveObject(
            ref Guid rclsid,
            IntPtr pvReserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
    }
}

static class WordComInterop
{
    const uint ObjidNativeOm = 0xFFFFFFF0;
    static readonly Guid IID_IDispatch = new("00020400-0000-0000-C000-000000000046");

    public static bool TryGetApplication(IntPtr hwnd, out object? app)
    {
        app = null;
        if (hwnd == IntPtr.Zero)
            return false;
        try
        {
            var iid = IID_IDispatch;
            var hr = AccessibleObjectFromWindow(hwnd, ObjidNativeOm, ref iid, out var obj);
            if (hr != 0 || obj is null)
                return false;
            app = obj;
            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("oleacc.dll")]
    static extern int AccessibleObjectFromWindow(
        IntPtr hwnd,
        uint dwObjectId,
        ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object? ppvObject);
}
