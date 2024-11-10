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

        public List<string> GetHolders()
        {
            if (_schema == null)
            {
                return new List<string>();
            }
            
            return _schema.SelectMany(directive => directive switch
            {
                DeonticFrame deonticFrame => new List<string> {deonticFrame.Holder},
                PowerFrame powerFrame => new List<string> {powerFrame.Holder},
                _ => new List<string>()
            }).ToList();
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
