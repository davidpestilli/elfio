using Hoblio.Model;

namespace Hoblio.Studio;

static class WordActionDefaults
{
    public static WordAction? TryCreate(string type, IReadOnlySet<string>? takenNames)
    {
        if (!WordActionTypes.IsWordType(type))
            return null;

        var instance = NextName("WordInstance", takenNames) ?? "WordInstance";
        var textOut = NextName("WordData", takenNames) ?? "WordData1";

        return type switch
        {
            WordActionTypes.Launch => new WordAction
            {
                Operation = type,
                Instance = instance,
                Visible = true,
                ReadOnly = false,
            },
            WordActionTypes.Attach => new WordAction
            {
                Operation = type,
                Instance = instance,
                DocumentName = "",
            },
            WordActionTypes.Save => new WordAction { Operation = type, Instance = instance },
            WordActionTypes.Close => new WordAction
            {
                Operation = type,
                Instance = instance,
                Mode = WordEnums.CloseMode.Close,
            },
            WordActionTypes.Read => new WordAction
            {
                Operation = type,
                Instance = instance,
                Mode = WordEnums.ReadMode.Document,
                OutputVariable = textOut,
            },
            WordActionTypes.Write => new WordAction
            {
                Operation = type,
                Instance = instance,
                Mode = WordEnums.WriteMode.End,
                Value = "",
                AppendNewLine = true,
            },
            WordActionTypes.InsertImage => new WordAction
            {
                Operation = type,
                Instance = instance,
                Mode = WordEnums.InsertImageMode.DocumentEnd,
                ImageSource = WordEnums.ImageSource.File,
                Path = "",
            },
            WordActionTypes.FindReplace => new WordAction
            {
                Operation = type,
                Instance = instance,
                FindText = "",
                ReplaceText = "",
                AllMatches = true,
                MatchWildcards = false,
                MatchCase = false,
                MatchEntireWord = false,
            },
            _ => null,
        };
    }

    static string? NextName(string prefix, IReadOnlySet<string>? takenNames)
    {
        if (takenNames is null || takenNames.Count == 0)
            return null;
        for (var i = 1; i <= 999; i++)
        {
            var candidate = i == 1 ? prefix : $"{prefix}{i}";
            if (!takenNames.Contains(candidate))
                return candidate;
        }
        return null;
    }
}
