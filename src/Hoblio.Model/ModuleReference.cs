namespace Hoblio.Model;

public sealed class ModuleReference
{
    public required string Name { get; init; }
    public required string AssemblyName { get; init; }
    public required string Version { get; init; }
    public int Type { get; init; } = ElfioEngineInfo.BuiltInModuleType;
}
