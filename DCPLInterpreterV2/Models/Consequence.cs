using System.Text.Json.Serialization;

namespace DCPLInterpreterV2.Models;

public class Consequence
{
    [JsonPropertyName("entity")]
    public string? Entity { get; set; }

    [JsonPropertyName("in")]
    public string? In { get; set; }

    [JsonPropertyName("plus")]
    public Plus? Plus { get; set; }
}
