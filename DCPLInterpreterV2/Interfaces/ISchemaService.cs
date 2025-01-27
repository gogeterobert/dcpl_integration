using DCPLInterpreterV2.Models;

namespace DCPLInterpreterV2.Interfaces;

public interface ISchemaService
{
    public void AddSchema(List<PowerFrame> schema);
    List<string> GetHolders();
    List<HolderAction> GetHolderActions();
    List<string> GetActions();
    Event GetActionConsequence(string action);
}
