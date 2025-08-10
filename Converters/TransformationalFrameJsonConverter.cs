using System;
using System.Linq;
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
            
            // Create options without THIS converter to avoid infinite recursion, but keep other converters
            var optionsWithoutTransformationalConverter = new JsonSerializerOptions(options);
            // Remove only the TransformationalFrameJsonConverter to prevent recursion
            var convertersToKeep = optionsWithoutTransformationalConverter.Converters
                .Where(c => c.GetType() != typeof(TransformationalFrameJsonConverter))
                .ToList();
            optionsWithoutTransformationalConverter.Converters.Clear();
            foreach (var converter in convertersToKeep)
            {
                optionsWithoutTransformationalConverter.Converters.Add(converter);
            }
            
            if (root.TryGetProperty("condition", out var condProp))
            {
                frame.Condition = condProp.GetString() ?? string.Empty;
            }
            if (root.TryGetProperty("conclusion", out var conclProp))
            {
                // Use FrameJsonConverter for conclusion
                var conclusion = JsonSerializer.Deserialize<DutyFrame>(conclProp.GetRawText(), optionsWithoutTransformationalConverter);
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
        // Create options without THIS converter to avoid infinite recursion, but keep other converters
        var optionsWithoutTransformationalConverter = new JsonSerializerOptions(options);
        // Remove only the TransformationalFrameJsonConverter to prevent recursion
        var convertersToKeep = optionsWithoutTransformationalConverter.Converters
            .Where(c => c.GetType() != typeof(TransformationalFrameJsonConverter))
            .ToList();
        optionsWithoutTransformationalConverter.Converters.Clear();
        foreach (var converter in convertersToKeep)
        {
            optionsWithoutTransformationalConverter.Converters.Add(converter);
        }
        
        writer.WriteStartObject();
        writer.WriteString("condition", value.Condition);
        writer.WritePropertyName("conclusion");
        JsonSerializer.Serialize(writer, value.Conclusion, optionsWithoutTransformationalConverter);
        writer.WriteEndObject();
    }
}
