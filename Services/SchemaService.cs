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

            powerActions.AddRange(schema
               .Select(powerFrame => powerFrame?.Conclusion)
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

        public string GenerateFromTemplate(string projectName)
        {
            if (projectName == null || string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Invalid project details. 'Name' is required.");
            }

            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var projectPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName);

            // Remove old project directory if it exists
            if (Directory.Exists(projectPath))
            {
                const int maxRetries = 5;
                const int delayMs = 500;
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        Directory.Delete(projectPath, true); // true = recursive delete
                        _logger.LogInformation($"Old project at {projectPath} deleted.");
                        break;
                    }
                    catch (IOException)
                    {
                        if (i == maxRetries - 1) throw;
                        Thread.Sleep(delayMs);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        if (i == maxRetries - 1) throw;
                        Thread.Sleep(delayMs);
                    }
                }
            }

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

            // Ensure entity name is a valid C# identifier and starts with uppercase
            var validEntityName = string.Concat(entityName.Where(char.IsLetterOrDigit));
            if (string.IsNullOrWhiteSpace(validEntityName))
                throw new ArgumentException("Entity name must contain at least one letter or digit.");
            validEntityName = char.ToUpper(validEntityName[0]) + validEntityName.Substring(1);

            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var entitiesPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Domain", "Entities");
            Directory.CreateDirectory(entitiesPath);

            var entityFilePath = Path.Combine(entitiesPath, $"{validEntityName}.cs");

            var entityClass = $@"using {projectName}.Domain.Common;

            namespace {projectName}.Domain.Entities
            {{
                public class {validEntityName} : BaseAuditableEntity
                {{
                    public string Name {{ get; set; }} = string.Empty;
                }}
            }}";

            File.WriteAllText(entityFilePath, entityClass);

            // Add DbSet to ApplicationDbContext.cs
            AddDbSetToDbContext(validEntityName, projectName, parentDir);
        }

        private void AddDbSetToDbContext(string validEntityName, string projectName, string? parentDir)
        {
            var dbContextPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Infrastructure", "Data", "ApplicationDbContext.cs");
            if (File.Exists(dbContextPath))
            {
                var dbContextText = File.ReadAllText(dbContextPath);
                var dbSetLine = $"    public DbSet<{validEntityName}> {validEntityName}s => Set<{validEntityName}>();";
                if (!dbContextText.Contains(dbSetLine))
                {
                    // Find last DbSet or constructor
                    var lines = dbContextText.Split('\n').ToList();
                    int insertIndex = lines.FindLastIndex(l => l.Contains("DbSet<"));
                    if (insertIndex == -1)
                    {
                        // Fallback: after constructor
                        insertIndex = lines.FindIndex(l => l.Contains("public ApplicationDbContext"));
                        while (insertIndex < lines.Count && !lines[insertIndex].Contains("{")) insertIndex++;
                        insertIndex++;
                    }
                    else
                    {
                        insertIndex++;
                    }
                    lines.Insert(insertIndex, dbSetLine);
                    File.WriteAllText(dbContextPath, string.Join("\n", lines));
                }
            }
        }
    }
}
