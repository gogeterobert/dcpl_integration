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
    }
}
