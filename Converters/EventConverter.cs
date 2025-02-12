using DCPLInterpreterV2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DCPLInterpreterV2.Converter;

public class EventConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Event).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            return new Event { Reference = (string)reader.Value };
        }

        var jsonObject = JObject.Load(reader);
        Event eventObj = new Event();
        serializer.Populate(jsonObject.CreateReader(), eventObj);
        return eventObj;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Event eventObj = (Event)value;

        if (!string.IsNullOrEmpty(eventObj.Reference))
        {
            writer.WriteValue(eventObj.Reference);
        }
        else
        {
            JObject jsonObject = JObject.FromObject(value, serializer);
            jsonObject.WriteTo(writer);
        }
    }
}
