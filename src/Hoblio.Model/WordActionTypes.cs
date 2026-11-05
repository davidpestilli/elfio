namespace Hoblio.Model;

/// <summary>Identificadores JSON (<c>type</c>) e telemetria (<c>action_id</c>) das ações Word.</summary>
public static class WordActionTypes
{
    public const string Launch = "word_launch";
    public const string Attach = "word_attach";
    public const string Save = "word_save";
    public const string Close = "word_close";
    public const string Read = "word_read";
    public const string Write = "word_write";
    public const string InsertImage = "word_insert_image";
    public const string FindReplace = "word_find_replace";

    public static readonly IReadOnlyList<string> All =
    [
        Launch, Attach, Save, Close, Read, Write, InsertImage, FindReplace,
    ];

    public static readonly IReadOnlyDictionary<string, string> ModuleActionIds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Launch] = "LaunchWord",
            [Attach] = "AttachToRunningWord",
            [Save] = "SaveWord",
            [Close] = "CloseWord",
            [Read] = "ReadFromWord",
            [Write] = "WriteToWord",
            [InsertImage] = "InsertImageToWord",
            [FindReplace] = "FindAndReplaceWord",
        };

    public static bool IsWordType(string? type) =>
        type is not null && ModuleActionIds.ContainsKey(type);

    public static string ModuleActionId(string type) =>
        ModuleActionIds.TryGetValue(type, out var id) ? id : type;
}
