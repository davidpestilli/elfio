using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hoblio.Model.Json;

public sealed class ControlKindJsonConverter : JsonConverter<ControlKind>
{
    public override ControlKind? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var type = root.GetProperty("type").GetString() ?? throw new JsonException("control missing type");
        return type switch
        {
            "if" => new IfControlKind
            {
                Left = root.GetProperty("left").GetString() ?? "",
                Operator = ParseCompareOperator(root.GetProperty("operator").GetString()),
                Right = root.GetProperty("right").GetString() ?? "",
                IgnoreCase = root.TryGetProperty("ignore_case", out var ic) && ic.GetBoolean(),
            },
            "for_each" => new ForEachControlKind
            {
                ListVariable = root.GetProperty("list_variable").GetString() ?? "",
                ItemVariable = root.GetProperty("item_variable").GetString() ?? "",
            },
            "while" => new WhileControlKind
            {
                Left = root.GetProperty("left").GetString() ?? "",
                Operator = ParseCompareOperator(root.GetProperty("operator").GetString()),
                Right = root.GetProperty("right").GetString() ?? "",
                IgnoreCase = root.TryGetProperty("ignore_case", out var ic) && ic.GetBoolean(),
            },
            "repeat" => new RepeatControlKind
            {
                Times = root.GetProperty("times").GetString() ?? "",
                IndexVariable = root.GetProperty("index_variable").GetString() ?? "",
            },
            _ => throw new JsonException($"unknown control type `{type}`"),
        };
    }

    public override void Write(Utf8JsonWriter writer, ControlKind value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        switch (value)
        {
            case IfControlKind c:
                writer.WriteString("left", c.Left);
                writer.WriteString("operator", OperatorToJson(c.Operator));
                writer.WriteString("right", c.Right);
                if (c.IgnoreCase)
                    writer.WriteBoolean("ignore_case", true);
                break;
            case ForEachControlKind c:
                writer.WriteString("list_variable", c.ListVariable);
                writer.WriteString("item_variable", c.ItemVariable);
                break;
            case WhileControlKind c:
                writer.WriteString("left", c.Left);
                writer.WriteString("operator", OperatorToJson(c.Operator));
                writer.WriteString("right", c.Right);
                if (c.IgnoreCase)
                    writer.WriteBoolean("ignore_case", true);
                break;
            case RepeatControlKind c:
                writer.WriteString("times", c.Times);
                writer.WriteString("index_variable", c.IndexVariable);
                break;
        }
        writer.WriteEndObject();
    }

    static string OperatorToJson(CompareOperator op) => op switch
    {
        CompareOperator.Eq => "eq",
        CompareOperator.Ne => "ne",
        CompareOperator.Contains => "contains",
        CompareOperator.Gt => "gt",
        CompareOperator.Gte => "gte",
        CompareOperator.Lt => "lt",
        CompareOperator.Lte => "lte",
        _ => throw new JsonException($"unknown operator {op}"),
    };

    static CompareOperator ParseCompareOperator(string? raw) => raw switch
    {
        "eq" => CompareOperator.Eq,
        "ne" => CompareOperator.Ne,
        "contains" => CompareOperator.Contains,
        "gt" => CompareOperator.Gt,
        "gte" => CompareOperator.Gte,
        "lt" => CompareOperator.Lt,
        "lte" => CompareOperator.Lte,
        _ => throw new JsonException($"unknown compare operator `{raw}`"),
    };
}

public static class ControlKindExtensions
{
    public static IfControl AsIf(this ControlKind control) => control switch
    {
        IfControlKind c => new IfControl(c.Left, c.Operator, c.Right, c.IgnoreCase),
        WhileControlKind c => new IfControl(c.Left, c.Operator, c.Right, c.IgnoreCase),
        _ => throw new InvalidOperationException("not an if/while control"),
    };

    public static ForEachControl AsForEach(this ControlKind control) => control switch
    {
        ForEachControlKind c => new ForEachControl(c.ListVariable, c.ItemVariable),
        _ => throw new InvalidOperationException("not for_each"),
    };

    public static RepeatControl AsRepeat(this ControlKind control) => control switch
    {
        RepeatControlKind c => new RepeatControl(c.Times, c.IndexVariable),
        _ => throw new InvalidOperationException("not repeat"),
    };
}
