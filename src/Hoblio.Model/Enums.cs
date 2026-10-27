using System.Text.Json.Serialization;

namespace Hoblio.Model;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserInputKind
{
    [JsonPropertyName("single_line")]
    SingleLine,
    [JsonPropertyName("multi_line")]
    MultiLine,
}

public enum NodeKind
{
    Start,
    Action,
    End,
    If,
    [JsonPropertyName("for_each")]
    ForEach,
    [JsonPropertyName("next_loop")]
    NextLoop,
    [JsonPropertyName("exit_loop")]
    ExitLoop,
    [JsonPropertyName("while_loop")]
    WhileLoop,
    Repeat,
}

public enum EdgePort
{
    Main,
    Then,
    Else,
    Body,
    After,
}

public enum CompareOperator
{
    Eq,
    Ne,
    Contains,
    Gt,
    Gte,
    Lt,
    Lte,
}

public enum OnFailure
{
    Stop,
    Continue,
}

public static class EdgePortExtensions
{
    public static EdgePort? Parse(string? s) => s switch
    {
        "main" => EdgePort.Main,
        "then" => EdgePort.Then,
        "else" => EdgePort.Else,
        "body" => EdgePort.Body,
        "after" => EdgePort.After,
        _ => null,
    };

    public static string AsString(this EdgePort port) => port switch
    {
        EdgePort.Main => "main",
        EdgePort.Then => "then",
        EdgePort.Else => "else",
        EdgePort.Body => "body",
        EdgePort.After => "after",
        _ => "main",
    };
}
