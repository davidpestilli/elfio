using System.Globalization;
using System.Text;
using System.Text.Json;
using Hoblio.Model;
using Hoblio.Model.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Writer;

namespace Hoblio.Runtime;

public sealed class PdfContext : IDisposable
{
    public string Execute(PdfAction action, RunVariables vars, string? projectRoot) =>
        action.Operation switch
        {
            PdfActionTypes.ExtractText => ExtractText(action, vars, projectRoot),
            PdfActionTypes.ExtractTables => ExtractTables(action, vars, projectRoot),
            PdfActionTypes.ExtractImages => ExtractImages(action, vars, projectRoot),
            PdfActionTypes.ExtractPages => ExtractPagesToFile(action, vars, projectRoot),
            PdfActionTypes.Merge => MergeFiles(action, vars, projectRoot),
            _ => throw new InvalidOperationException($"unknown pdf action `{action.Operation}`"),
        };

    static PdfDocument OpenPdf(string fullPath, string? password) =>
        string.IsNullOrEmpty(password)
            ? PdfDocument.Open(fullPath)
            : PdfDocument.Open(fullPath, new ParsingOptions { Password = password });

    string ExtractText(PdfAction action, RunVariables vars, string? projectRoot)
    {
        if (string.IsNullOrWhiteSpace(action.OutputVariable))
            throw new InvalidOperationException("pdf_extract_text: output_variable is required");

        var path = RunVariables.Interpolate(action.Path, vars);
        var full = FileActions.ResolveProjectPath(path, projectRoot);
        if (!File.Exists(full))
            throw new HoblioRunException($"pdf_extract_text: file not found `{full}`");

        var mode = (action.Mode ?? PdfEnums.ModeAll).Trim().ToLowerInvariant();
        if (mode.Length == 0)
            mode = PdfEnums.ModeAll;

        var password = string.IsNullOrWhiteSpace(action.Password)
            ? null
            : RunVariables.Interpolate(action.Password!, vars);

        using var doc = OpenPdf(full, password);

        var pageCount = doc.NumberOfPages;
        if (pageCount == 0)
        {
            vars[action.OutputVariable.Trim()] = "";
            return $"pdf_extract_text: {full} (0 pages) → {action.OutputVariable.Trim()}";
        }

        var sb = new StringBuilder();

        if (mode == PdfEnums.ModeAll)
        {
            for (var i = 1; i <= pageCount; i++)
                AppendPageText(sb, doc.GetPage(i));
        }
        else if (mode == PdfEnums.ModePage)
        {
            var pageNum = ResolvePageIndex(action.Page, vars, pageCount, "page");
            AppendPageText(sb, doc.GetPage(pageNum));
        }
        else if (mode == PdfEnums.ModeRange)
        {
            var (start, end) = ResolvePageRange(action, vars, pageCount);
            for (var i = start; i <= end; i++)
                AppendPageText(sb, doc.GetPage(i));
        }
        else
            throw new InvalidOperationException(
                $"pdf_extract_text: unknown mode `{action.Mode}` (use {PdfEnums.ModeAll}, {PdfEnums.ModePage}, or {PdfEnums.ModeRange})");

        var text = sb.ToString().TrimEnd();
        vars[action.OutputVariable.Trim()] = text;
        var preview = text.Length > 120 ? text[..120] + "…" : text;
        return $"pdf_extract_text: {full} mode={mode} ({text.Length} chars) → {action.OutputVariable.Trim()}: {preview}";
    }

    string ExtractTables(PdfAction action, RunVariables vars, string? projectRoot)
    {
        if (string.IsNullOrWhiteSpace(action.OutputVariable))
            throw new InvalidOperationException("pdf_extract_tables: output_variable is required");

        var path = RunVariables.Interpolate(action.Path, vars);
        var full = FileActions.ResolveProjectPath(path, projectRoot);
        if (!File.Exists(full))
            throw new HoblioRunException($"pdf_extract_tables: file not found `{full}`");

        var password = string.IsNullOrWhiteSpace(action.Password)
            ? null
            : RunVariables.Interpolate(action.Password!, vars);

        var tol = 4.0;
        if (!string.IsNullOrWhiteSpace(action.RowTolerance))
        {
            var tr = RunVariables.Interpolate(action.RowTolerance!, vars).Trim();
            if (!double.TryParse(tr, NumberStyles.Float, CultureInfo.InvariantCulture, out tol)
                && !double.TryParse(tr, NumberStyles.Float, CultureInfo.CurrentCulture, out tol))
                throw new InvalidOperationException($"pdf_extract_tables: row_tolerance must be a number, got `{tr}`");
        }

        using var doc = OpenPdf(full, password);
        var pageCount = doc.NumberOfPages;
        if (pageCount == 0)
        {
            vars[action.OutputVariable.Trim()] = "[]";
            return $"pdf_extract_tables: {full} (0 pages)";
        }

        var mode = (action.Mode ?? PdfEnums.ModeAll).Trim().ToLowerInvariant();
        if (mode.Length == 0)
            mode = PdfEnums.ModeAll;

        var tablesJson = new List<JsonElement>();

        void ProcessPage(Page page)
        {
            var rows = BuildTableRows(page, tol);
            if (rows.Count == 0)
                return;
            using var ms = new MemoryStream();
            using (var jw = new Utf8JsonWriter(ms))
            {
                jw.WriteStartArray();
                foreach (var row in rows)
                {
                    jw.WriteStartArray();
                    foreach (var cell in row)
                        jw.WriteStringValue(cell);
                    jw.WriteEndArray();
                }
                jw.WriteEndArray();
            }
            tablesJson.Add(JsonDocument.Parse(ms.ToArray()).RootElement.Clone());
        }

        if (mode == PdfEnums.ModeAll)
        {
            for (var i = 1; i <= pageCount; i++)
                ProcessPage(doc.GetPage(i));
        }
        else if (mode == PdfEnums.ModePage)
        {
            var n = ResolvePageIndex(action.Page, vars, pageCount, "page");
            ProcessPage(doc.GetPage(n));
        }
        else if (mode == PdfEnums.ModeRange)
        {
            var (start, end) = ResolvePageRange(action, vars, pageCount);
            for (var i = start; i <= end; i++)
                ProcessPage(doc.GetPage(i));
        }
        else
            throw new InvalidOperationException($"pdf_extract_tables: unknown mode `{action.Mode}`");

        var rootJson = JsonSerializer.SerializeToElement(tablesJson, HoblioJson.Options);
        var jsonStr = JsonSerializer.Serialize(rootJson, HoblioJson.Options);
        vars[action.OutputVariable.Trim()] = jsonStr;
        return $"pdf_extract_tables: {full} ({tablesJson.Count} table chunk(s)) → {action.OutputVariable.Trim()}";
    }

    static List<List<string>> BuildTableRows(Page page, double rowTolerance)
    {
        var words = page.GetWords().ToList();
        if (words.Count == 0)
            return [];

        words.Sort((a, b) =>
        {
            var cy = a.BoundingBox.Bottom.CompareTo(b.BoundingBox.Bottom);
            return cy != 0 ? cy : a.BoundingBox.Left.CompareTo(b.BoundingBox.Left);
        });

        var rows = new List<List<Word>>();
        foreach (var w in words)
        {
            List<Word>? row = null;
            foreach (var r in rows)
            {
                if (r.Count == 0)
                    continue;
                var refY = (r[0].BoundingBox.Top + r[0].BoundingBox.Bottom) / 2;
                var wy = (w.BoundingBox.Top + w.BoundingBox.Bottom) / 2;
                if (Math.Abs(refY - wy) <= rowTolerance)
                {
                    row = r;
                    break;
                }
            }
            if (row is null)
            {
                row = [];
                rows.Add(row);
            }
            row.Add(w);
        }

        foreach (var r in rows)
            r.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));

        var result = new List<List<string>>();
        foreach (var r in rows)
        {
            if (r.Count == 0)
                continue;
            var cells = new List<string>();
            var cur = new StringBuilder(r[0].Text);
            const double colGapMin = 8.0;
            for (var i = 1; i < r.Count; i++)
            {
                var prev = r[i - 1];
                var next = r[i];
                var gap = next.BoundingBox.Left - prev.BoundingBox.Right;
                if (gap > colGapMin)
                {
                    cells.Add(cur.ToString());
                    cur.Clear().Append(next.Text);
                }
                else
                {
                    if (cur.Length > 0)
                        cur.Append(' ');
                    cur.Append(next.Text);
                }
            }
            cells.Add(cur.ToString());
            result.Add(cells);
        }
        return result;
    }

    string ExtractImages(PdfAction action, RunVariables vars, string? projectRoot)
    {
        var path = RunVariables.Interpolate(action.Path, vars);
        var full = FileActions.ResolveProjectPath(path, projectRoot);
        if (!File.Exists(full))
            throw new HoblioRunException($"pdf_extract_images: file not found `{full}`");

        var outDirTemplate = action.OutputPath;
        if (string.IsNullOrWhiteSpace(outDirTemplate))
            throw new InvalidOperationException("pdf_extract_images: output_path (folder) is required");
        var outDir = FileActions.ResolveProjectPath(RunVariables.Interpolate(outDirTemplate.Trim(), vars), projectRoot);
        Directory.CreateDirectory(outDir);

        var fmt = (action.ImageFormat ?? "png").Trim().ToLowerInvariant();
        if (fmt is not "png" and not "jpg" and not "jpeg")
            fmt = "png";

        var password = string.IsNullOrWhiteSpace(action.Password)
            ? null
            : RunVariables.Interpolate(action.Password!, vars);

        using var doc = OpenPdf(full, password);
        var pageCount = doc.NumberOfPages;

        var mode = (action.Mode ?? PdfEnums.ModeAll).Trim().ToLowerInvariant();
        if (mode.Length == 0)
            mode = PdfEnums.ModeAll;

        var saved = new List<string>();
        void ProcessPage(int pageNumber, Page page)
        {
            var idx = 0;
            foreach (var img in page.GetImages())
            {
                idx++;
                var baseName = $"page_{pageNumber}_{idx}";
                if (img.TryGetPng(out var png) && png.Length > 0)
                {
                    var fp = Path.Combine(outDir, baseName + ".png");
                    File.WriteAllBytes(fp, png);
                    saved.Add(fp);
                }
                else if (img.TryGetBytes(out var raw) && raw is { Count: > 0 })
                {
                    var ext = fmt == "jpg" || fmt == "jpeg" ? ".jpg" : ".bin";
                    var fp = Path.Combine(outDir, baseName + ext);
                    File.WriteAllBytes(fp, raw.ToArray());
                    saved.Add(fp);
                }
            }
        }

        if (pageCount == 0)
        {
            var summaryJson = JsonSerializer.SerializeToElement(saved, HoblioJson.Options);
            var s = JsonSerializer.Serialize(summaryJson, HoblioJson.Options);
            if (!string.IsNullOrWhiteSpace(action.OutputVariable))
                vars[action.OutputVariable!.Trim()] = s;
            return $"pdf_extract_images: {full} (0 pages) saved 0 → {outDir}";
        }

        if (mode == PdfEnums.ModeAll)
        {
            for (var i = 1; i <= pageCount; i++)
                ProcessPage(i, doc.GetPage(i));
        }
        else if (mode == PdfEnums.ModePage)
        {
            var n = ResolvePageIndex(action.Page, vars, pageCount, "page");
            ProcessPage(n, doc.GetPage(n));
        }
        else if (mode == PdfEnums.ModeRange)
        {
            var (start, end) = ResolvePageRange(action, vars, pageCount);
            for (var i = start; i <= end; i++)
                ProcessPage(i, doc.GetPage(i));
        }
        else
            throw new InvalidOperationException($"pdf_extract_images: unknown mode `{action.Mode}`");

        var json = JsonSerializer.Serialize(saved, HoblioJson.Options);
        if (!string.IsNullOrWhiteSpace(action.OutputVariable))
            vars[action.OutputVariable.Trim()] = json;
        return $"pdf_extract_images: {full} saved {saved.Count} file(s) → {outDir}";
    }

    string ExtractPagesToFile(PdfAction action, RunVariables vars, string? projectRoot)
    {
        var srcPath = RunVariables.Interpolate(action.Path, vars);
        var fullSrc = FileActions.ResolveProjectPath(srcPath, projectRoot);
        if (!File.Exists(fullSrc))
            throw new HoblioRunException($"pdf_extract_pages: source not found `{fullSrc}`");

        var destTemplate = action.OutputPath;
        if (string.IsNullOrWhiteSpace(destTemplate))
            throw new InvalidOperationException("pdf_extract_pages: output_path is required");
        var fullDest = FileActions.ResolveProjectPath(RunVariables.Interpolate(destTemplate.Trim(), vars), projectRoot);
        FileActions.EnsureParent(fullDest);

        using var srcStream = File.OpenRead(fullSrc);
        var password = string.IsNullOrWhiteSpace(action.Password)
            ? null
            : RunVariables.Interpolate(action.Password!, vars);
        int pageCount;
        using (var tmpDoc = OpenPdf(fullSrc, password))
            pageCount = tmpDoc.NumberOfPages;

        var mode = (action.Mode ?? PdfEnums.ModeRange).Trim().ToLowerInvariant();
        List<int> pages;
        if (mode == PdfEnums.ModeAll)
        {
            pages = Enumerable.Range(1, pageCount).ToList();
        }
        else if (mode == PdfEnums.ModePage)
        {
            var n = ResolvePageIndex(action.Page, vars, pageCount, "page");
            pages = [n];
        }
        else if (mode == PdfEnums.ModeRange)
        {
            var (start, end) = ResolvePageRange(action, vars, pageCount);
            pages = Enumerable.Range(start, end - start + 1).ToList();
        }
        else
            throw new InvalidOperationException($"pdf_extract_pages: unknown mode `{action.Mode}`");

        IReadOnlyList<Stream> streams = [srcStream];
        IReadOnlyList<IReadOnlyList<int>> bundle = [pages];

        using var outStream = File.Create(fullDest);
        PdfMerger.Merge(streams, outStream, bundle, PdfAStandard.A1B, null);
        outStream.Flush();

        var note = string.IsNullOrWhiteSpace(action.OutputVariable) ? "" : $" var={action.OutputVariable.Trim()}";
        if (!string.IsNullOrWhiteSpace(action.OutputVariable))
            vars[action.OutputVariable.Trim()] = fullDest;

        return $"pdf_extract_pages: {fullSrc} → {fullDest} ({pages.Count} page(s)){note}";
    }

    string MergeFiles(PdfAction action, RunVariables vars, string? projectRoot)
    {
        var raw = action.InputPaths;
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("pdf_merge: input_paths is required (one path per line)");

        var paths = new List<string>();
        foreach (var line in raw.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None))
        {
            var t = RunVariables.Interpolate(line.Trim(), vars);
            if (t.Length == 0)
                continue;
            var full = FileActions.ResolveProjectPath(t, projectRoot);
            if (!File.Exists(full))
                throw new HoblioRunException($"pdf_merge: file not found `{full}`");
            paths.Add(full);
        }
        if (paths.Count < 2)
            throw new InvalidOperationException("pdf_merge: need at least two PDF paths in input_paths");

        var destTemplate = action.OutputPath ?? action.Path;
        if (string.IsNullOrWhiteSpace(destTemplate))
            throw new InvalidOperationException("pdf_merge: output_path (or path) is required for the merged PDF");
        var fullDest = FileActions.ResolveProjectPath(RunVariables.Interpolate(destTemplate.Trim(), vars), projectRoot);
        FileActions.EnsureParent(fullDest);

        using var outStream = File.Create(fullDest);
        PdfMerger.Merge(outStream, PdfAStandard.A1B, null, paths.ToArray());
        outStream.Flush();

        if (!string.IsNullOrWhiteSpace(action.OutputVariable))
            vars[action.OutputVariable.Trim()] = fullDest;

        return $"pdf_merge: {paths.Count} file(s) → {fullDest}";
    }

    static void AppendPageText(StringBuilder sb, Page page)
    {
        foreach (var word in page.GetWords())
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(word.Text);
        }
        sb.AppendLine();
    }

    static int ResolvePageIndex(string? template, RunVariables vars, int pageCount, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new InvalidOperationException($"pdf: `{fieldName}` is required for this mode");
        var raw = RunVariables.Interpolate(template.Trim(), vars).Trim();
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            && !int.TryParse(raw, NumberStyles.Integer, CultureInfo.CurrentCulture, out n))
            throw new InvalidOperationException($"pdf: `{fieldName}` must be an integer, got `{raw}`");
        if (n < 1 || n > pageCount)
            throw new InvalidOperationException($"pdf: `{fieldName}` must be between 1 and {pageCount}, got {n}");
        return n;
    }

    static (int Start, int End) ResolvePageRange(PdfAction action, RunVariables vars, int pageCount)
    {
        var start = ResolvePageIndex(action.StartPage, vars, pageCount, "start_page");
        var end = ResolvePageIndex(action.EndPage, vars, pageCount, "end_page");
        if (end < start)
            (start, end) = (end, start);
        return (start, end);
    }

    public void Dispose()
    {
    }
}
