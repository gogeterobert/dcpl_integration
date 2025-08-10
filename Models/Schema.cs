namespace DCPLInterpreterV2.Models;

public class PowerFrame : Frame
{
    public string Position { get; set; }
    public string Holder { get; set; }
    public string Action { get; set; }
    public Event Consequence { get; set; }
}

public class DutyFrame : Frame
{
    public string Position { get; set; }
    public string Holder { get; set; }
    public string Action { get; set; }
    public string Counterparty { get; set; }
    public EventExpression? Violation { get; set; }
    public EventExpression? Termination { get; set; }
}

public class TransformationalFrame : Frame
{
    public string Condition { get; set; }
    public DutyFrame Conclusion { get; set; }
}

public class EventExpression
{
    public string? Expression { get; set; }
    public string? Event { get; set; }
}

public class Object
{

}

public class Frame
{
}

public class Event
{

}

public class ProductionEvent : Event
{
}

public class PlusProductEvent : Event
{
    public string? Plus { get; set; }
}

public class NamingEvent : Event
{
    public string? Entity { get; set; }
    public string? In { get; set; }
}

public static class PositionTypes
{
    public const string Power = "power";
    public const string Duty = "duty";
} 

