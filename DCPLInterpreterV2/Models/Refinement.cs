using System.Text.Json.Serialization;

namespace DCPLInterpreterV2.Models;

public class Refinement
{
    [JsonPropertyName("item")]
    public string? Item { get; set; }

    [JsonPropertyName("borrower")]
    public string? Borrower { get; set; }

    [JsonPropertyName("lender")]
    public string? Lender { get; set; }
}
