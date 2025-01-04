using DCPLInterpreterV2.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DCPLInterpreterV2.Controllers;

[ApiController]
[Route("[controller]")]
public class GeneratedSchemaController : ControllerBase
{
    private readonly IEntityService _entityService;

    public GeneratedSchemaController(IEntityService entityService)
    {
        _entityService = entityService;
    }

    [HttpPost("register")]
    public IActionResult registerAction([FromBody] Guid guid)
    {
        _entityService.UpdateEntityHolder(guid, "member");
        return Ok();
    }

    [HttpPost("request")]
    public IActionResult requestAction([FromBody] Guid guid)
    {
        _entityService.UpdateEntityHolder(guid, "");
        return Ok();
    }

}
