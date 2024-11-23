using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;

namespace DCPLInterpreterV2.Services
{
    public class ActionService: IActionService
    {
        private readonly ISchemaService _schemaService;
        public ActionService(ISchemaService schemaService)
        {
            _schemaService = schemaService;
        }

        public bool Act(string holder, string action)
        {
            var holderActions = _schemaService.GetHolderActions();

            return holderActions.Exists(holderAction => holderAction.Holder == holder && holderAction.Action == action);
        }
    }
}
