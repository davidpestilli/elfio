namespace Hoblio.Runtime;

public static class FileActions
{
    public static string ResolveProjectPath(string path, string? projectRoot)
    {
        if (Path.IsPathRooted(path))
            return path;
        return projectRoot is not null ? Path.Combine(projectRoot, path) : path;
    }

    public static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
    }

    public static string FileWrite(string path, string content, string? projectRoot)
    {
        var full = ResolveProjectPath(path, projectRoot);
        EnsureParent(full);
        File.WriteAllText(full, content, Encoding.UTF8);
        var byteCount = Encoding.UTF8.GetByteCount(content);
        return $"file_write: {full} ({byteCount} bytes)";
    }

    public static string FileAppend(string path, string content, string? projectRoot)
    {
        var full = ResolveProjectPath(path, projectRoot);
        EnsureParent(full);
        File.AppendAllText(full, content);
        return $"file_append: {full} (+{content.Length} bytes)";
    }

    public static string FileRead(string path, string? projectRoot)
    {
        var full = ResolveProjectPath(path, projectRoot);
        return File.ReadAllText(full);
    }

    public static string FileReadIntoVariable(string pathTemplate, string variable, RunVariables vars, string? projectRoot)
    {
        var name = variable.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("file_read: variable name is empty");
        var path = RunVariables.Interpolate(pathTemplate, vars);
        var content = FileRead(path, projectRoot);
        vars[name] = content;
        return $"file_read: `{name}` ({content.Length} bytes)";
    }
}
