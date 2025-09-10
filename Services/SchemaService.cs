using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using DCPLInterpreterV2.Infrastructure;
using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;

namespace DCPLInterpreterV2.Services
{    
    public class SchemaService : ISchemaService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        private readonly SchemaDbContext _context;
        private readonly ILogger<SchemaService> _logger;

        public SchemaService(SchemaDbContext context, ILogger<SchemaService> logger)
        {
            _context = context;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _jsonOptions.Converters.Add(new FrameJsonConverter());
            _jsonOptions.Converters.Add(new EventJsonConverter());
        }

        public void AddAndReplaceSchema(List<Frame> schema)
        {
            var directiveEntities = schema.Select(directive => new DirectiveEntity
            {
                DirectiveType = directive.GetType().ToString(),
                JsonData = JsonSerializer.Serialize(directive, _jsonOptions)
            }).ToList();

            _context.Directives.RemoveRange(_context.Directives);
            _context.Directives.AddRange(directiveEntities);
            _context.SaveChanges();
        }

        public List<ActionHolder> GetActionHolders()
        {
            var directives = _context.Directives.ToList();
            var powerFrames = directives.Select(directiveEntity =>
                JsonSerializer.Deserialize<PowerFrame>(directiveEntity.JsonData, _jsonOptions)
            ).Where(d => !string.IsNullOrEmpty(d?.Holder)).ToList();

            var actionHolders = powerFrames.SelectMany(powerFrame => new List<ActionHolder>
                {
                    new ActionHolder {
                        Holder = powerFrame?.Holder ?? string.Empty,
                        Action = powerFrame?.Action ?? string.Empty,
                        Consequence = powerFrame?.Consequence
                    }
                }).Where(d => !string.IsNullOrEmpty(d.Action)).ToList();

            // Process direct DutyFrames
            var dutyFrames = directives.Select(directiveEntity =>
                JsonSerializer.Deserialize<DutyFrame>(directiveEntity.JsonData, _jsonOptions)
            ).Where(d => !string.IsNullOrEmpty(d?.Counterparty)).ToList();

            actionHolders.AddRange(dutyFrames.SelectMany(dutyFrame => new List<ActionHolder>
                {
                    new ActionHolder {
                        Holder = dutyFrame?.Holder ?? string.Empty,
                        Action = dutyFrame?.Action ?? string.Empty
                    },
                    new ActionHolder {
                        Holder = dutyFrame?.Counterparty ?? string.Empty,
                        ViolationExpression = dutyFrame?.Violation?.Expression,
                        ViolationEvent = dutyFrame?.Violation?.Event
                    }
                })
                .Where(a => !string.IsNullOrEmpty(a.Action) || !string.IsNullOrEmpty(a.ViolationExpression))
                .ToList());

            var transformationFrames = directives.Select(directiveEntity =>
                JsonSerializer.Deserialize<TransformationalFrame>(directiveEntity.JsonData, _jsonOptions)
            ).Where(d => d?.Conclusion != null).ToList();
            actionHolders.AddRange(transformationFrames
               .SelectMany(tf => new List<ActionHolder>
               {
                    new ActionHolder {
                        Holder = (tf?.Conclusion as PowerFrame)?.Holder ?? string.Empty,
                        Action = (tf?.Conclusion as PowerFrame)?.Action ?? string.Empty,
                        Condition = tf?.Condition ?? string.Empty
                    },
                    new ActionHolder {
                        Holder = (tf?.Conclusion as DutyFrame)?.Counterparty ?? string.Empty,
                        Condition = tf?.Condition ?? string.Empty,
                        ViolationExpression = (tf?.Conclusion as DutyFrame)?.Violation?.Expression,
                        ViolationEvent = (tf?.Conclusion as DutyFrame)?.Violation?.Event
                    }
               })
               .Where(a => !string.IsNullOrEmpty(a.Action) || !string.IsNullOrEmpty(a.ViolationExpression) || !string.IsNullOrEmpty(a.ViolationEvent))
               .ToList());

            // Process CompoundFrames
            var compoundFrames = directives.Select(directiveEntity =>
                JsonSerializer.Deserialize<CompoundFrame>(directiveEntity.JsonData, _jsonOptions)
            ).Where(d => !string.IsNullOrEmpty(d?.Compound) && d?.Content?.Any() == true).ToList();

            actionHolders.AddRange(compoundFrames
                .SelectMany(cf => cf?.Content?.SelectMany(contentFrame => new List<ActionHolder>
                {
                    new ActionHolder {
                        Holder = (contentFrame as PowerFrame)?.Holder ?? string.Empty,
                        Action = (contentFrame as PowerFrame)?.Action ?? string.Empty,
                        Condition = cf.Compound, // Use compound name as condition
                        Consequence = (contentFrame as PowerFrame)?.Consequence
                    },
                    new ActionHolder {
                        Holder = (contentFrame as DutyFrame)?.Holder ?? string.Empty,
                        Action = (contentFrame as DutyFrame)?.Action ?? string.Empty,
                        Condition = cf.Compound, // Use compound name as condition
                        ViolationExpression = (contentFrame as DutyFrame)?.Violation?.Expression,
                        ViolationEvent = (contentFrame as DutyFrame)?.Violation?.Event
                    },
                    new ActionHolder {
                        Holder = (contentFrame as DutyFrame)?.Counterparty ?? string.Empty,
                        Condition = cf.Compound, // Use compound name as condition
                        ViolationExpression = (contentFrame as DutyFrame)?.Violation?.Expression,
                        ViolationEvent = (contentFrame as DutyFrame)?.Violation?.Event
                    }
                }) ?? Enumerable.Empty<ActionHolder>())
                .Where(a => !string.IsNullOrEmpty(a.Action) || !string.IsNullOrEmpty(a.ViolationExpression) || !string.IsNullOrEmpty(a.ViolationEvent))
                .ToList());

            return actionHolders;
        }

        public List<string> GetViolationExpressions()
        {
            return GetActionHolders()
                .Where(actionHolder => !string.IsNullOrEmpty(actionHolder.ViolationExpression))
                .Select(actionHolder => actionHolder.ViolationExpression!)
                .Distinct()
                .ToList();
        }

        public List<string> GetHolders()
        {
            return GetActionHolders().Select(holderAction => holderAction.Holder).Distinct().ToList();
        }

        public List<string> GetActions()
        {
            return GetActionHolders().Select(holderAction => holderAction.Action).Distinct().ToList();
        }

        public List<string> ParseAllEntitiesFromSchema()
        {
            var schemaEntities = new List<Entity>();
            var directives = _context.Directives.ToList();
            var powerFrames = directives.Select(directiveEntity =>
                    JsonSerializer.Deserialize<PowerFrame>(directiveEntity.JsonData, _jsonOptions)
            ).ToList();

            schemaEntities.AddRange(GetHolders().Select(holder => new Entity { Holder = holder }));
            schemaEntities.AddRange(powerFrames
                .Select(powerFrame => new Entity
                {
                    Holder = (powerFrame?.Consequence as PlusProductEvent)?.Plus ?? string.Empty
                })
                .Where(e => !string.IsNullOrEmpty(e.Holder)).ToList());
            var transformationalFrames = directives.Select(directiveEntity =>
                    JsonSerializer.Deserialize<TransformationalFrame>(directiveEntity.JsonData, _jsonOptions)
            ).Where(t => t?.Conclusion != null).ToList();
            schemaEntities.AddRange(transformationalFrames.SelectMany(transformationFrame =>
                new List<Entity>
                {
                    new Entity
                    {
                        Holder = (transformationFrame?.Conclusion as DutyFrame)?.Holder ?? string.Empty
                    },
                    new Entity
                    {
                        Holder = ((transformationFrame?.Conclusion as PowerFrame)?.Consequence as PlusProductEvent)?.Plus ?? string.Empty
                    }
                })
                .Where(e => !string.IsNullOrEmpty(e.Holder))
                .Distinct(new EntityEqualityComparer()).ToList());

            // Process CompoundFrames for entities
            var compoundFrames = directives.Select(directiveEntity =>
                JsonSerializer.Deserialize<CompoundFrame>(directiveEntity.JsonData, _jsonOptions)
            ).Where(d => !string.IsNullOrEmpty(d?.Compound) && d?.Content?.Any() == true).ToList();

            schemaEntities.AddRange(compoundFrames
                .SelectMany(cf => cf?.Content?.SelectMany(contentFrame => new List<Entity>
                {
                    new Entity { Holder = (contentFrame as PowerFrame)?.Holder ?? string.Empty },
                    new Entity { Holder = (contentFrame as DutyFrame)?.Holder ?? string.Empty },
                    new Entity { Holder = (contentFrame as DutyFrame)?.Counterparty ?? string.Empty },
                    new Entity { Holder = ((contentFrame as PowerFrame)?.Consequence as PlusProductEvent)?.Plus ?? string.Empty },
                    new Entity { Holder = ((contentFrame as PowerFrame)?.Consequence as NamingEvent)?.In ?? string.Empty },
                    new Entity { Holder = cf.Compound } // Add the compound name itself as an entity
                }) ?? Enumerable.Empty<Entity>())
                .Where(e => !string.IsNullOrEmpty(e.Holder))
                .Distinct(new EntityEqualityComparer()).ToList());

            return schemaEntities.Select(e => e.Holder).Distinct().ToList();
        }

        public string GenerateFromTemplate(string projectName)
        {
            if (projectName == null || string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Invalid project details. 'Name' is required.");
            }

            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var projectPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName);
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
            AddDbSetToDbContext(validEntityName, projectName, parentDir);
            AddEntityToApplicationDbContextInterface(validEntityName, projectName, parentDir);
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
                    var lines = dbContextText.Split('\n').ToList();
                    int insertIndex = lines.FindLastIndex(l => l.Contains("DbSet<"));
                    if (insertIndex == -1)
                    {
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

        private void CreateControllerAndCommands(string validHolderName, List<ActionHolder> actionHolders, string projectName, string? parentDir, List<string> diLines, ref int addInfraIndex, string diPath)
        {
            var controllersPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Web", "Controllers");
            Directory.CreateDirectory(controllersPath);
            var controllerFilePath = Path.Combine(controllersPath, $"{validHolderName}Controller.cs");
            var controllerClass = $@"using MediatR;
using Microsoft.AspNetCore.Mvc;
using {projectName}.Application.{validHolderName}.Commands;

namespace {projectName}.Web.Controllers
{{
        [ApiController]
    [Route(""api/{validHolderName}"")]
    public class {validHolderName}Controller : ControllerBase
    {{
        private readonly IMediator _mediator;
        public {validHolderName}Controller(IMediator mediator) => _mediator = mediator;

";
            foreach (var action in actionHolders)
            {
                string? validActionName = GetActionNameForEndpoint(action);
                if (string.IsNullOrWhiteSpace(validActionName))
                    continue;

                controllerClass += $@"
        [HttpPost(""{validActionName}"")]
        public async Task<IActionResult> Create{validActionName}([FromBody] Create{validActionName}Command command)
        {{
            var result = await _mediator.Send(command);
            return Ok(result);
        }}
    ";
            }

            controllerClass += $@"
    }}
}}";

            File.WriteAllText(controllerFilePath, controllerClass);

            foreach (var action in actionHolders)
            {
                string? validActionName = GetActionNameForEndpoint(action);
                if (string.IsNullOrWhiteSpace(validActionName))
                {
                    continue;
                }

                var commandCode = GetActionEntityIfGate(validHolderName, action);
                commandCode += GetActionCodeBasedOnFrameEventType(action);

                CreateMediatRCommandAndHandler(validActionName, validHolderName, projectName, commandCode, false, parentDir);
                CreateApplicationInterface(validActionName, projectName, parentDir);
                CreateInfrastructureImplementation(validActionName, projectName, parentDir);
                RegisterServiceInDependencyInjection(validActionName, projectName, diLines, ref addInfraIndex, diPath);
            }
        }

        private static string? GetValidNaming(string naming)
        {
            var validActionName = string.Concat(naming.Where(char.IsLetterOrDigit));
            if (string.IsNullOrWhiteSpace(validActionName))
                return null;
            validActionName = char.ToUpper(validActionName[0]) + validActionName.Substring(1);
            return validActionName;
        }

        private static string? GetActionNameForEndpoint(ActionHolder actionHolder)
        {
            // Priority: Action > ViolationEvent > ViolationExpression
            if (!string.IsNullOrWhiteSpace(actionHolder.Action))
            {
                return GetValidNaming(actionHolder.Action);
            }
            
            if (!string.IsNullOrWhiteSpace(actionHolder.ViolationEvent))
            {
                return GetValidNaming($"Violation{actionHolder.ViolationEvent}");
            }
            
            if (!string.IsNullOrWhiteSpace(actionHolder.ViolationExpression))
            {
                // Generate a simple name for violation expressions
                return "ViolationExpression";
            }
            
            return null;
        }

        private string GetActionCodeBasedOnFrameEventType(ActionHolder actionHolder)
        {
            if (!string.IsNullOrEmpty(actionHolder.ViolationExpression))
            {
                return $@"
                var violationEvent = new Application.Common.Events.ViolationDetectedEvent(
                    ""Violation action '{actionHolder.Action}' was triggered"",
                    ""{actionHolder.ViolationExpression?.Replace("\"", "\\\"")}"",
                    ""{actionHolder.Action}"");
                
                await _mediator.Publish(violationEvent, cancellationToken);
                return ""Violation action executed"";
                ";
            }

            if (!string.IsNullOrEmpty(actionHolder.ViolationEvent))
            {
                return $@"
                var violationEvent = new Application.Common.Events.ViolationDetectedEvent(
                    ""Violation event '{actionHolder.ViolationEvent}' was triggered"",
                    null,
                    ""{actionHolder.Action}"");
                
                await _mediator.Publish(violationEvent, cancellationToken);
                return ""Violation event executed"";
                ";
            }

            if ((actionHolder.Consequence as NamingEvent) != null && (actionHolder.Consequence as NamingEvent).Entity != null)
            {
                var validIn = GetValidNaming((actionHolder.Consequence as NamingEvent).In);
                return $@"
                var entity = new Domain.Entities.{validIn} {{ Name = request.Name }};
                _applicationDbContext.{validIn}s.Add(entity);
                await _applicationDbContext.SaveChangesAsync(cancellationToken);
                return entity.Name;
                ";
            }

            if ((actionHolder.Consequence as PlusProductEvent) != null)
            {
                var validHolderName = GetValidNaming((actionHolder.Consequence as PlusProductEvent).Plus);
                return $@"
                var entity = new Domain.Entities.{validHolderName} {{ Name = request.Name }};
                _applicationDbContext.{validHolderName}s.Add(entity);
                await _applicationDbContext.SaveChangesAsync(cancellationToken);
                return entity.Name;
                ";
            }

            return "return \"\";";
        }

        private string GetActionEntityIfGate(string validHolderName, ActionHolder actionHolder)
        {
            var extraGuard = GetExtraGuardBasedOnActionCondition(actionHolder);
            return $@"
            if (!await _applicationDbContext.{validHolderName}s.AnyAsync(x => x.Name == request.Name){extraGuard})
                throw new NotFoundException(nameof({validHolderName}), request.Name);
            ";
        }

        private object GetExtraGuardBasedOnActionCondition(ActionHolder actionHolder)
        {
            if (string.IsNullOrEmpty(actionHolder.Condition))
                return string.Empty;

            var validObjectName = GetValidNaming(actionHolder.Condition);

            return $@" && !await _applicationDbContext.{validObjectName}s.AnyAsync()";
        }

        private void AddEntityToApplicationDbContextInterface(string validHolderName, string projectName, string? parentDir)
        {
            var dbContextPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", "Common", "Interfaces", "IApplicationDbContext.cs");
            var dbContextClass = File.ReadAllText(dbContextPath);

            var entityName = $"{validHolderName}";
            var entityDbSet = $"public DbSet<{entityName}> {entityName}s {{ get; set; }}";

            if (!dbContextClass.Contains(entityDbSet))
            {
                var lines = File.ReadAllLines(dbContextPath).ToList();
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains("DbSet<TodoItem> TodoItems { get; }"))
                    {
                        lines.Insert(i + 1, $"DbSet<Domain.Entities.{entityName}> {entityName}s {{ get; }}");
                        break;
                    }
                }
                File.WriteAllLines(dbContextPath, lines);
            }
        }

        private void CreateMediatRCommandAndHandler(string validActionName, string validHolderName, string projectName, string commandCode, bool skipService, string? parentDir)
        {
            var commandsPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", validActionName, "Commands");
            Directory.CreateDirectory(commandsPath);
            var commandFilePath = Path.Combine(commandsPath, $"Create{validActionName}Command.cs");
            var handlerFilePath = Path.Combine(commandsPath, $"Create{validActionName}CommandHandler.cs");

            var commandClass = $@"using MediatR;

namespace {projectName}.Application.{validHolderName}.Commands
{{
    public record Create{validActionName}Command(string Name) : IRequest<string>;
}}";
            File.WriteAllText(commandFilePath, commandClass);

            var handlerClass = $@"using MediatR;
using {projectName}.Application.Interfaces;
using {projectName}.Application.Common.Interfaces;

namespace {projectName}.Application.{validHolderName}.Commands
{{
    public class Create{validActionName}CommandHandler : IRequestHandler<Create{validActionName}Command, string>
    {{
        "
        + (skipService ? "" : $"private readonly I{validActionName}Service _service;") +
        @$"
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IMediator _mediator;

        public Create{validActionName}CommandHandler(
        "
        + (skipService ? "" : $"I{validActionName}Service service, ") +
        @$"
        IApplicationDbContext applicationDbContext,
        IMediator mediator)
        {{
        "
        + (skipService ? "" : $"_service = service ?? throw new ArgumentNullException(nameof(service));") +
        @$"
            _applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }}

        public async Task<string> Handle(Create{validActionName}Command request, CancellationToken cancellationToken)
        {{
            {commandCode}
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
        Task<string> Create{validActionName}Async(string name);
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
        public Task<string> Create{validActionName}Async(string name)
        {{
            return Task.FromResult(Guid.NewGuid().ToString());
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

            if (File.Exists(diPath))
            {
                File.WriteAllText(diPath, string.Join("\n", diLines));
            }

            if (File.Exists(diPath))
            {
                var diTextCurrent = File.ReadAllText(diPath);
                var usingApp = $"using {projectName}.Application.Interfaces;";
                var usingInfra = $"using {projectName}.Infrastructure;";
                var updated = false;
                if (!diTextCurrent.Contains(usingApp))
                {
                    diTextCurrent = usingApp + "\n" + diTextCurrent;
                    updated = true;
                }
                if (!diTextCurrent.Contains(usingInfra))
                {
                    diTextCurrent = usingInfra + "\n" + diTextCurrent;
                    updated = true;
                }
                if (updated) File.WriteAllText(diPath, diTextCurrent);
            }
        }

        public void CreateGenericControllersAndCommands(List<ActionHolder> actionHolders, string projectName)
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var diPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Infrastructure", "DependencyInjection.cs");
            var diText = File.Exists(diPath) ? File.ReadAllText(diPath) : string.Empty;
            var diLines = diText.Split('\n').ToList();
            int addInfraIndex = diLines.FindIndex(l => l.Contains("void AddInfrastructureServices"));
            if (addInfraIndex != -1)
            {
                while (addInfraIndex < diLines.Count && !diLines[addInfraIndex].Contains("{")) addInfraIndex++;
                addInfraIndex++;
            }

            var groupedByHolder = actionHolders
                .Where(ah => !string.IsNullOrWhiteSpace(ah.Holder) && 
                           (!string.IsNullOrWhiteSpace(ah.Action) || 
                            !string.IsNullOrWhiteSpace(ah.ViolationExpression) || 
                            !string.IsNullOrWhiteSpace(ah.ViolationEvent)))
                .GroupBy(ah => ah.Holder)
                .ToList();

            foreach (var holderGroup in groupedByHolder)
            {
                var validHolderName = string.Concat(holderGroup.Key.Where(char.IsLetterOrDigit));
                if (string.IsNullOrWhiteSpace(validHolderName))
                    continue;
                validHolderName = char.ToUpper(validHolderName[0]) + validHolderName.Substring(1);

                var actions = holderGroup
                    .Where(ah => !string.IsNullOrWhiteSpace(ah.Action) || 
                               !string.IsNullOrWhiteSpace(ah.ViolationExpression) || 
                               !string.IsNullOrWhiteSpace(ah.ViolationEvent))
                    .Distinct()
                    .ToList();

                CreateControllerAndCommands(validHolderName, actions, projectName, parentDir, diLines, ref addInfraIndex, diPath);
                CreateEntityCreationController(validHolderName, projectName, parentDir, diLines, ref addInfraIndex, diPath);
            }

            DeleteOldWebEndpoints(projectName);
            InsertAddControllersAfterAddWebServices(projectName);
            InsertMapControllersBeforeMapEndpoints(projectName);
        }

        private void CreateEntityCreationController(string validHolderName, string projectName, string? parentDir, List<string> diLines, ref int addInfraIndex, string diPath)
        {
            var controllersPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Web", "Controllers");
            Directory.CreateDirectory(controllersPath);
            var controllerFilePath = Path.Combine(controllersPath, $"{validHolderName}CreationController.cs");
            var controllerClass = $@"using MediatR;
using Microsoft.AspNetCore.Mvc;
using {projectName}.Application.{validHolderName}.Commands;
namespace {projectName}.Web.Controllers
{{

    [ApiController]
    [Route(""api/{validHolderName}/create"")]
    public class {validHolderName}CreationController : ControllerBase
    {{
        private readonly IMediator _mediator;
        public {validHolderName}CreationController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEntity{validHolderName}Command command)
        {{
            var result = await _mediator.Send(command);
            return Ok(result);
        }}
    }}
}}";

            File.WriteAllText(controllerFilePath, controllerClass);
            var commandCode = @$"
                var entity = new Domain.Entities.{validHolderName} {{ Name = request.Name }};
                _applicationDbContext.{validHolderName}s.Add(entity);
                await _applicationDbContext.SaveChangesAsync(cancellationToken);
                return entity.Name;
            ";
            CreateMediatRCommandAndHandler("Entity" + validHolderName, validHolderName, projectName, commandCode, true, parentDir);
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

        public void AddMigrationLine(string projectName)
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            var programPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Web", "Program.cs");
            if (!File.Exists(programPath))
                return;

            var lines = File.ReadAllLines(programPath).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("app.UseStaticFiles();"))
                {
                    lines.Insert(i, "await app.InitialiseDatabaseAsync();");
                    break;
                }
            }
            File.WriteAllLines(programPath, lines);
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
                    lines.Insert(i, "app.MapControllers();");
                    break;
                }
            }
            File.WriteAllLines(programPath, lines);
        }

        public void CreateViolationEvaluatorService(string projectName)
        {
            var violationExpressions = GetViolationExpressions();
            if (!violationExpressions.Any())
                return;

            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
            CreateViolationException(projectName, parentDir);
            CreateViolationEvent(projectName, parentDir);
            CreateViolationEventHandler(projectName, parentDir);
            CreateExpressionEvaluatorInterface(projectName, parentDir);
            CreateExpressionEvaluatorImplementation(projectName, parentDir, violationExpressions);
            CreateViolationEvaluatorImplementation(projectName, parentDir, violationExpressions);
            RegisterViolationEvaluatorService(projectName, parentDir);
        }

        private void CreateViolationException(string projectName, string? parentDir)
        {
            var exceptionsPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", "Common", "Exceptions");
            Directory.CreateDirectory(exceptionsPath);
            var exceptionFilePath = Path.Combine(exceptionsPath, "ViolationException.cs");

            var exceptionCode = $@"namespace {projectName}.Application.Common.Exceptions;

public class ViolationException : Exception
{{
    public ViolationException()
    {{
    }}

    public ViolationException(string message)
        : base(message)
    {{
    }}

    public ViolationException(string message, Exception innerException)
        : base(message, innerException)
    {{
    }}
}}";

            File.WriteAllText(exceptionFilePath, exceptionCode);
        }

        private void CreateViolationEvent(string projectName, string? parentDir)
        {
            var eventsPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", "Common", "Events");
            Directory.CreateDirectory(eventsPath);
            var eventFilePath = Path.Combine(eventsPath, "ViolationDetectedEvent.cs");

            var eventCode = $@"using MediatR;

namespace {projectName}.Application.Common.Events;

public class ViolationDetectedEvent : INotification
{{
    public string ViolationMessage {{ get; set; }}
    public string? ViolationExpression {{ get; set; }}
    public string? ViolationAction {{ get; set; }}
    public DateTime DetectedAt {{ get; set; }}

    public ViolationDetectedEvent(string violationMessage, string? violationExpression = null, string? violationAction = null)
    {{
        ViolationMessage = violationMessage;
        ViolationExpression = violationExpression;
        ViolationAction = violationAction;
        DetectedAt = DateTime.UtcNow;
    }}
}}";

            File.WriteAllText(eventFilePath, eventCode);
        }

        private void CreateViolationEventHandler(string projectName, string? parentDir)
        {
            var handlersPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", "Common", "EventHandlers");
            Directory.CreateDirectory(handlersPath);
            var handlerFilePath = Path.Combine(handlersPath, "ViolationDetectedEventHandler.cs");

            var handlerCode = $@"using MediatR;
using Microsoft.Extensions.Logging;
using {projectName}.Application.Common.Events;
using {projectName}.Application.Common.Exceptions;

namespace {projectName}.Application.Common.EventHandlers;

public class ViolationDetectedEventHandler : INotificationHandler<ViolationDetectedEvent>
{{
    private readonly ILogger<ViolationDetectedEventHandler> _logger;

    public ViolationDetectedEventHandler(ILogger<ViolationDetectedEventHandler> logger)
    {{
        _logger = logger;
    }}

    public Task Handle(ViolationDetectedEvent notification, CancellationToken cancellationToken)
    {{
        _logger.LogError(""Violation detected at {{DetectedAt}}: {{Message}}"", 
            notification.DetectedAt, notification.ViolationMessage);
        
        if (!string.IsNullOrEmpty(notification.ViolationExpression))
        {{
            _logger.LogError(""Violation Expression: {{Expression}}"", notification.ViolationExpression);
        }}
        
        if (!string.IsNullOrEmpty(notification.ViolationAction))
        {{
            _logger.LogError(""Violation Action: {{Action}}"", notification.ViolationAction);
        }}
        throw new ViolationException(notification.ViolationMessage);
    }}
}}";

            File.WriteAllText(handlerFilePath, handlerCode);
        }

        private void CreateExpressionEvaluatorInterface(string projectName, string? parentDir)
        {
            var interfacesPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", "Common", "Interfaces");
            Directory.CreateDirectory(interfacesPath);
            var interfaceFilePath = Path.Combine(interfacesPath, "IExpressionEvaluatorService.cs");

            var interfaceCode = $@"namespace {projectName}.Application.Common.Interfaces;

public class ViolationResult
{{
    public string Message {{ get; set; }}
    public string Expression {{ get; set; }}
    
    public ViolationResult(string message, string expression)
    {{
        Message = message;
        Expression = expression;
    }}
}}

public interface IExpressionEvaluatorService
{{
    Task<List<ViolationResult>> EvaluateViolationExpressionsAsync();
}}";

            File.WriteAllText(interfaceFilePath, interfaceCode);
        }

        private void CreateExpressionEvaluatorImplementation(string projectName, string? parentDir, List<string> violationExpressions)
        {
            var infraPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Infrastructure");
            Directory.CreateDirectory(infraPath);
            var serviceFilePath = Path.Combine(infraPath, "ExpressionEvaluatorService.cs");

            var evaluationMethods = string.Join(Environment.NewLine, violationExpressions.Select((expr, index) => 
                $@"    private bool EvaluateExpression{index + 1}()
    {{
        try
        {{
            return {expr};
        }}
        catch (Exception ex)
        {{
            _logger.LogError(ex, ""Error evaluating violation expression: {expr.Replace("\"", "\\\"")}"");
            return false;
        }}
    }}"));

            var evaluationCalls = string.Join(Environment.NewLine, violationExpressions.Select((expr, index) => 
                $@"        if (EvaluateExpression{index + 1}())
        {{
            violations.Add(new ViolationResult(""Violation detected: {expr.Replace("\"", "\\\"")}"", ""{expr.Replace("\"", "\\\"")}""));
        }}"));

            var serviceCode = $@"using Microsoft.Extensions.Logging;
using {projectName}.Application.Common.Interfaces;

namespace {projectName}.Infrastructure;

public class ExpressionEvaluatorService : IExpressionEvaluatorService
{{
    private readonly ILogger<ExpressionEvaluatorService> _logger;

    public ExpressionEvaluatorService(ILogger<ExpressionEvaluatorService> logger)
    {{
        _logger = logger;
    }}

    public async Task<List<ViolationResult>> EvaluateViolationExpressionsAsync()
    {{
        var violations = new List<ViolationResult>();
        
{evaluationCalls}
        
        await Task.CompletedTask;
        return violations;
    }}

{evaluationMethods}
}}";

            File.WriteAllText(serviceFilePath, serviceCode);
        }

        private void CreateViolationEvaluatorInterface(string projectName, string? parentDir)
        {
            var interfacesPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", "Common", "Interfaces");
            Directory.CreateDirectory(interfacesPath);
            var interfaceFilePath = Path.Combine(interfacesPath, "IViolationEvaluatorService.cs");

            var interfaceCode = $@"namespace {projectName}.Application.Common.Interfaces;

public interface IViolationEvaluatorService
{{
    Task CheckViolationsAsync();
}}";

            File.WriteAllText(interfaceFilePath, interfaceCode);
        }

        private void CreateViolationEvaluatorImplementation(string projectName, string? parentDir, List<string> violationExpressions)
        {
            var servicesPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", "Common", "Services");
            Directory.CreateDirectory(servicesPath);
            var serviceFilePath = Path.Combine(servicesPath, "ViolationEvaluatorService.cs");

            var serviceCode = $@"using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MediatR;
using {projectName}.Application.Common.Interfaces;
using {projectName}.Application.Common.Events;
using {projectName}.Application.Common.Exceptions;

namespace {projectName}.Application.Common.Services;

public class ViolationEvaluatorService : BackgroundService
{{
    private readonly ILogger<ViolationEvaluatorService> _logger;
    private readonly IExpressionEvaluatorService _expressionEvaluator;
    private readonly IMediator _mediator;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(1);

    public ViolationEvaluatorService(
        ILogger<ViolationEvaluatorService> logger,
        IExpressionEvaluatorService expressionEvaluator,
        IMediator mediator)
    {{
        _logger = logger;
        _expressionEvaluator = expressionEvaluator;
        _mediator = mediator;
    }}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {{
        _logger.LogInformation(""ViolationEvaluatorService started with 1-second interval"");
        
        while (!stoppingToken.IsCancellationRequested)
        {{
            try
            {{
                await CheckViolationsAsync();
                await Task.Delay(_checkInterval, stoppingToken);
            }}
            catch (ViolationException vex)
            {{
                _logger.LogError(vex, ""Violation detected during scheduled check"");
            }}
            catch (OperationCanceledException)
            {{
                break;
            }}
            catch (Exception ex)
            {{
                _logger.LogError(ex, ""Unexpected error during violation check"");
                await Task.Delay(_checkInterval, stoppingToken);
            }}
        }}
        
        _logger.LogInformation(""ViolationEvaluatorService stopped"");
    }}

    public async Task CheckViolationsAsync()
    {{
        var violations = await _expressionEvaluator.EvaluateViolationExpressionsAsync();
        
        foreach (var violation in violations)
        {{
            var violationEvent = new ViolationDetectedEvent(
                violation.Message, 
                violation.Expression, 
                null);
            
            await _mediator.Publish(violationEvent);
        }}
        
        await Task.CompletedTask;
    }}

    public override async Task StopAsync(CancellationToken cancellationToken)
    {{
        _logger.LogInformation(""ViolationEvaluatorService is stopping"");
        await base.StopAsync(cancellationToken);
    }}
}}";

            File.WriteAllText(serviceFilePath, serviceCode);
        }

        private void RegisterViolationEvaluatorService(string projectName, string? parentDir)
        {
            var appDiPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Application", "DependencyInjection.cs");
            var infraDiPath = Path.Combine(parentDir ?? "", "compiled_solution", projectName, "src", "Infrastructure", "DependencyInjection.cs");
            if (File.Exists(appDiPath))
            {
                var appDiText = File.ReadAllText(appDiPath);
                var hostedRegistration = $"builder.Services.AddHostedService<ViolationEvaluatorService>();";

                if (!appDiText.Contains(hostedRegistration))
                {
                    var lines = appDiText.Split('\n').ToList();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].Contains("builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());"))
                        {
                            lines.Insert(i, $"        {hostedRegistration}");
                            lines.Insert(i, "");
                            break;
                        }
                    }
                    File.WriteAllText(appDiPath, string.Join("\n", lines));
                }
                var appUsingStatement = $"using {projectName}.Application.Common.Services;";
                if (!appDiText.Contains(appUsingStatement))
                {
                    var lines = File.ReadAllLines(appDiPath).ToList();
                    var lastUsingIndex = -1;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith("using "))
                            lastUsingIndex = i;
                    }
                    if (lastUsingIndex != -1)
                    {
                        lines.Insert(lastUsingIndex + 1, appUsingStatement);
                        File.WriteAllLines(appDiPath, lines);
                    }
                }
            }
            if (File.Exists(infraDiPath))
            {
                var infraDiText = File.ReadAllText(infraDiPath);
                var infraRegistration = $"builder.Services.AddSingleton<IExpressionEvaluatorService, ExpressionEvaluatorService>();";
                
                if (!infraDiText.Contains(infraRegistration))
                {
                    var lines = infraDiText.Split('\n').ToList();
                    int addInfraIndex = lines.FindIndex(l => l.Contains("void AddInfrastructureServices"));
                    if (addInfraIndex != -1)
                    {
                        while (addInfraIndex < lines.Count && !lines[addInfraIndex].Contains("{")) addInfraIndex++;
                        addInfraIndex++;
                        lines.Insert(addInfraIndex, $"        {infraRegistration}");
                        lines.Insert(addInfraIndex, "");
                        File.WriteAllText(infraDiPath, string.Join("\n", lines));
                    }
                }
                var infraUsingStatements = new[]
                {
                    $"using {projectName}.Application.Common.Interfaces;",
                    $"using {projectName}.Infrastructure;"
                };

                var currentInfraDiText = File.ReadAllText(infraDiPath);
                var updated = false;
                foreach (var usingStatement in infraUsingStatements)
                {
                    if (!currentInfraDiText.Contains(usingStatement))
                    {
                        currentInfraDiText = usingStatement + "\n" + currentInfraDiText;
                        updated = true;
                    }
                }
                if (updated) File.WriteAllText(infraDiPath, currentInfraDiText);
            }
        }
    }
}
