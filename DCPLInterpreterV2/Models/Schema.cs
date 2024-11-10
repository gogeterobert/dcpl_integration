using DCPLInterpreterV2.Converter;
using Newtonsoft.Json;

namespace DCPLInterpreterV2.Models;

[JsonConverter(typeof(DirectiveConverter))]
public interface IDirective
{
}

public class TransformationalRule : IDirective
{
    public Object Condition { get; set; }
    public Object Conclusion { get; set; }
    public AtomicObject Alias { get; set; }
}

public class ReactiveRule : IDirective
{
    public Event Event { get; set; }
    public TransitionEvent Reaction { get; set; }
    public AtomicObject Alias { get; set; }
}

public class DeonticFrame : IDirective
{
    public string Position { get; set; }
    public string Holder  { get; set; }
    public Object Counterparty { get; set; }
    public Event Action { get; set; }
    public Violation Violation { get; set; }
    public Termination Termination { get; set; }
    public AtomicObject Alias { get; set; }
}

public class PowerFrame : IDirective
{
    public string Position { get; set; }
    public string Holder  { get; set; }
    
    [JsonConverter(typeof(EventConverter))]
    public Event Action { get; set; }
    public TransitionEvent Consequence { get; set; }
    public AtomicObject Alias { get; set; }
}

public class CompoundFrame : IDirective
{
    public AtomicObject Compound { get; set; }
    public List<AtomicParams> Params { get; set; }
    public List<IDirective> Content { get; set; }
}

public class ExternalExpression
{
    public string Type { get; set; }
}

[JsonConverter(typeof(AtomicObjectConverter))]
public class AtomicObject
{
    public string Type { get; set; }
    public string Pattern { get; set; }
}

public class AtomicParams
{
    public AtomicEvent AtomicEvent { get; set; }
    public AtomicObject AtomicObject { get; set; }
}

public class Refinement
{
    public object Type { get; set; }
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
    public DeonticFrame DeonticFrame { get; set; }
}

public abstract class Event
{
}

public class AtomicEvent : Event
{
    public string Reference { get; set; }
}

public class RefinedEvent : Event
{
    public AtomicEvent Reference { get; set; }
    public Refinement Refinement { get; set; }
    public AtomicEvent Alias { get; set; }
}

public abstract class TransitionEvent : Event
{
}

public class ProductionEvent : TransitionEvent
{
    public Object Plus { get; set; }
    public Object Minus { get; set; }
}

public class NamingEvent : TransitionEvent
{
    public string Entity { get; set; }
    public string In { get; set; }
    public Object Out { get; set; }
}

public class Violation
{
    public ExternalExpression Expression { get; set; }
    public Event Event { get; set; }
    public AtomicObject Alias { get; set; }
}

public class Termination
{
    public ExternalExpression Expression { get; set; }
    public Event Event { get; set; }
}
