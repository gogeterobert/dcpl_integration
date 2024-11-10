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
            return _schema.SelectMany(directive => directive switch
            {
                DeonticFrame deonticFrame => new List<string> {deonticFrame.Holder},
                PowerFrame powerFrame => new List<string> {powerFrame.Holder},
                _ => new List<string>()
            }).ToList();
        }
    }
}
