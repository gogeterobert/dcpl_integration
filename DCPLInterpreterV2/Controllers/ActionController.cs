using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using Microsoft.AspNetCore.Mvc;

namespace DCPLInterpreterV2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ActionController : ControllerBase
    {
        private readonly IActionService _actionService;

        public ActionController(IActionService actionService)
        {
            _actionService = actionService;
        }

        [HttpPost("act")]
        public void Act([FromBody] EntityAction holderAction)
        {
            var canAct = _actionService.Act(holderAction.Guid, holderAction.Action);

            if (canAct)
            {
                Response.StatusCode = StatusCodes.Status200OK;
            }
            else
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
            }
        }
    }
}
