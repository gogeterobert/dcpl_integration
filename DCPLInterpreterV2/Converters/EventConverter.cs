using DCPLInterpreterV2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

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
            return new AtomicEvent { Reference = (string)reader.Value };
        }

        var jsonObject = JObject.Load(reader);
        Event eventObj;

        if (jsonObject["reference"] != null && jsonObject["refinement"] != null)
        {
            eventObj = new RefinedEvent();
        }
        else if (jsonObject["plus"] != null || jsonObject["minus"] != null)
        {
            eventObj = new RefinedEvent();
        }
        else if (jsonObject["entity"] != null && (jsonObject["in"] != null || jsonObject["out"] != null))
        {
            throw new NotImplementedException();
            // eventObj = new NamingEvent();
        }
        else if (jsonObject["reference"] != null)
        {
            eventObj = new AtomicEvent();
        }
        else
        {
            throw new JsonSerializationException("Unknown event type");
        }

        serializer.Populate(jsonObject.CreateReader(), eventObj);
        return eventObj;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
