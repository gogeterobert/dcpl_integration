namespace DCPLInterpreterV2.Models;

public class ActionHolder
{
    public string Holder { get; set; }
    public string Action { get; set; }
    public string Condition { get; set; }
    public Event? Consequence { get; set; }
    public string? ViolationExpression { get; set; }
}