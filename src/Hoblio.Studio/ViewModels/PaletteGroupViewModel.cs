using System.Collections.ObjectModel;

namespace Hoblio.Studio.ViewModels;

public sealed class PaletteGroupViewModel
{
    public required string Name { get; init; }
    public ObservableCollection<PaletteEntryViewModel> Items { get; init; } = [];
}

public sealed class PaletteEntryViewModel
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public string? Hint { get; init; }
}
