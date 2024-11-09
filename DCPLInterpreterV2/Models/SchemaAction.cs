using System.Text.Json.Serialization;

namespace DCPLInterpreterV2.Models;

public class SchemaAction
{
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("refinement")]
    public Refinement? Refinement { get; set; }
}
