using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DCPLInterpreterV2.Models;

public class TransformationalFrameJsonConverter : JsonConverter<TransformationalFrame>
{
    public override TransformationalFrame? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            var frame = new TransformationalFrame();
            
            // Create options without converters to avoid infinite recursion
            var optionsWithoutConverters = new JsonSerializerOptions(options);
            optionsWithoutConverters.Converters.Clear();
            
            if (root.TryGetProperty("condition", out var condProp))
            {
                frame.Condition = condProp.GetString() ?? string.Empty;
            }
            if (root.TryGetProperty("conclusion", out var conclProp))
            {
                // Use FrameJsonConverter for conclusion
                var conclusion = JsonSerializer.Deserialize<DutyFrame>(conclProp.GetRawText(), optionsWithoutConverters);
                if (conclusion != null)
                {
                    frame.Conclusion = conclusion;
                }
            }
            return frame;
        }
    }

    public override void Write(Utf8JsonWriter writer, TransformationalFrame value, JsonSerializerOptions options)
    {
        // Create options without converters to avoid infinite recursion
        var optionsWithoutConverters = new JsonSerializerOptions(options);
        optionsWithoutConverters.Converters.Clear();
        
        writer.WriteStartObject();
        writer.WriteString("condition", value.Condition);
        writer.WritePropertyName("conclusion");
        JsonSerializer.Serialize(writer, value.Conclusion, optionsWithoutConverters);
        writer.WriteEndObject();
    }
}
