using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Hoblio.Studio.ViewModels;

public sealed class PaletteGroupViewModel
{
    public required string Name { get; init; }
    public bool StartsExpanded { get; init; }
    public ObservableCollection<PaletteEntryViewModel> Items { get; init; } = [];
    public ObservableCollection<PaletteSectionViewModel> Sections { get; init; } = [];

    public bool HasSections => Sections.Count > 0;

    int ItemCount => Items.Count + Sections.Sum(s => s.Items.Count);

    public string HeaderText => ItemCount == 0 ? Name : $"{Name} ({ItemCount})";
}

public sealed class PaletteSectionViewModel
{
    public required string Name { get; init; }
    public bool StartsExpanded { get; init; }
    public ObservableCollection<PaletteEntryViewModel> Items { get; init; } = [];

    public string HeaderText => Items.Count == 0 ? Name : $"{Name} ({Items.Count})";
}

public sealed class PaletteEntryViewModel
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public string? Hint { get; init; }
    public required string SearchIndex { get; init; }
    public required string Glyph { get; init; }
    public required Brush AccentBrush { get; init; }

    public static PaletteEntryViewModel FromCatalogEntry(PaletteEntry entry)
    {
        var visual = PaletteVisuals.ForKey(entry.Key);
        return new PaletteEntryViewModel
        {
            Key = entry.Key,
            Title = entry.Title,
            Hint = entry.Hint,
            SearchIndex = PaletteSearch.BuildSearchIndex(entry.Key, entry.Title, entry.Hint, entry.SearchAliases),
            Glyph = visual.Glyph,
            AccentBrush = PaletteVisuals.AccentBrush(entry.Key),
        };
    }
}
