namespace Hoblio.Model;

public abstract class ControlKind
{
    public abstract string Type { get; }
}

public sealed class IfControlKind : ControlKind
{
    public override string Type => "if";
    public required string Left { get; init; }
    public required CompareOperator Operator { get; init; }
    public required string Right { get; init; }
    public bool IgnoreCase { get; init; }
}

public sealed class ForEachControlKind : ControlKind
{
    public override string Type => "for_each";
    public required string ListVariable { get; init; }
    public required string ItemVariable { get; init; }
}

public sealed class WhileControlKind : ControlKind
{
    public override string Type => "while";
    public required string Left { get; init; }
    public required CompareOperator Operator { get; init; }
    public required string Right { get; init; }
    public bool IgnoreCase { get; init; }
}

public sealed class RepeatControlKind : ControlKind
{
    public override string Type => "repeat";
    public required string Times { get; init; }
    public required string IndexVariable { get; init; }
}

public sealed record IfControl(
    string Left,
    CompareOperator Operator,
    string Right,
    bool IgnoreCase);

public sealed record ForEachControl(string ListVariable, string ItemVariable);

public sealed record RepeatControl(string Times, string IndexVariable);
