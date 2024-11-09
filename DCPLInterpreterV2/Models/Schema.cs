using System.Text.Json.Serialization;

namespace DCPLInterpreterV2.Models;

public class Schema
{
    public List<Record> Records { get; set; } = new List<Record>();
}
