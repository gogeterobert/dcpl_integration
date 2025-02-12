namespace DCPLInterpreterV2.Models;

public class HolderAction
{
    public string Holder { get; set; }
    public string Action { get; set; }
    public Event? Consequence { get; set; }
}