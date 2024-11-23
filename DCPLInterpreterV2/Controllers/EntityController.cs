using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Models;
using DCPLInterpreterV2.Services;
using Microsoft.AspNetCore.Mvc;

namespace DCPLInterpreterV2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EntityController : ControllerBase
    {
        private readonly IEntityService _entityService;

        public EntityController(IEntityService entityService)
        {
            _entityService = entityService;
        }

        [HttpPost]
        public Guid Create([FromBody] string holder)
        {
            return _entityService.Create(holder);
        }

        [HttpGet]
        public List<Entity> List()
        {
            return _entityService.List();
        }
    }
}
