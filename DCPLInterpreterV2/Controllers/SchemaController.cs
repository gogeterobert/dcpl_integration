using DCPLInterpreterV2.Models;
using Microsoft.AspNetCore.Mvc;

namespace DCPLInterpreterV2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ActionController : ControllerBase
    {
        private readonly ILogger<ActionController> _logger;

        private static List<Schema> _schemas = new List<Schema>();

        public ActionController(ILogger<ActionController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public Schema Create([FromBody] List<Record> records)
        {
            var schema = new Schema { Records = records };
            _schemas.Add(schema);
            return schema;
        }
    }
}
