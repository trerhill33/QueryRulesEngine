using Microsoft.AspNetCore.Mvc;

namespace QueryRulesEngine.Controllers
{

    [ApiController]
    [Route("api/approvalhierarchymanager/v{version:apiVersion}/[controller]")]
    public abstract class BaseVersionedController<T> : ControllerBase
    {
    }
}
