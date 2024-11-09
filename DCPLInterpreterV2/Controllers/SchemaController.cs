using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;

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
        public async Task<IActionResult> Create([FromBody] JToken body)
        {
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
            
            IList<string> validationErrors = new List<string>();

            if (!body.IsValid(schema, out validationErrors))
            {
                return BadRequest(new { Errors = validationErrors });
            }

            // Process the valid body
            return Ok();
        }
    }
}
