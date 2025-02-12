using System.ComponentModel.DataAnnotations;
using DCPLInterpreterV2.Infrastructure;
using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using Newtonsoft.Json;

namespace DCPLInterpreterV2.Services
{
    public class SchemaService : ISchemaService
    {
        private readonly SchemaDbContext _context;

        public SchemaService(SchemaDbContext context)
        {
            _context = context;
        }

        public void AddAndReplaceSchema(List<PowerFrame> schema)
        {
            var directiveEntities = schema.Select(directive => new DirectiveEntity
            {
                DirectiveType = directive.GetType().Name,
                JsonData = JsonConvert.SerializeObject(directive)
            }).ToList();

            _context.Directives.RemoveRange(_context.Directives);
            _context.Directives.AddRange(directiveEntities);
            _context.SaveChanges();
        }

        public List<HolderAction> GetHolderActions()
        {
            var directives = _context.Directives.ToList();
            var schema = directives.Select(directiveEntity =>
                JsonConvert.DeserializeObject(directiveEntity.JsonData, typeof(PowerFrame)) as PowerFrame
            ).ToList();

            return schema.SelectMany(directive => new List<HolderAction> { new HolderAction { Holder = directive.Holder, Action = directive.Action?.Reference ?? string.Empty } }).Where(d => !string.IsNullOrEmpty(d.Action)).ToList();
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

            var powerActions = schema.SelectMany(powerFrame => new List<string>
                {
                    powerFrame?.Action?.Reference ?? string.Empty,
                }).Where(a => !string.IsNullOrEmpty(a)).ToList();
             powerActions.AddRange(schema
                .SelectMany(powerFrame => powerFrame?.Conclusion?.Content ?? new List<PowerFrame>())
                .SelectMany(pf => pf.Action?.Reference != null ? new List<string> { pf.Action.Reference } : new List<string>())
                .Where(a => !string.IsNullOrEmpty(a))
                .ToList());

            return powerActions;
        }

        public Event GetActionConsequence(string action)
        {
            var directives = _context.Directives.ToList();
            var schema = directives.Select(directiveEntity =>
                JsonConvert.DeserializeObject(directiveEntity.JsonData, typeof(PowerFrame)) as PowerFrame
            ).ToList();

            return schema.SelectMany(powerFrame => new List<(string action, Event namingEvent)> { (powerFrame?.Action?.Reference, powerFrame?.Consequence) })
                .Where(t => !string.IsNullOrEmpty(t.action) && t.namingEvent is not null)
                .FirstOrDefault(namingEvent => namingEvent.action == action).namingEvent;
        }

        public List<string> ParseAllEntitiesFromSchema()
        {
            var schemaEntities = new List<Entity>();
            var directives = _context.Directives.ToList();
            var schema = directives.Select(directiveEntity =>
                JsonConvert.DeserializeObject(directiveEntity.JsonData, typeof(PowerFrame)) as PowerFrame
            ).ToList();

            schemaEntities.AddRange(schema.Select(powerFrame => new Entity { Id = Guid.NewGuid(), Holder = powerFrame.Holder })
                .Where(e => !string.IsNullOrEmpty(e.Holder))
                .Distinct(new EntityEqualityComparer()).ToList());
            schemaEntities.AddRange(schema.Select(powerFrame => new Entity { Id = Guid.NewGuid(), Holder = powerFrame?.Action?.Refinement?.Item ?? string.Empty })
                .Where(e => !string.IsNullOrEmpty(e.Holder))
                .Distinct(new EntityEqualityComparer()).ToList());

            return schemaEntities.Select(e => e.Holder).ToList();
        }
    }
}
