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

            var powerActions = schema.SelectMany(powerFrame => new List<HolderAction>
                {
                    new HolderAction {
                        Holder = powerFrame?.Holder ?? string.Empty,
                        Action = powerFrame?.Action?.Reference ?? string.Empty,
                        Consequence = powerFrame?.Consequence }
                }).Where(d => !string.IsNullOrEmpty(d.Action)).ToList();

            powerActions.AddRange(schema
               .SelectMany(powerFrame => powerFrame?.Conclusion?.Content ?? new List<PowerFrame>())
               .SelectMany(pf => new List<HolderAction>
               {
                    new HolderAction {
                        Holder = pf?.Holder ?? string.Empty,
                        Action = pf?.Action?.Reference ?? string.Empty,
                        Consequence = pf?.Consequence }
               })
               .Where(a => !string.IsNullOrEmpty(a.Action))
               .ToList());

            return powerActions;
        }

        public List<string> GetHolders()
        {
            return GetHolderActions().Select(holderAction => holderAction.Holder).Distinct().ToList();
        }

        public List<string> GetActions()
        {
            return GetHolderActions().Select(holderAction => holderAction.Action).Distinct().ToList();
        }

        public Event GetActionConsequence(string action)
        {
            return GetHolderActions().FirstOrDefault(holderAction => holderAction.Action == action)?.Consequence;
        }

        public List<string> ParseAllEntitiesFromSchema()
        {
            var schemaEntities = new List<Entity>();
            var directives = _context.Directives.ToList();
            var schema = directives.Select(directiveEntity =>
                JsonConvert.DeserializeObject(directiveEntity.JsonData, typeof(PowerFrame)) as PowerFrame
            ).ToList();

            schemaEntities.AddRange(GetHolders().Select(holder => new Entity { Holder = holder }));
            schemaEntities.AddRange(schema.Select(powerFrame => new Entity { Holder = powerFrame?.Action?.Refinement?.Item ?? string.Empty })
                .Where(e => !string.IsNullOrEmpty(e.Holder))
                .Distinct(new EntityEqualityComparer()).ToList());

            return schemaEntities.Select(e => e.Holder).ToList();
        }
    }
}
