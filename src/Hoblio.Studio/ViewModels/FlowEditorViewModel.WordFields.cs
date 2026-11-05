using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public bool ShowActionWord => GetSelectedNode()?.Action is WordAction;

    WordAction? WordA() => A<WordAction>();

    void PatchWord(Func<WordAction, WordActionPatch> patchFields)
    {
        if (WordA() is not { } a)
            return;
        SetAction(WordActionMutator.With(a, patchFields(a)));
    }

    public string WordInstance
    {
        get => WordA()?.Instance ?? "";
        set => PatchWord(_ => new WordActionPatch { Instance = value });
    }

    public string WordPath
    {
        get => WordA()?.Path ?? "";
        set => PatchWord(_ => new WordActionPatch { Path = value });
    }

    public string WordDocumentName
    {
        get => WordA()?.DocumentName ?? "";
        set => PatchWord(_ => new WordActionPatch { DocumentName = value });
    }

    public string WordMode
    {
        get => WordA()?.Mode ?? "";
        set => PatchWord(_ => new WordActionPatch { Mode = value });
    }

    public string WordPages
    {
        get => WordA()?.Pages ?? "";
        set => PatchWord(_ => new WordActionPatch { Pages = value });
    }

    public string WordBookmark
    {
        get => WordA()?.Bookmark ?? "";
        set => PatchWord(_ => new WordActionPatch { Bookmark = value });
    }

    public string WordValue
    {
        get => WordA()?.Value ?? "";
        set => PatchWord(_ => new WordActionPatch { Value = value });
    }

    public string WordOutputVariable
    {
        get => WordA()?.OutputVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || WordA() is not { } a)
                return;
            ApplyProducerRenameOr(
                node,
                () => node.Action = WordActionMutator.With(a, new WordActionPatch { OutputVariable = value }),
                a.OutputVariable,
                value);
        }
    }

    public string WordFindText
    {
        get => WordA()?.FindText ?? "";
        set => PatchWord(_ => new WordActionPatch { FindText = value });
    }

    public string WordReplaceText
    {
        get => WordA()?.ReplaceText ?? "";
        set => PatchWord(_ => new WordActionPatch { ReplaceText = value });
    }

    public string WordImageSource
    {
        get => WordA()?.ImageSource ?? "";
        set => PatchWord(_ => new WordActionPatch { ImageSource = value });
    }

    public string WordTextToFind
    {
        get => WordA()?.TextToFind ?? "";
        set => PatchWord(_ => new WordActionPatch { TextToFind = value });
    }

    public bool WordVisible
    {
        get => WordA()?.Visible ?? true;
        set => PatchWord(_ => new WordActionPatch { Visible = value });
    }

    public bool WordReadOnly
    {
        get => WordA()?.ReadOnly ?? false;
        set => PatchWord(_ => new WordActionPatch { ReadOnly = value });
    }

    public bool WordAppendNewLine
    {
        get => WordA()?.AppendNewLine ?? true;
        set => PatchWord(_ => new WordActionPatch { AppendNewLine = value });
    }

    public bool WordAllMatches
    {
        get => WordA()?.AllMatches ?? true;
        set => PatchWord(_ => new WordActionPatch { AllMatches = value });
    }

    public bool WordMatchWildcards
    {
        get => WordA()?.MatchWildcards ?? false;
        set => PatchWord(_ => new WordActionPatch { MatchWildcards = value });
    }

    public bool WordMatchCase
    {
        get => WordA()?.MatchCase ?? false;
        set => PatchWord(_ => new WordActionPatch { MatchCase = value });
    }

    public bool WordMatchEntireWord
    {
        get => WordA()?.MatchEntireWord ?? false;
        set => PatchWord(_ => new WordActionPatch { MatchEntireWord = value });
    }

    void RefreshWordInspector()
    {
        OnPropertyChanged(nameof(ShowActionWord));
        foreach (var name in new[]
                 {
                     nameof(WordInstance), nameof(WordPath), nameof(WordDocumentName), nameof(WordMode),
                     nameof(WordPages), nameof(WordBookmark), nameof(WordValue), nameof(WordOutputVariable),
                     nameof(WordFindText), nameof(WordReplaceText), nameof(WordImageSource), nameof(WordTextToFind),
                     nameof(WordVisible), nameof(WordReadOnly), nameof(WordAppendNewLine), nameof(WordAllMatches),
                     nameof(WordMatchWildcards), nameof(WordMatchCase), nameof(WordMatchEntireWord),
                 })
            OnPropertyChanged(name);
    }
}
