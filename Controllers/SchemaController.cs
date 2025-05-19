using DCPLInterpreterV2.Infrastructure;
using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace DCPLInterpreterV2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SchemaController : ControllerBase
    {
        private readonly ISchemaService _schemaService;

        public SchemaController(ISchemaService schemaService)
        {
            _schemaService = schemaService;
        }

        [HttpPost("CreateAndReplace")]
        public async Task<IActionResult> Create([FromBody] List<PowerFrame> directives)
        {
            if (directives == null)
            {
                return BadRequest(new { Errors = new List<string> { "Invalid JSON format. Expected an array of directives." } });
            }

            _schemaService.AddAndReplaceSchema(directives);

            return Ok();
        }

        [HttpGet("holders")]
        public List<string> GetHolders()
        {
            return _schemaService.GetHolders();
        }

        [HttpGet("actions")]
        public List<string> GetActions()
        {
            return _schemaService.GetActions();
        }

        [HttpGet("entities")]
        public List<string> GetEntities()
        {
            return _schemaService.ParseAllEntitiesFromSchema();
        }

        [HttpPost("generate")]
        public void Generate([FromBody] NewProject newProject)
        {
            var projectPath = _schemaService.GenerateFromTemplate(newProject.Name);

            var entities = _schemaService.ParseAllEntitiesFromSchema();
            foreach (var entity in entities)
            {
                _schemaService.CreateNewEntityInGeneratedSolution(entity!, newProject.Name);
            }
        }
    }
}

// var actions = _schemaService.GetActions();

        // var sb = new StringBuilder();
        // sb.AppendLine("using DCPLInterpreterV2.Infrastructure;");
        // sb.AppendLine("using DCPLInterpreterV2.Interfaces;");
        // sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        // sb.AppendLine();
        // sb.AppendLine("namespace DCPLInterpreterV2.Controllers;");
        // sb.AppendLine();
        // sb.AppendLine("[ApiController]");
        // sb.AppendLine("[Route(\"[controller]\")]");
        // sb.AppendLine("public class GeneratedSchemaController : ControllerBase");
        // sb.AppendLine("{");
        // sb.AppendLine("    private readonly IEntityService _entityService;");
        // sb.AppendLine();
        // sb.AppendLine("    public GeneratedSchemaController(IEntityService entityService)");
        // sb.AppendLine("    {");
        // sb.AppendLine("        _entityService = entityService;");
        // sb.AppendLine("    }");
        // sb.AppendLine();

        // foreach (var action in actions)
        // {
        //     var consequence = _schemaService.GetActionConsequence(action);

        //     if (consequence is null)
        //     {
        //         continue;
        //     }

        //     var actionHolders = _actionService.GetActionsHolders(action);
        //     var arrayDeclaration = $"var actionHoldersArray = new string[] {{ {string.Join(", ", actionHolders.Select(s => $"\"{s}\""))} }};";
        //     var actionName = action.TrimStart('#');
        //     sb.AppendLine($"    [HttpPost(\"{actionName}\")]");
        //     sb.AppendLine($"    public IActionResult {actionName}Action([FromBody] Guid guid)");
        //     sb.AppendLine("    {");
        //     sb.AppendLine($"        var entityHolder = _entityService.GetEntityHolder(guid);");
        //     sb.AppendLine($"        {arrayDeclaration}");
        //     sb.AppendLine($"        if (!actionHoldersArray.Contains(entityHolder))");
        //     sb.AppendLine($"            return Ok();");
        //     sb.AppendLine($"        ");
        //     sb.AppendLine($"        _entityService.UpdateEntityHolder(guid, \"{consequence.In}\");");
        //     sb.AppendLine("        return Ok();");
        //     sb.AppendLine("    }");
        //     sb.AppendLine();
        // }

        // foreach (var holder in entities)
        // {
        //     sb.AppendLine($"    [HttpPost(\"{holder}\")]");
        //     sb.AppendLine($"    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]");
        //     sb.AppendLine($"    public IActionResult {holder}Action()");
        //     sb.AppendLine("    {");
        //     sb.AppendLine($"        var entity = new Entity {{ Id = Guid.NewGuid(), Holder = \"{holder}\" }};");
        //     sb.AppendLine("        _entityService.Add(entity);");
        //     sb.AppendLine("        ");
        //     sb.AppendLine("        return Ok(entity.Id);");
        //     sb.AppendLine("    }");
        //     sb.AppendLine();
        // }

        // sb.AppendLine("}");

        // var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Controllers", "GeneratedSchemaController.cs");
        // System.IO.File.WriteAllText(filePath, sb.ToString());