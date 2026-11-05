using Hoblio.Model;
using Hoblio.Serde;

namespace Hoblio.Studio;

/// <summary>
/// Operações puras para declarar um projeto vinculado (<c>manifest.linked_projects</c>) a partir do Studio.
/// Sem UI: o <see cref="ViewModels.FlowEditorViewModel"/> apenas aplica o <see cref="PlannedLink"/>.
/// </summary>
public static class LinkedProjectOps
{
    /// <summary>Vínculo calculado, ainda não aplicado ao manifest.</summary>
    public sealed record PlannedLink(string Id, string RelativePath, LinkedProject Link);

    public static bool IsProjectFolder(string folder) =>
        !string.IsNullOrWhiteSpace(folder)
        && File.Exists(ProjectLayout.ManifestPath(folder))
        && File.Exists(ProjectLayout.FlowPath(folder));

    /// <summary>
    /// Caminho relativo do projeto chamador até <paramref name="targetDir"/>, sempre com <c>/</c>
    /// (o JSON fica estável entre sistemas e legível no manifest).
    /// </summary>
    public static string RelativePath(string callerDir, string targetDir) =>
        Path.GetRelativePath(callerDir, targetDir).Replace('\\', '/');

    /// <summary>Id sugerido a partir do nome da pasta, restrito a letras, dígitos, <c>_</c> e <c>-</c>.</summary>
    public static string SuggestId(string folder)
    {
        var name = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var sb = new System.Text.StringBuilder();
        foreach (var ch in name)
            sb.Append(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-' ? ch : '_');
        var id = sb.ToString().Trim('_');
        return id.Length == 0 ? "projeto" : id;
    }

    public static string UniqueId(IReadOnlyDictionary<string, LinkedProject> links, string baseId)
    {
        if (!links.ContainsKey(baseId))
            return baseId;
        for (var n = 2; n < 100; n++)
        {
            var candidate = $"{baseId}_{n}";
            if (!links.ContainsKey(candidate))
                return candidate;
        }
        return baseId + "_" + Guid.NewGuid().ToString("N")[..6];
    }

    public static string SanitizeFolderName(string? raw)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var ch in (raw ?? "").Trim())
        {
            if (Array.IndexOf(Path.GetInvalidFileNameChars(), ch) >= 0)
                continue;
            sb.Append(ch);
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Calcula o vínculo para <paramref name="targetDir"/>.
    /// Retorna <c>null</c> quando a pasta não é um projeto Elfio ou é o próprio projeto chamador.
    /// Reaproveita o id (e os metadados) de um vínculo que já aponta para o mesmo destino.
    /// </summary>
    public static PlannedLink? Plan(
        IReadOnlyDictionary<string, LinkedProject>? links,
        string callerDir,
        string targetDir,
        string? preferredId = null)
    {
        if (string.IsNullOrWhiteSpace(callerDir) || string.IsNullOrWhiteSpace(targetDir))
            return null;

        var caller = Path.GetFullPath(callerDir).TrimEnd(Path.DirectorySeparatorChar);
        var target = Path.GetFullPath(targetDir).TrimEnd(Path.DirectorySeparatorChar);
        if (!IsProjectFolder(target))
            return null;
        if (string.Equals(caller, target, StringComparison.OrdinalIgnoreCase))
            return null;

        var relative = RelativePath(caller, target);

        var existing = links is null
            ? default
            : links.FirstOrDefault(kv =>
                string.Equals(
                    Path.GetFullPath(Path.Combine(caller, kv.Value.Path)).TrimEnd(Path.DirectorySeparatorChar),
                    target,
                    StringComparison.OrdinalIgnoreCase));

        var id = !string.IsNullOrWhiteSpace(existing.Key)
            ? existing.Key
            : !string.IsNullOrWhiteSpace(preferredId)
                ? UniqueId(links ?? new Dictionary<string, LinkedProject>(), preferredId!.Trim())
                : UniqueId(links ?? new Dictionary<string, LinkedProject>(), SuggestId(target));

        var previous = string.IsNullOrWhiteSpace(existing.Key) ? null : existing.Value;
        var link = new LinkedProject
        {
            Path = relative,
            Description = previous?.Description,
            Flow = previous?.Flow,
            Requires = previous?.Requires,
            Exports = previous?.Exports,
        };
        return new PlannedLink(id, relative, link);
    }

    /// <summary>Aplica o vínculo no manifest, criando o dicionário quando necessário.</summary>
    public static void Apply(FlowManifest manifest, PlannedLink planned)
    {
        manifest.LinkedProjects ??= new Dictionary<string, LinkedProject>(StringComparer.Ordinal);
        manifest.LinkedProjects[planned.Id] = planned.Link;
    }

    /// <summary>Ids dos vínculos declarados, em ordem estável (para o picker).</summary>
    public static IReadOnlyList<string> Ids(FlowManifest manifest) =>
        manifest.LinkedProjects is { Count: > 0 }
            ? manifest.LinkedProjects.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList()
            : [];

    /// <summary>
    /// Cria uma pasta de projeto Elfio válida (manifest + <c>flow.json</c> com Start → End) dentro de
    /// <paramref name="parentDir"/>. Retorna o caminho criado, ou <c>null</c> quando o nome é inválido.
    /// </summary>
    public static string? CreateScaffold(string parentDir, string folderName, string? description = null)
    {
        var clean = SanitizeFolderName(folderName);
        if (string.IsNullOrWhiteSpace(parentDir) || clean.Length == 0)
            return null;

        var target = Path.Combine(Path.GetFullPath(parentDir), clean);
        if (IsProjectFolder(target))
            return target;

        Directory.CreateDirectory(target);
        ProjectLoader.SaveProject(target, new FlowProject
        {
            Manifest = new FlowManifest
            {
                Name = clean,
                Version = "0.1.0",
                Description = description ?? "Fluxo principal criado a partir de um superfluxo",
                EntryFlow = ProjectLayout.FlowFile,
            },
            Flow = MinimalGraph(),
            Variables = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal),
        });
        return target;
    }

    static FlowGraph MinimalGraph() => new()
    {
        Start = "start",
        Nodes =
        [
            new Node { Id = "start", Kind = NodeKind.Start, Label = "Start", Ui = new NodeUi { X = 80, Y = 120 } },
            new Node { Id = "end", Kind = NodeKind.End, Label = "End", Ui = new NodeUi { X = 320, Y = 120 } },
        ],
        Edges = [new Edge { From = "start", To = "end", Port = "main" }],
    };
}
