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

        private void CreateController(string validHolderName, string validActionName, string projectName, string? parentDir, List<string> diLines, ref int addInfraIndex, string diPath)
        {
            var controllersPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Web", "Controllers");
            Directory.CreateDirectory(controllersPath);
            var controllerFilePath = Path.Combine(controllersPath, $"{validActionName}Controller.cs");
            var controllerClass = $@"using MediatR;
using Microsoft.AspNetCore.Mvc;
using {projectName}.Application.{validActionName}.Commands;

namespace {projectName}.Web.Controllers
{{
    [ApiController]
    [Route(""api/{validActionName}"")]
    public class {validHolderName}Controller : ControllerBase
    {{
        private readonly IMediator _mediator;
        public {validHolderName}Controller(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Create{validActionName}Command command)
        {{
            var result = await _mediator.Send(command);
            return Ok(result);
        }}
    }}
}}";
            File.WriteAllText(controllerFilePath, controllerClass);

            CreateMediatRCommandAndHandler(validActionName, projectName, parentDir);
            CreateApplicationInterface(validActionName, projectName, parentDir);
            CreateInfrastructureImplementation(validActionName, projectName, parentDir);
            RegisterServiceInDependencyInjection(validActionName, projectName, diLines, ref addInfraIndex, diPath);
        }

        private void CreateMediatRCommandAndHandler(string validActionName, string projectName, string? parentDir)
        {
            var commandsPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", validActionName, "Commands");
            Directory.CreateDirectory(commandsPath);
            var commandFilePath = Path.Combine(commandsPath, $"Create{validActionName}Command.cs");
            var handlerFilePath = Path.Combine(commandsPath, $"Create{validActionName}CommandHandler.cs");

            var commandClass = $@"using MediatR;

namespace {projectName}.Application.{validActionName}.Commands
{{
    public record Create{validActionName}Command(string Name) : IRequest<Guid>;
}}";
            File.WriteAllText(commandFilePath, commandClass);

            var handlerClass = $@"using MediatR;
using {projectName}.Application.Interfaces;

namespace {projectName}.Application.{validActionName}.Commands
{{
    public class Create{validActionName}CommandHandler : IRequestHandler<Create{validActionName}Command, Guid>
    {{
        private readonly I{validActionName}Service _service;
        public Create{validActionName}CommandHandler(I{validActionName}Service service) => _service = service;

        public async Task<Guid> Handle(Create{validActionName}Command request, CancellationToken cancellationToken)
        {{
            return await _service.Create{validActionName}Async(request.Name);
        }}
    }}
}}";
            File.WriteAllText(handlerFilePath, handlerClass);
        }

        private void CreateApplicationInterface(string validActionName, string projectName, string? parentDir)
        {
            var appInterfacesPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", "Interfaces");
            Directory.CreateDirectory(appInterfacesPath);
            var interfaceFilePath = Path.Combine(appInterfacesPath, $"I{validActionName}Service.cs");
            var interfaceClass = $@"using System.Threading.Tasks;

namespace {projectName}.Application.Interfaces
{{
    public interface I{validActionName}Service
    {{
        Task<Guid> Create{validActionName}Async(string name);
    }}
}}";
            File.WriteAllText(interfaceFilePath, interfaceClass);
        }

        private void CreateInfrastructureImplementation(string validActionName, string projectName, string? parentDir)
        {
            var infraPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Infrastructure");
            Directory.CreateDirectory(infraPath);
            var infraImplPath = Path.Combine(infraPath, $"{validActionName}Service.cs");
            var infraImplClass = $@"using System;
using System.Threading.Tasks;
using {projectName}.Application.Interfaces;

namespace {projectName}.Infrastructure
{{
    public class {validActionName}Service : I{validActionName}Service
    {{
        public Task<Guid> Create{validActionName}Async(string name)
        {{
            // TODO: Implement logic
            return Task.FromResult(Guid.NewGuid());
        }}
    }}
}}";
            File.WriteAllText(infraImplPath, infraImplClass);
        }

        private void RegisterServiceInDependencyInjection(string validActionName, string projectName, List<string> diLines, ref int addInfraIndex, string diPath)
        {
            var registration = $"builder.Services.AddScoped<I{validActionName}Service, {validActionName}Service>();";
            if (addInfraIndex != -1 && !diLines.Any(l => l.Contains(registration)))
            {
                diLines.Insert(addInfraIndex + 1, "        " + registration);
                addInfraIndex++;
            }

            // Write back DependencyInjection.cs if changed
            if (File.Exists(diPath))
            {
                File.WriteAllText(diPath, string.Join("\n", diLines));
            }

            // Ensure correct usings in DependencyInjection.cs
            if (File.Exists(diPath))
            {
                var diTextCurrent = File.ReadAllText(diPath);
                var usingApp = $"using {projectName}.Application.Interfaces;";
                var usingInfra = $"using {projectName}.Infrastructure;";
                var updated = false;
                if (!diTextCurrent.Contains(usingApp)) {
                    diTextCurrent = usingApp + "\n" + diTextCurrent;
                    updated = true;
                }
                if (!diTextCurrent.Contains(usingInfra)) {
                    diTextCurrent = usingInfra + "\n" + diTextCurrent;
                    updated = true;
                }
                if (updated) File.WriteAllText(diPath, diTextCurrent);
            }
        }

        public void CreateGenericControllersAndCommands(List<HolderAction> actionHolders, string projectName)
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var diPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Infrastructure", "DependencyInjection.cs");
            var diText = File.Exists(diPath) ? File.ReadAllText(diPath) : string.Empty;
            var diLines = diText.Split('\n').ToList();
            int addInfraIndex = diLines.FindIndex(l => l.Contains("void AddInfrastructureServices"));
            if (addInfraIndex != -1)
            {
                // Find the opening brace of the method
                while (addInfraIndex < diLines.Count && !diLines[addInfraIndex].Contains("{")) addInfraIndex++;
                addInfraIndex++;
            }

            foreach (var actionHolder in actionHolders)
            {
                var validHolderName = string.Concat(actionHolder.Holder.Where(char.IsLetterOrDigit));
                var validActionName = string.Concat(actionHolder.Action.Where(char.IsLetterOrDigit));
                if (string.IsNullOrWhiteSpace(validActionName))
                    continue;
                if (string.IsNullOrWhiteSpace(validHolderName))
                    continue;
                validActionName = char.ToUpper(validActionName[0]) + validActionName.Substring(1);
                validHolderName = char.ToUpper(validHolderName[0]) + validHolderName.Substring(1);

                CreateController(validHolderName, validActionName, projectName, parentDir, diLines, ref addInfraIndex, diPath);
            }

            DeleteOldWebEndpoints(projectName);
            InsertAddControllersAfterAddWebServices(projectName);
            InsertMapControllersBeforeMapEndpoints(projectName);
        }

        public void AddEfMigration(string projectName, string migrationName)
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var solutionRoot = Path.Combine(parentDir ?? "", "compiled_solution", projectName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ef migrations add {migrationName} --project ./src/Infrastructure --startup-project ./src/Web",
                WorkingDirectory = solutionRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Error running ef migrations add: {error}");
                }
                _logger.LogInformation($"Migration added: {output}");
            }
        }

        public void ApplyEfMigrations(string projectName)
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var solutionRoot = Path.Combine(parentDir ?? "", "compiled_solution", projectName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ef database update --project ./src/Infrastructure --startup-project ./src/Web",
                WorkingDirectory = solutionRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Error running ef database update: {error}");
                }
                _logger.LogInformation($"Database updated: {output}");
            }
        }

        public void RemoveDevelopmentIfElse(string projectName)
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var programPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Web", "Program.cs");
            if (!File.Exists(programPath))
                return;

            var lines = File.ReadAllLines(programPath).ToList();
            var newLines = new List<string>();
            int i = 0;
            while (i < lines.Count)
            {
                if (lines[i].Contains("if") && lines[i].Contains("app.Environment.IsDevelopment()"))
                {
                    // Skip exactly 9 lines (the if block and its contents)
                    i += 9;
                }
                else
                {
                    newLines.Add(lines[i]);
                    i++;
                }
            }
            File.WriteAllLines(programPath, newLines);
        }

        public void DeleteOldWebEndpoints(string projectName)
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var endpointsPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Web", "Endpoints");
            if (Directory.Exists(endpointsPath))
            {
                Directory.Delete(endpointsPath, true); // true = recursive
            }
        }

        public void InsertAddControllersAfterAddWebServices(string projectName)
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var programPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Web", "Program.cs");
            if (!File.Exists(programPath))
                return;

            var lines = File.ReadAllLines(programPath).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("builder.AddWebServices();"))
                {
                    // Insert after this line
                    lines.Insert(i + 1, "builder.Services.AddControllers();");
                    break;
                }
            }
            File.WriteAllLines(programPath, lines);
        }

        public void InsertMapControllersBeforeMapEndpoints(string projectName)
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var programPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Web", "Program.cs");
            if (!File.Exists(programPath))
                return;

            var lines = File.ReadAllLines(programPath).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("app.MapEndpoints();"))
                {
                    // Insert before this line
                    lines.Insert(i, "app.MapControllers();");
                    break;
                }
            }
            File.WriteAllLines(programPath, lines);
        }
    }
}
