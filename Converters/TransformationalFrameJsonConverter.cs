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
            if (root.TryGetProperty("condition", out var condProp))
            {
                frame.Condition = condProp.GetString();
            }
            // if (root.TryGetProperty("conclusion", out var conclProp))
            // {
            //     // Use FrameJsonConverter for conclusion
            //     var conclusion = JsonSerializer.Deserialize<Frame>(conclProp.GetRawText(), options);
            //     frame.Conclusion = conclusion;
            // }
            return frame;
        }
    }

    public override void Write(Utf8JsonWriter writer, TransformationalFrame value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("condition", value.Condition);
        writer.WritePropertyName("conclusion");
        JsonSerializer.Serialize(writer, value.Conclusion, options);
        writer.WriteEndObject();
    }
}
