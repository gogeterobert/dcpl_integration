using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DCPLInterpreterV2.Models;

public class FrameJsonConverter : JsonConverter<Frame>
{
    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static JsonElement? GetPropertyCaseInsensitive(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }
        return null;
    }
    public override Frame? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            
            // Create options without THIS converter to avoid infinite recursion, but keep other converters
            var optionsWithoutFrameConverter = new JsonSerializerOptions(options);
            // Remove only the FrameJsonConverter to prevent recursion, but keep EventJsonConverter and others
            var convertersToKeep = optionsWithoutFrameConverter.Converters
                // .Where(c => c.GetType() != typeof(FrameJsonConverter))
                .ToList();
            optionsWithoutFrameConverter.Converters.Clear();
            foreach (var converter in convertersToKeep)
            {
                optionsWithoutFrameConverter.Converters.Add(converter);
            }
            
            if (TryGetPropertyCaseInsensitive(root, "position", out var positionProp))
            {
                var position = positionProp.GetString();
                if (string.Equals(position, "power", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonSerializer.Deserialize<PowerFrame>(root.GetRawText(), optionsWithoutFrameConverter);
                }
                else if (string.Equals(position, "duty", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonSerializer.Deserialize<DutyFrame>(root.GetRawText(), optionsWithoutFrameConverter);
                }
            }
            else if (TryGetPropertyCaseInsensitive(root, "conclusion", out var conclusionProp))
            {
                if (TryGetPropertyCaseInsensitive(conclusionProp, "position", out var transformationalPositionProp))
                {
                    var position = transformationalPositionProp.GetString();
                    Frame? frame = null;
                    if (string.Equals(position, "power", StringComparison.OrdinalIgnoreCase))
                    {
                        frame = JsonSerializer.Deserialize<PowerFrame>(conclusionProp.GetRawText(), optionsWithoutFrameConverter);
                    }
                    else if (string.Equals(position, "duty", StringComparison.OrdinalIgnoreCase))
                    {
                        frame = JsonSerializer.Deserialize<DutyFrame>(conclusionProp.GetRawText(), optionsWithoutFrameConverter);
                    }
                    return new TransformationalFrame
                    {
                        Condition = GetPropertyCaseInsensitive(root, "condition")?.GetString() ?? string.Empty,
                        Conclusion = frame,
                    };
                }
            }
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, Frame value, JsonSerializerOptions options)
    {
        // Create options without THIS converter to avoid infinite recursion, but keep other converters
        var optionsWithoutFrameConverter = new JsonSerializerOptions(options);
        // Remove only the FrameJsonConverter to prevent recursion, but keep EventJsonConverter and others
        var convertersToKeep = optionsWithoutFrameConverter.Converters
            // .Where(c => c.GetType() != typeof(FrameJsonConverter))
            .ToList();
        optionsWithoutFrameConverter.Converters.Clear();
        foreach (var converter in convertersToKeep)
        {
            optionsWithoutFrameConverter.Converters.Add(converter);
        }
        
        if (value is PowerFrame power)
        {
            JsonSerializer.Serialize(writer, power, optionsWithoutFrameConverter);
        }
        else if (value is DutyFrame duty)
        {
            JsonSerializer.Serialize(writer, duty, optionsWithoutFrameConverter);
        }
        else if (value is TransformationalFrame transformational)
        {
            JsonSerializer.Serialize(writer, transformational, optionsWithoutFrameConverter);
        }
        else
        {
            // For base Frame, write empty object
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
