using DCPLInterpreterV2.Models;
using Microsoft.AspNetCore.Mvc;

namespace DCPLInterpreterV2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ActionController : ControllerBase
    {
        [HttpPost]
        public Schema Act([FromBody] Models.Action action)
        {
            var schema = new Schema { Records = records };
            _schemas.Add(schema);
            return schema;
        }
    }
}
