using System.Text.Json;

namespace Hoblio.Model;

public sealed class FlowProject
{
    public required FlowManifest Manifest { get; set; }
    public required FlowGraph Flow { get; set; }
    public Dictionary<string, JsonElement> Variables { get; set; } = new(StringComparer.Ordinal);
}

public sealed class FlowManifest
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? EntryFlow { get; init; }
    public Dictionary<string, ManifestFlowEntry>? Flows { get; init; }

    /// <summary>
    /// Projetos externos chamáveis por <c>run_flow</c> com <c>project</c>: id → caminho relativo.
    /// Declarar aqui é o que autoriza a chamada e o que forma o "superfluxo" (fluxo principal composto).
    /// </summary>
    public Dictionary<string, LinkedProject>? LinkedProjects { get; set; }

    public string? EngineVersion { get; set; }
    public string? CreatedEngineVersion { get; set; }
    public List<ModuleReference>? ModuleReferences { get; set; }
}

/// <summary>
/// Referência a outro projeto (fluxo principal) que pode ser executado por um nó <c>run_flow</c>.
/// O contrato declarado (<see cref="Requires"/>/<see cref="Exports"/>) é validado estaticamente:
/// <see cref="Exports"/> entra como variável conhecida do projeto chamador.
/// </summary>
public sealed class LinkedProject
{
    /// <summary>Caminho relativo à pasta do projeto chamador (a pasta do <c>manifest.json</c>).</summary>
    public required string Path { get; set; }

    public string? Description { get; set; }

    /// <summary>Fluxo de entrada no projeto vinculado (id em <c>manifest.flows</c>); padrão: <c>entry_flow</c>.</summary>
    public string? Flow { get; set; }

    /// <summary>Variáveis que este projeto espera receber do chamador (documental + Studio).</summary>
    public List<string>? Requires { get; set; }

    /// <summary>Variáveis que este projeto entrega ao chamador depois da execução.</summary>
    public List<string>? Exports { get; set; }
}

public sealed class ManifestFlowEntry
{
    public required string Path { get; init; }
    public List<string>? Inputs { get; init; }
    public List<string>? Outputs { get; init; }
    public string? Description { get; init; }
}
