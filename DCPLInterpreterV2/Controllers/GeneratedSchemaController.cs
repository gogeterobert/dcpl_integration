using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
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
        var entityHolder = _entityService.GetEntityHolder(guid);
        var actionHoldersArray = new string[] { "person" };
        if (!actionHoldersArray.Contains(entityHolder))
            return Ok();
        
        _entityService.UpdateEntityHolder(guid, "member");
        return Ok();
    }

    [HttpPost("request")]
    public IActionResult requestAction([FromBody] Guid guid)
    {
        var entityHolder = _entityService.GetEntityHolder(guid);
        var actionHoldersArray = new string[] { "member" };
        if (!actionHoldersArray.Contains(entityHolder))
            return Ok();
        
        _entityService.UpdateEntityHolder(guid, "");
        return Ok();
    }

    [HttpPost("person")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public IActionResult personAction()
    {
        var entity = new Entity { Guid = Guid.NewGuid(), Holder = "person" };
        _entityService.Add(entity);
        
        return Ok(entity.Guid);
    }

    [HttpPost("member")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public IActionResult memberAction()
    {
        var entity = new Entity { Guid = Guid.NewGuid(), Holder = "member" };
        _entityService.Add(entity);
        
        return Ok(entity.Guid);
    }

}
