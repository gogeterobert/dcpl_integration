using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using Microsoft.AspNetCore.Mvc;

namespace DCPLInterpreterV2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ActionController : ControllerBase
    {
        private readonly ILogger<ActionController> _logger;
        private readonly ISchemaService _schemaService;

        public ActionController(ILogger<ActionController> logger, ISchemaService schemaService)
        {
            _logger = logger;
            _schemaService = schemaService;
        }

        [HttpPost]
        public Schema Create([FromBody] List<Record> records)
        {
            var schema = new Schema { Records = records };
            _schemaService.AddSchema(schema);
            return schema;
        }
    }
}
