using DCPLInterpreterV2.Models;

namespace DCPLInterpreterV2.Interfaces;

public interface IActionService
{
    public bool Act(string holder, string action);
}
