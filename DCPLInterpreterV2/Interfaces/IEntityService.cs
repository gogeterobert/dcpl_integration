using DCPLInterpreterV2.Infrastructure;

namespace DCPLInterpreterV2.Interfaces;

public interface IEntityService
{
    public Guid Create(string holder);
    public List<Entity> List();
    public string GetEntityHolder(Guid guid);
    public void UpdateEntityHolder(Guid guid, string holder);
    public void Add(Entity entity);
}
