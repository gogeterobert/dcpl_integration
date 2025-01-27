// Data/SchemaDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace DCPLInterpreterV2.Infrastructure
{
    public class SchemaDbContext : DbContext
    {
        public SchemaDbContext(DbContextOptions<SchemaDbContext> options) : base(options) { }

        public DbSet<DirectiveEntity> Directives { get; set; }
        public DbSet<Entity> Entities { get; set; }
    }

    public class DirectiveEntity
    {
        public int Id { get; set; }
        public string DirectiveType { get; set; }
        public string JsonData { get; set; }
    }

    public class Entity
    {
        public Guid Id { get; set; }
        public string Holder { get; set; }
    }
}