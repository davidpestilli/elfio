using Hoblio.Model;

namespace Hoblio.Runtime;

/// <summary>
/// Publica — e depois restaura — as variáveis de contexto do run
/// (<see cref="BuiltInVariables.RunMode"/> e <see cref="BuiltInVariables.RunDepth"/>).
///
/// Um fluxo não precisa saber quem o chamou: ele pergunta em que <em>modo</em> está rodando.
/// <c>run_flow project</c> marca o filho como <c>superflow</c>; qualquer outro caminho
/// (subfluxo embutido, arquivo do mesmo projeto) mantém o modo do chamador, porque o run
/// continua sendo do mesmo projeto. Ao voltar do filho os valores anteriores são restaurados,
/// senão o pai passaria a se ver como filho.
/// </summary>
public static class RunContext
{
    /// <summary>Contexto de um run iniciado neste projeto (CLI, Studio, sonda).</summary>
    public static IDisposable EnterRoot(RunVariables vars) =>
        Enter(vars, BuiltInVariables.Standalone, 0);

    /// <summary>
    /// Contexto de um <c>run_flow</c>: atravessar a fronteira de um projeto vinculado muda o modo
    /// para <c>superflow</c> e incrementa a profundidade.
    /// </summary>
    public static IDisposable EnterChild(RunVariables vars, bool crossesProjectBoundary) =>
        Enter(
            vars,
            crossesProjectBoundary ? BuiltInVariables.Superflow : CurrentMode(vars),
            CurrentDepth(vars) + (crossesProjectBoundary ? 1 : 0));

    public static string CurrentMode(RunVariables vars) =>
        vars.TryGetValue(BuiltInVariables.RunMode, out var mode) && mode.Length > 0
            ? mode
            : BuiltInVariables.Standalone;

    public static int CurrentDepth(RunVariables vars) =>
        vars.TryGetValue(BuiltInVariables.RunDepth, out var raw)
        && int.TryParse(raw, out var depth)
        && depth >= 0
            ? depth
            : 0;

    static IDisposable Enter(RunVariables vars, string mode, int depth)
    {
        var scope = new Scope(vars);
        vars[BuiltInVariables.RunMode] = mode;
        vars[BuiltInVariables.RunDepth] = depth.ToString();
        return scope;
    }

    sealed class Scope : IDisposable
    {
        readonly RunVariables _vars;
        readonly string? _previousMode;
        readonly string? _previousDepth;
        bool _disposed;

        public Scope(RunVariables vars)
        {
            _vars = vars;
            _previousMode = vars.TryGetValue(BuiltInVariables.RunMode, out var mode) ? mode : null;
            _previousDepth = vars.TryGetValue(BuiltInVariables.RunDepth, out var depth) ? depth : null;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Restore(BuiltInVariables.RunMode, _previousMode);
            Restore(BuiltInVariables.RunDepth, _previousDepth);
        }

        void Restore(string name, string? value)
        {
            if (value is null)
                _vars.Remove(name);
            else
                _vars[name] = value;
        }
    }
}
