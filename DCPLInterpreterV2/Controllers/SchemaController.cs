using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

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

            JSchema schema;
            try
            {
                schema = JSchema.Parse(schemaJson);
            }
            catch (JSchemaException e)
            {
                _logger.LogError(e, "Error parsing schema");
                return StatusCode(500, "Error parsing schema");
            }

            // Validate each directive against the schema
            IList<string> validationErrors = new List<string>();
            foreach (var directive in directives)
            {
                var directiveToken = JToken.FromObject(directive);
                if (!directiveToken.IsValid(schema, out IList<string> errors))
                {
                    validationErrors = validationErrors.Concat(errors).ToList();
                }
            }

            if (validationErrors.Any())
            {
                return BadRequest(new { Errors = validationErrors });
            }

            // Process the valid directives
            return Ok(directives);
        }
    }
}
