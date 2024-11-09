using DCPLInterpreterV2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DCPLInterpreterV2.Models;

public class DirectiveConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(IDirective).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        IDirective directive;

        if (jsonObject["condition"] != null && jsonObject["conclusion"] != null)
        {
            directive = new TransformationalRule();
        }
        else if (jsonObject["event"] != null && jsonObject["reaction"] != null)
        {
            directive = new ReactiveRule();
        }
        else if (jsonObject["position"] != null && jsonObject["holder"] != null && jsonObject["action"] != null && jsonObject["consequence"] != null)
        {
            directive = new PowerFrame();
        }
        else if (jsonObject["position"] != null && jsonObject["holder"] != null && jsonObject["action"] != null)
        {
            directive = new DeonticFrame();
        }
        else if (jsonObject["compound"] != null && jsonObject["content"] != null)
        {
            directive = new CompoundFrame();
        }
        else
        {
            throw new JsonSerializationException("Unknown directive type");
        }

        serializer.Populate(jsonObject.CreateReader(), directive);
        return directive;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
