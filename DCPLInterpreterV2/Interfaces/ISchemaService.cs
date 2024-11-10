using DCPLInterpreterV2.Models;

namespace DCPLInterpreterV2.Interfaces;

public interface ISchemaService
{
    public void AddSchema(List<IDirective> schema);
    List<string> GetHolders();
    List<string> GetActions();
}
