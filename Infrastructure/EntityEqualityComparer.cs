namespace DCPLInterpreterV2.Infrastructure
{
    public class EntityEqualityComparer : IEqualityComparer<Entity>
    {
        public bool Equals(Entity x, Entity y)
        {
            if (x == null || y == null)
                return false;

            return x.Holder == y.Holder;
        }

        public int GetHashCode(Entity obj)
        {
            return obj?.Holder?.GetHashCode() ?? 0;
        }
    }
}