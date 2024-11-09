using System.Text.Json.Serialization;

namespace DCPLInterpreterV2.Models;

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
