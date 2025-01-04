using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DCPLInterpreterV2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SchemaController : ControllerBase
    {
        private readonly ILogger<SchemaController> _logger;
        private readonly ISchemaService _schemaService;
        private static readonly HttpClient _httpClient = new HttpClient();

        public SchemaController(ILogger<SchemaController> logger, ISchemaService schemaService)
        {
            _logger = logger;
            _schemaService = schemaService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] List<IDirective> directives)
        {
            if (directives == null)
            {
                return BadRequest(new { Errors = new List<string> { "Invalid JSON format. Expected an array of directives." } });
            }

            string schemaUrl = "https://raw.githubusercontent.com/gsileno/DCPLschema/main/DPCLschema.json";
            string schemaJson;

            try
            {
                schemaJson = await _httpClient.GetStringAsync(schemaUrl);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Error fetching schema from URL");
                return StatusCode(500, "Error fetching schema");
            }

            _schemaService.AddSchema(directives);

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

        [HttpPost("generate")]
        public void Generate()
        {
            var holders = _schemaService.GetHolders();
            var actions = _schemaService.GetActions();

            var sb = new StringBuilder();
            sb.AppendLine("using DCPLInterpreterV2.Interfaces;");
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine();
            sb.AppendLine("namespace DCPLInterpreterV2.Controllers;");
            sb.AppendLine();
            sb.AppendLine("[ApiController]");
            sb.AppendLine("[Route(\"[controller]\")]");
            sb.AppendLine("public class GeneratedSchemaController : ControllerBase");
            sb.AppendLine("{");
            sb.AppendLine("    private readonly IEntityService _entityService;");
            sb.AppendLine();
            sb.AppendLine("    public GeneratedSchemaController(IEntityService entityService)");
            sb.AppendLine("    {");
            sb.AppendLine("        _entityService = entityService;");
            sb.AppendLine("    }");
            sb.AppendLine();

            foreach (var action in actions)
            {
                var consequence = _schemaService.GetActionConsequence(action);
                var actionName = action.TrimStart('#');
                sb.AppendLine($"    [HttpPost(\"{actionName}\")]");
                sb.AppendLine($"    public IActionResult {actionName}Action([FromBody] Guid guid)");
                sb.AppendLine("    {");
                sb.AppendLine($"        _entityService.UpdateEntityHolder(guid, \"{consequence.In}\");");
                sb.AppendLine("        return Ok();");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Controllers", "GeneratedSchemaController.cs");
            System.IO.File.WriteAllText(filePath, sb.ToString());
        }
    }
}
