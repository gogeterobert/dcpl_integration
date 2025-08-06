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
                if (root.TryGetProperty("entity", out _))
                {
                    // NamingEvent
                    return JsonSerializer.Deserialize<NamingEvent>(root.GetRawText(), options);
                }
                if (root.TryGetProperty("plus", out _))
                {
                    // PlusProductEvent
                    return JsonSerializer.Deserialize<PlusProductEvent>(root.GetRawText(), options);
                }
            }
            else if (root.ValueKind == JsonValueKind.String)
            {
                // Could be a simple event string, fallback to base Event
                return new Event();
            }
            // fallback
            return JsonSerializer.Deserialize<Event>(root.GetRawText(), options);
        }
    }

    public override void Write(Utf8JsonWriter writer, Event value, JsonSerializerOptions options)
    {
        if (value is NamingEvent naming)
        {
            JsonSerializer.Serialize(writer, naming, options);
        }
        else if (value is PlusProductEvent plus)
        {
            JsonSerializer.Serialize(writer, plus, options);
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
