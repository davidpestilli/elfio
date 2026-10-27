namespace Hoblio.Runtime;

public static class FileActions
{
    static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

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
        File.WriteAllText(full, content, Utf8NoBom);
        var byteCount = Utf8NoBom.GetByteCount(content);
        return $"file_write: {full} ({byteCount} bytes)";
    }

    public static string FileAppend(string path, string content, string? projectRoot)
    {
        var full = ResolveProjectPath(path, projectRoot);
        EnsureParent(full);
        File.AppendAllText(full, content, Utf8NoBom);
        var byteCount = Utf8NoBom.GetByteCount(content);
        return $"file_append: {full} (+{byteCount} bytes)";
    }

    public static string FileRead(string path, string? projectRoot)
    {
        var full = ResolveProjectPath(path, projectRoot);
        try
        {
            return File.ReadAllText(full, Utf8NoBom);
        }
        catch (Exception ex)
        {
            throw new HoblioRunException($"read file {full}", ex);
        }
    }

    public static string FileReadIntoVariable(string pathTemplate, string variable, RunVariables vars, string? projectRoot)
    {
        var name = variable.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("file_read: variable name is empty");
        var path = RunVariables.Interpolate(pathTemplate, vars);
        var full = ResolveProjectPath(path, projectRoot);
        string content;
        try
        {
            content = File.ReadAllText(full, Utf8NoBom);
        }
        catch (Exception ex)
        {
            throw new HoblioRunException($"read file {full}", ex);
        }
        vars[name] = content;
        var byteCount = Utf8NoBom.GetByteCount(content);
        return $"file_read: `{name}` ({byteCount} bytes)";
    }
}
