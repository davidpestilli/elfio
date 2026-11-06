namespace Hoblio.Model;

/// <summary>
/// Variáveis que o runtime publica em **todo** run, sem o fluxo precisar declará-las em
/// <c>flow.json</c>. Servem para o fluxo saber *como* foi iniciado: pelo usuário (individual)
/// ou como etapa de um superfluxo (chamado por <c>run_flow</c> com <c>project</c>).
/// </summary>
public static class BuiltInVariables
{
    /// <summary>
    /// <c>standalone</c> quando o run começou neste projeto; <c>superflow</c> quando o projeto
    /// foi chamado pelo fluxo de outro projeto.
    /// </summary>
    public const string RunMode = "HoblioRunMode";

    /// <summary>Quantos vínculos de projeto já foram atravessados (0 = execução individual).</summary>
    public const string RunDepth = "HoblioRunDepth";

    public const string Standalone = "standalone";
    public const string Superflow = "superflow";

    public static readonly IReadOnlyList<string> All = [RunMode, RunDepth];

    public static bool IsBuiltIn(string? name) =>
        name is not null && (name == RunMode || name == RunDepth);
}
