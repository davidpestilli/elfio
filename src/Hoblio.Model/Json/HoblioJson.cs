using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hoblio.Model.Json;

public static class HoblioJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(SnakeCaseNamingPolicy.Instance));
        options.Converters.Add(new ActionKindJsonConverter());
        options.Converters.Add(new ControlKindJsonConverter());
        return options;
    }
}
