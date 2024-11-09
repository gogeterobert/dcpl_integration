using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace DCPLInterpreter.Web.Endpoints;

[Route("api/[controller]")]
public class SchemaController : ControllerBase
{
    [HttpPost]
    public ActionResult Index([FromBody] List<Record> records)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringOrObjectConverter() }
        };

        try
        {
            // Deserialize JSON into a list of Record objects
            // var records = JsonSerializer.Deserialize<List<Record>>(jsonElement, options);

            // Do something with the parsed records, such as logging or returning a response
            return Ok(records);
        }
        catch (JsonException ex)
        {
            // Handle JSON parsing errors
            return BadRequest($"JSON Parsing Error: {ex.Message}");
        }
    }
}

public class JsonStringOrObjectConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<Action>(ref reader, options) ?? new Action();
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        throw new JsonException("Unexpected token type");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is string strValue)
        {
            writer.WriteStringValue(strValue);
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}

public class Action
{
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("refinement")]
    public Refinement? Refinement { get; set; }
}

public class Consequence
{
    [JsonPropertyName("entity")]
    public string? Entity { get; set; }

    [JsonPropertyName("in")]
    public string? In { get; set; }

    [JsonPropertyName("plus")]
    public Plus? Plus { get; set; }
}

public class Plus
{
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("refinement")]
    public Refinement? Refinement { get; set; }
}

public class Refinement
{
    [JsonPropertyName("item")]
    public string? Item { get; set; }

    [JsonPropertyName("borrower")]
    public string? Borrower { get; set; }

    [JsonPropertyName("lender")]
    public string? Lender { get; set; }
}

public class Record
{
    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    [JsonPropertyName("holder")]
    public string Holder { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public object Action { get; set; } = string.Empty;

    [JsonPropertyName("consequence")]
    public Consequence Consequence { get; set; } = new Consequence();
}
