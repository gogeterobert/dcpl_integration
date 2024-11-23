using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;

namespace DCPLInterpreterV2.Services
{
    public class SchemaService: ISchemaService
    {
        private static List<IDirective> _schema { get; set; }
    
        public void AddSchema(List<IDirective> schema)
        {
            _schema = schema;
        }

        public List<HolderAction> GetHolderActions()
        {
            if (_schema == null)
            {
                return new List<HolderAction>();
            }
            
            return _schema.SelectMany(directive => directive switch
            {
                DeonticFrame deonticFrame => new List<HolderAction> { new HolderAction{ Holder = deonticFrame.Holder, Action = deonticFrame.Action.Reference}},
                PowerFrame powerFrame => new List<HolderAction> {new HolderAction{ Holder = powerFrame.Holder, Action = powerFrame.Action.Reference}},
                _ => new List<HolderAction>()
            }).ToList();
        }

        public List<string> GetHolders()
        {
            return GetHolderActions().Select(holderAction => holderAction.Holder).Distinct().ToList();
        }

        public List<string> GetActions()
        {
            if (_schema == null)
            {
                return new List<string>();
            }

            return _schema.SelectMany(directive => directive switch
            {
                DeonticFrame deonticFrame => new List<string> {deonticFrame.Action.Reference},
                PowerFrame powerFrame => new List<string> {powerFrame.Action.Reference},
                _ => new List<string>()
            }).ToList();
        }
    }
}
