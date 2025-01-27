using DCPLInterpreterV2.Infrastructure;
using DCPLInterpreterV2.Interfaces;

namespace DCPLInterpreterV2.Services
{
    public class EntityService: IEntityService
    {
        private readonly SchemaDbContext _context;
        private readonly ISchemaService _schemaService;

        public EntityService(SchemaDbContext context, ISchemaService schemaService)
        {
            _context = context;
            _schemaService = schemaService;
        }

        public Guid Create(string holder)
        {
            var validHolders = _schemaService.GetHolders();

            if (!validHolders.Contains(holder))
            {
                throw new ArgumentException("Invalid holder");
            }

            var entity = new Entity { Id = Guid.NewGuid(), Holder = holder };
            _context.Entities.Add(entity);
            _context.SaveChanges();

            return entity.Id;
        }

        public void Add(Entity entity)
        {
            _context.Entities.Add(entity);
            _context.SaveChanges();
        }

        public List<Entity> List()
        {
            return _context.Entities.ToList();
        }

        public string GetEntityHolder(Guid guid)
        {
            var entity = _context.Entities.FirstOrDefault(entity => entity.Id == guid)?.Holder;

            if (entity == null)
            {
                throw new KeyNotFoundException("Entity not found");
            }

            return entity;
        }

        public void UpdateEntityHolder(Guid guid, string holder)
        {
            var entity = _context.Entities.FirstOrDefault(entity => entity.Id == guid);

            if (entity == null)
            {
                throw new KeyNotFoundException("Entity not found");
            }

            entity.Holder = holder;
        }
    }
}
