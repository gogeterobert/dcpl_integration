using DCPLInterpreterV2.Interfaces;

namespace DCPLInterpreterV2.Services
{
    public class ActionService: IActionService
    {
        private readonly ISchemaService _schemaService;
        private readonly IEntityService _entityService;
        public ActionService(ISchemaService schemaService, IEntityService entityService)
        {
            _schemaService = schemaService;
            _entityService = entityService;
        }

        public bool Act(Guid guid, string action)
        {
            var holderActions = _schemaService.GetHolderActions();
            var entityHolder = _entityService.GetEntityHolder(guid);
            var consequence = _schemaService.GetActionConsequence(action);

            var canAct = holderActions.Exists(holderAction => holderAction.Holder == entityHolder && holderAction.Action == action);

            if (!canAct)
            {
                return canAct;
            }

            //todo update with other consequences
            // _entityService.UpdateEntityHolder(guid, consequence.In);

            return canAct;
        }

        public List<string> GetActionsHolders(string action)
        {
            var holderActions = _schemaService.GetHolderActions();
            return holderActions.FindAll(holderAction => holderAction.Action == action).Select(holderAction => holderAction.Holder).ToList();
        }
    }
}
