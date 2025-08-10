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
            
            // Create options without converters to avoid infinite recursion
            var optionsWithoutConverters = new JsonSerializerOptions(options);
            optionsWithoutConverters.Converters.Clear();
            
            if (root.TryGetProperty("position", out var positionProp))
            {
                var position = positionProp.GetString();
                if (string.Equals(position, "power", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonSerializer.Deserialize<PowerFrame>(root.GetRawText(), optionsWithoutConverters);
                }
                else if (string.Equals(position, "duty", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonSerializer.Deserialize<DutyFrame>(root.GetRawText(), optionsWithoutConverters);
                }
            }
            else if (root.TryGetProperty("condition", out var conditionProp))
            {
                return JsonSerializer.Deserialize<TransformationalFrame>(root.GetRawText(), optionsWithoutConverters);
            }
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, Frame value, JsonSerializerOptions options)
    {
        // Create options without the converters to avoid infinite recursion
        var optionsWithoutConverters = new JsonSerializerOptions(options);
        optionsWithoutConverters.Converters.Clear();
        
        if (value is PowerFrame power)
        {
            JsonSerializer.Serialize(writer, power, optionsWithoutConverters);
        }
        else if (value is DutyFrame duty)
        {
            JsonSerializer.Serialize(writer, duty, optionsWithoutConverters);
        }
        else if (value is TransformationalFrame transformational)
        {
            JsonSerializer.Serialize(writer, transformational, optionsWithoutConverters);
        }
        else
        {
            // For base Frame, write empty object
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
