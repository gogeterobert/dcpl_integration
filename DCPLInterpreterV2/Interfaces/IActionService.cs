using DCPLInterpreterV2.Models;

namespace DCPLInterpreterV2.Interfaces;

public interface IActionService
{
    public bool Act(Guid guid, string action);
    public List<string> GetActionsHolders(string action);
}
