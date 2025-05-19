using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using DCPLInterpreterV2.Infrastructure;
using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using Newtonsoft.Json;

namespace DCPLInterpreterV2.Services
{
    public class SchemaService : ISchemaService
    {
        private readonly SchemaDbContext _context;
        private readonly ILogger<SchemaService> _logger;

        public SchemaService(SchemaDbContext context, ILogger<SchemaService> logger)
        {
            _context = context;
            _logger = logger;
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

            // powerActions.AddRange(schema
            //    .SelectMany(powerFrame => powerFrame?.Conclusion?.Content ?? new List<PowerFrame>())
            //    .SelectMany(pf => new List<HolderAction>
            //    {
            //         new HolderAction {
            //             Holder = pf?.Holder ?? string.Empty,
            //             Action = pf?.Action?.Reference ?? string.Empty,
            //             Consequence = pf?.Consequence }
            //    })
            //    .Where(a => !string.IsNullOrEmpty(a.Action))
            //    .ToList());

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
            // schemaEntities.AddRange(schema.Select(powerFrame => new Entity { Holder = powerFrame?.Action?.Refinement?.Item ?? string.Empty })
            //     .Where(e => !string.IsNullOrEmpty(e.Holder))
            //     .Distinct(new EntityEqualityComparer()).ToList());

            return schemaEntities.Select(e => e.Holder).ToList();
        }

        public string GenerateFromTemplate(string projectName)
        {
            if (projectName == null || string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Invalid project details. 'Name' is required.");
            }

            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var projectPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new ca-sln -cf None --database sqlite -o {projectPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Error generating project: {error}");
                }

                _logger.LogInformation($"Project generated successfully at {projectPath}: {output}");
            }

            return projectPath;
        }

        public void CreateNewEntityInGeneratedSolution(string entityName, string projectName)
        {
            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(projectName))
                throw new ArgumentException("Entity name and project name must be provided.");

            // Ensure entity name is valid C# identifier
            var validEntityName = string.Concat(entityName.Where(char.IsLetterOrDigit));
            if (string.IsNullOrWhiteSpace(validEntityName))
                throw new ArgumentException("Entity name must contain at least one letter or digit.");

            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var entitiesPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Domain", "Entities");
            Directory.CreateDirectory(entitiesPath);

            var entityFilePath = Path.Combine(entitiesPath, $"{validEntityName}.cs");

            var entityClass = $@"using System;\n\nnamespace {projectName}.Domain.Entities\n{{\n    public class {validEntityName}\n    {{\n        public Guid Id {{ get; set; }}\n        public string Name {{ get; set; }}\n    }}\n}}\n";

            File.WriteAllText(entityFilePath, entityClass);
        }
    }
}
