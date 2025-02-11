// Data/SchemaDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace DCPLInterpreterV2.Infrastructure
{
    public class SchemaDbContext : DbContext
    {
        public SchemaDbContext(DbContextOptions<SchemaDbContext> options) : base(options) { }

        public DbSet<DirectiveEntity> Directives { get; set; }
        public DbSet<Entity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var converter = new ValueConverter<Dictionary<string, string>, string>(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Dictionary<string, string>>(v));

            modelBuilder.Entity<Entity>()
                .Property(e => e.Attributes)
                .HasConversion(converter);
        }
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
        public Dictionary<string, string> Attributes { get; set; }
    }
}