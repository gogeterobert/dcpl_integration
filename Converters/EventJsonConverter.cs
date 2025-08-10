using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DCPLInterpreterV2.Models;

public class EventJsonConverter : JsonConverter<Event>
{
    public override Event? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                // Check for empty object
                if (root.GetRawText() == "{}")
                    return new Event();

                if (root.TryGetProperty("Entity", out _))
                {
                    // NamingEvent
                    var optionsWithoutConverters = new JsonSerializerOptions(options);
                    optionsWithoutConverters.Converters.Clear();
                    return JsonSerializer.Deserialize<NamingEvent>(root.GetRawText(), optionsWithoutConverters);
                }
                if (root.TryGetProperty("Plus", out _))
                {
                    // PlusProductEvent
                    var optionsWithoutConverters = new JsonSerializerOptions(options);
                    optionsWithoutConverters.Converters.Clear();
                    return JsonSerializer.Deserialize<PlusProductEvent>(root.GetRawText(), optionsWithoutConverters);
                }
                
                // If no known properties, treat as base Event
                return new Event();
            }
            else if (root.ValueKind == JsonValueKind.String)
            {
                // Could be a simple event string, fallback to base Event
                return new Event();
            }
            // fallback
            return new Event();
        }
    }

    public override void Write(Utf8JsonWriter writer, Event value, JsonSerializerOptions options)
    {
        // Create options without the converters to avoid infinite recursion
        var optionsWithoutConverters = new JsonSerializerOptions(options);
        optionsWithoutConverters.Converters.Clear();
        
        if (value is NamingEvent naming)
        {
            JsonSerializer.Serialize(writer, naming, optionsWithoutConverters);
        }
        else if (value is PlusProductEvent plus)
        {
            JsonSerializer.Serialize(writer, plus, optionsWithoutConverters);
        }
        else
        {
            // For base Event, write empty object
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
