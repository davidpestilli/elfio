using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public bool ShowActionPdf => GetSelectedNode()?.Action is PdfAction;

    PdfAction? PdfA() => A<PdfAction>();

    public bool PdfShowOutputPathField =>
        PdfA() is { Operation: PdfActionTypes.ExtractImages or PdfActionTypes.ExtractPages or PdfActionTypes.Merge };

    public bool PdfShowInputPathsField => PdfA()?.Operation == PdfActionTypes.Merge;

    public bool PdfShowImageFormatField => PdfA()?.Operation == PdfActionTypes.ExtractImages;

    public bool PdfShowRowToleranceField => PdfA()?.Operation == PdfActionTypes.ExtractTables;

    void PatchPdf(Func<PdfAction, PdfActionPatch> patchFields)
    {
        if (PdfA() is not { } a)
            return;
        SetAction(PdfActionMutator.With(a, patchFields(a)));
    }

    public string PdfPath
    {
        get => PdfA()?.Path ?? "";
        set => PatchPdf(_ => new PdfActionPatch { Path = value });
    }

    public string PdfMode
    {
        get => PdfA()?.Mode ?? PdfEnums.ModeAll;
        set => PatchPdf(_ => new PdfActionPatch { Mode = string.IsNullOrWhiteSpace(value) ? PdfEnums.ModeAll : value.Trim() });
    }

    public string PdfPage
    {
        get => PdfA()?.Page ?? "";
        set => PatchPdf(_ => new PdfActionPatch { Page = string.IsNullOrWhiteSpace(value) ? null : value });
    }

    public string PdfStartPage
    {
        get => PdfA()?.StartPage ?? "";
        set => PatchPdf(_ => new PdfActionPatch { StartPage = string.IsNullOrWhiteSpace(value) ? null : value });
    }

    public string PdfEndPage
    {
        get => PdfA()?.EndPage ?? "";
        set => PatchPdf(_ => new PdfActionPatch { EndPage = string.IsNullOrWhiteSpace(value) ? null : value });
    }

    public string PdfPassword
    {
        get => PdfA()?.Password ?? "";
        set => PatchPdf(_ => new PdfActionPatch { Password = string.IsNullOrWhiteSpace(value) ? null : value });
    }

    public string PdfOutputPath
    {
        get => PdfA()?.OutputPath ?? "";
        set => PatchPdf(_ => new PdfActionPatch { OutputPath = string.IsNullOrWhiteSpace(value) ? null : value });
    }

    public string PdfInputPaths
    {
        get => PdfA()?.InputPaths ?? "";
        set => PatchPdf(_ => new PdfActionPatch { InputPaths = string.IsNullOrWhiteSpace(value) ? null : value });
    }

    public string PdfImageFormat
    {
        get => PdfA()?.ImageFormat ?? "";
        set => PatchPdf(_ => new PdfActionPatch { ImageFormat = string.IsNullOrWhiteSpace(value) ? null : value });
    }

    public string PdfRowTolerance
    {
        get => PdfA()?.RowTolerance ?? "";
        set => PatchPdf(_ => new PdfActionPatch { RowTolerance = string.IsNullOrWhiteSpace(value) ? null : value });
    }

    public string PdfOutputVariable
    {
        get => PdfA()?.OutputVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || PdfA() is not { } a)
                return;
            ApplyProducerRenameOr(
                node,
                () => node.Action = PdfActionMutator.With(a, new PdfActionPatch { OutputVariable = value }),
                a.OutputVariable,
                value);
        }
    }

    void RefreshPdfInspector()
    {
        OnPropertyChanged(nameof(ShowActionPdf));
        OnPropertyChanged(nameof(PdfShowOutputPathField));
        OnPropertyChanged(nameof(PdfShowInputPathsField));
        OnPropertyChanged(nameof(PdfShowImageFormatField));
        OnPropertyChanged(nameof(PdfShowRowToleranceField));
        foreach (var name in new[]
                 {
                     nameof(PdfPath), nameof(PdfMode), nameof(PdfPage), nameof(PdfStartPage), nameof(PdfEndPage),
                     nameof(PdfPassword), nameof(PdfOutputPath), nameof(PdfInputPaths), nameof(PdfImageFormat),
                     nameof(PdfRowTolerance), nameof(PdfOutputVariable),
                 })
            OnPropertyChanged(name);
    }
}
