using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DCPLInterpreterV2.Models;

public class FrameJsonConverter : JsonConverter<Frame>
{
    public override Frame? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("position", out var positionProp))
            {
                var position = positionProp.GetString();
                if (string.Equals(position, "power", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonSerializer.Deserialize<PowerFrame>(root.GetRawText(), options);
                }
                else if (string.Equals(position, "duty", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonSerializer.Deserialize<DutyFrame>(root.GetRawText(), options);
                }
            }
            else if (root.TryGetProperty("condition", out var conditionProp))
            {
                var frame = new TransformationalFrame();
                frame.Condition = conditionProp.GetString();

                if (root.TryGetProperty("conclusion", out var conclusionProp))
                {
                    var conclusionJson = conclusionProp.GetRawText();
                    frame.Conclusion = JsonSerializer.Deserialize<DutyFrame>(conclusionJson, options);
                }
                return frame;
            }
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, Frame value, JsonSerializerOptions options)
    {
        if (value is PowerFrame power)
        {
            JsonSerializer.Serialize(writer, power, options);
        }
        else if (value is DutyFrame duty)
        {
            JsonSerializer.Serialize(writer, duty, options);
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
