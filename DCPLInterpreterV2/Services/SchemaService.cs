using DCPLInterpreterV2.Infrastructure;
using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using Newtonsoft.Json;

namespace DCPLInterpreterV2.Services
{
    public class SchemaService: ISchemaService
    {
        private readonly SchemaDbContext _context;

        public SchemaService(SchemaDbContext context)
        {
            _context = context;
        }
    
        public void AddSchema(List<PowerFrame> schema)
        {
            var directiveEntities = schema.Select(directive => new DirectiveEntity
            {
                DirectiveType = directive.GetType().Name,
                JsonData = JsonConvert.SerializeObject(directive)
            }).ToList();

            _context.Directives.AddRange(directiveEntities);
            _context.SaveChanges();
        }

        public List<HolderAction> GetHolderActions()
        {
            var directives = _context.Directives.ToList();
            var schema = directives.Select(directiveEntity =>
                JsonConvert.DeserializeObject(directiveEntity.JsonData, typeof(PowerFrame)) as PowerFrame
            ).ToList();
            
            return schema.SelectMany(directive => new List<HolderAction> {new HolderAction{ Holder = directive.Holder, Action = directive.Action.Reference}}).ToList();
        }

        public List<string> GetHolders()
        {
            return GetHolderActions().Select(holderAction => holderAction.Holder).Distinct().ToList();
        }

        public List<string> GetActions()
        {
            var directives = _context.Directives.ToList();
            var schema = directives.Select(directiveEntity =>
                JsonConvert.DeserializeObject(directiveEntity.JsonData, typeof(PowerFrame)) as PowerFrame
            ).ToList();

            return schema.SelectMany(powerFrame => new List<string> {powerFrame.Action.Reference}).ToList();
        }

        public Event GetActionConsequence(string action)
        {
            var directives = _context.Directives.ToList();
            var schema = directives.Select(directiveEntity =>
                JsonConvert.DeserializeObject(directiveEntity.JsonData, typeof(PowerFrame)) as PowerFrame
            ).ToList();

            return schema.SelectMany(powerFrame => new List<(string action, Event namingEvent)> {(powerFrame.Action.Reference, powerFrame.Consequence)}).FirstOrDefault(namingEvent => namingEvent.action == action).namingEvent;
        }
    }
}
