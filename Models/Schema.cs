using System.Text.Json.Serialization;
using DCPLInterpreterV2.Converter;

namespace DCPLInterpreterV2.Models;

public class PowerFrame
{
    public string? Condition { get; set; }
    public string? Position { get; set; }
    public string? Holder  { get; set; }
    
    
    [JsonConverter(typeof(EventConverter))]
    public Event? Action { get; set; }

    [JsonConverter(typeof(EventConverter))]
    public Event? Consequence { get; set; }
    public CompoundFrame? Conclusion { get; set; }
}

public class ExternalExpression
{
    public string Type { get; set; }
}

public class AtomicObject
{
    public string Type { get; set; }
    public string Pattern { get; set; }
}

public class CompoundFrame
{
    public string Compound { get; set; } = string.Empty;
    public List<string> Params = new List<string>();
    public List<PowerFrame> Content = new List<PowerFrame>();
}

public class RefinedObject
{
    public AtomicObject Reference { get; set; }
    public Refinement Refinement { get; set; }
    public AtomicObject Alias { get; set; }
}

public class Object
{
    public AtomicObject AtomicObject { get; set; }
    public RefinedObject RefinedObject { get; set; }
    public PowerFrame PowerFrame { get; set; }
}

public class Event
{
    // Atomic event
    public string? Reference { get; set; }

    // Refined event
    public Refinement? Refinement { get; set; }
    public string? Alias { get; set; }

    // Naming event
    public string? Entity { get; set; }
    public string? In { get; set; }
    public Object? Out { get; set; }
}

public class Refinement
{
    public string? Item { get; set; }
    public string? Type { get; set; }
}