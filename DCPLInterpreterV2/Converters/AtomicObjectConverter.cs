using DCPLInterpreterV2.Models;
using Newtonsoft.Json;

namespace DCPLInterpreterV2.Converter;

public class AtomicObjectConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(AtomicObject).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.String)
        {
            throw new JsonSerializationException("Expected string value.");
        }

        var value = (string)reader.Value;
        var atomicObject = new AtomicObject();

        if (value.StartsWith("#"))
        {
            atomicObject.Type = "#";
            atomicObject.Pattern = value.Substring(1);
        }
        else
        {
            atomicObject.Type = string.Empty;
            atomicObject.Pattern = value;
        }

        return atomicObject;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var atomicObject = (AtomicObject)value;
        var stringValue = atomicObject.Type + atomicObject.Pattern;
        writer.WriteValue(stringValue);
    }
}
