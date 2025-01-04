using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;

namespace DCPLInterpreterV2.Services
{
    public class EntityService: IEntityService
    {
        private static List<Entity> _entities {get; set;} = new List<Entity>();

        private readonly ISchemaService _schemaService;
        public EntityService(ISchemaService schemaService)
        {
            _schemaService = schemaService;
        }

        public Guid Create(string holder)
        {
            var validHolders = _schemaService.GetHolders();

            if (!validHolders.Contains(holder))
            {
                throw new ArgumentException("Invalid holder");
            }

            var entity = new Entity { Guid = Guid.NewGuid(), Holder = holder };
            _entities.Add(entity);
            return entity.Guid;
        }

        public void Add(Entity entity)
        {
            _entities.Add(entity);
        }

        public List<Entity> List()
        {
            return _entities;
        }

        public string GetEntityHolder(Guid guid)
        {
            var entity = _entities.Find(entity => entity.Guid == guid)?.Holder;

            if (entity == null)
            {
                throw new KeyNotFoundException("Entity not found");
            }

            return entity;
        }

        public void UpdateEntityHolder(Guid guid, string holder)
        {
            var entity = _entities.Find(entity => entity.Guid == guid);

            if (entity == null)
            {
                throw new KeyNotFoundException("Entity not found");
            }

            entity.Holder = holder;
        }
    }
}
