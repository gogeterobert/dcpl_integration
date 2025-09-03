namespace DCPLInterpreterV2.Models;

public class ActionHolder
{
    public string Holder { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public Event? Consequence { get; set; }
    public string? ViolationExpression { get; set; }
    public string? ViolationEvent { get; set; }
}