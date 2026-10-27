using System.Text;
using System.Text.Json;
using Hoblio.Model;
using Hoblio.Model.Json;

namespace Hoblio.Serde;

public static class ProjectLayout
{
    public const string ManifestFile = "manifest.json";
    public const string FlowFile = "flow.json";
    public const string AssetsDir = "assets";

    public static string ManifestPath(string root) => Path.Combine(root, ManifestFile);
    public static string FlowPath(string root) => Path.Combine(root, FlowFile);
}

public static class ProjectLoader
{
    public static FlowProject LoadProject(string dir)
    {
        if (!Directory.Exists(dir))
            throw new InvalidOperationException($"project path is not a directory: {dir}");
        var manifestPath = ProjectLayout.ManifestPath(dir);
        var flowPath = ProjectLayout.FlowPath(dir);
        if (!File.Exists(manifestPath))
            throw new InvalidOperationException($"missing {ProjectLayout.ManifestFile} in {dir}");
        if (!File.Exists(flowPath))
            throw new InvalidOperationException($"missing {ProjectLayout.FlowFile} in {dir}");

        var manifestJson = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<FlowManifest>(manifestJson, HoblioJson.Options)
            ?? throw new InvalidOperationException($"parse {manifestPath}");

        var flowDoc = JsonDocument.Parse(File.ReadAllText(flowPath));
        var flow = flowDoc.RootElement.Deserialize<FlowGraph>(HoblioJson.Options)
            ?? throw new InvalidOperationException($"invalid flow schema in {flowPath}");

        Dictionary<string, JsonElement> variables = new(StringComparer.Ordinal);
        if (flowDoc.RootElement.TryGetProperty("variables", out var varsEl) && varsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in varsEl.EnumerateObject())
                variables[prop.Name] = prop.Value.Clone();
        }

        return new FlowProject
        {
            Manifest = manifest,
            Flow = flow,
            Variables = variables,
        };
    }

    public static void SaveProject(string dir, FlowProject project)
    {
        Directory.CreateDirectory(dir);
        var manifestPath = ProjectLayout.ManifestPath(dir);
        var flowPath = ProjectLayout.FlowPath(dir);

        WriteJsonIfChanged(manifestPath, project.Manifest);
        WriteFlow(flowPath, project.Flow, project.Variables);
    }

    public static string SerializeProjectToString(FlowProject project)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("manifest");
            JsonSerializer.Serialize(writer, project.Manifest, HoblioJson.Options);
            writer.WritePropertyName("flow");
            WriteFlowBody(writer, project.Flow, project.Variables);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static FlowProject DeserializeProjectFromString(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var manifest = root.GetProperty("manifest").Deserialize<FlowManifest>(HoblioJson.Options)!;
        var flowEl = root.GetProperty("flow");
        var flow = flowEl.Deserialize<FlowGraph>(HoblioJson.Options)!;
        Dictionary<string, JsonElement> variables = new(StringComparer.Ordinal);
        if (flowEl.TryGetProperty("variables", out var varsEl) && varsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in varsEl.EnumerateObject())
                variables[prop.Name] = prop.Value.Clone();
        }
        return new FlowProject { Manifest = manifest, Flow = flow, Variables = variables };
    }

    static void WriteFlowBody(Utf8JsonWriter writer, FlowGraph flow, Dictionary<string, JsonElement> variables)
    {
        writer.WriteStartObject();
        writer.WriteString("start", flow.Start);
        writer.WritePropertyName("nodes");
        JsonSerializer.Serialize(writer, flow.Nodes, HoblioJson.Options);
        writer.WritePropertyName("edges");
        JsonSerializer.Serialize(writer, flow.Edges, HoblioJson.Options);
        if (variables.Count > 0)
        {
            writer.WritePropertyName("variables");
            writer.WriteStartObject();
            foreach (var (key, value) in variables.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(key);
                value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    public static void ValidateProjectDir(string dir)
    {
        var project = LoadProject(dir);
        GraphValidator.ValidateProject(project);
    }

    static void WriteFlow(string path, FlowGraph flow, Dictionary<string, JsonElement> variables)
    {
        var flowJson = JsonSerializer.Serialize(flow, HoblioJson.Options);
        using var doc = JsonDocument.Parse(flowJson);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("start", flow.Start);
            writer.WritePropertyName("nodes");
            doc.RootElement.GetProperty("nodes").WriteTo(writer);
            writer.WritePropertyName("edges");
            doc.RootElement.GetProperty("edges").WriteTo(writer);
            if (variables.Count > 0)
            {
                writer.WritePropertyName("variables");
                writer.WriteStartObject();
                foreach (var (key, value) in variables.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(key);
                    value.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        File.WriteAllBytes(path, stream.ToArray());
    }

    static void WriteJsonIfChanged<T>(string path, T value)
    {
        var text = JsonSerializer.Serialize(value, HoblioJson.Options) + Environment.NewLine;
        if (File.Exists(path))
        {
            try
            {
                var existing = File.ReadAllText(path);
                if (JsonSemanticEquals(existing, text))
                    return;
            }
            catch
            {
                // fall through to write
            }
        }
        File.WriteAllText(path, text);
    }

    static bool JsonSemanticEquals(string a, string b)
    {
        try
        {
            using var da = JsonDocument.Parse(a);
            using var db = JsonDocument.Parse(b);
            return da.RootElement.GetRawText() == db.RootElement.GetRawText();
        }
        catch
        {
            return a == b;
        }
    }
}
