using Microsoft.AspNetCore.Mvc;
using QueryRulesEngine.Features.Hierarchies.CreateHierarchy;
using QueryRulesEngine.Persistence;
using Swashbuckle.AspNetCore.Annotations;

namespace QueryRulesEngine.Controllers;

[ApiController]
[Route("api/approvalhierarchymanager/v{version:apiVersion}/hierarchies")]
public class HierarchiesController : BaseApiController<HierarchiesController>
{
    private readonly ICreateHierarchyService _createService;
    private readonly IGetHierarchyDetailsService _getDetailsService;

    public HierarchiesController(
        ICreateHierarchyService createService,
        IGetHierarchyDetailsService getDetailsService)
    {
        _createService = createService;
        _getDetailsService = getDetailsService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create new hierarchy",
        Description = "Creates a new approval hierarchy with default levels",
        Tags = ["Hierarchies"])]
    [ProducesResponseType(typeof(Result<CreateHierarchyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CreateHierarchyResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<CreateHierarchyResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateHierarchyRequest request)
    {
        var result = await _createService.ExecuteAsync(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}/details")]
    [SwaggerOperation(
        Summary = "Get hierarchy details",
        Description = "Returns detailed hierarchy information including levels and metadata configuration",
        Tags = ["Hierarchies"])]
    [ProducesResponseType(typeof(Result<GetHierarchyDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<GetHierarchyDetailsResponse>), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Result<GetHierarchyDetailsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDetails([FromRoute] int id)
    {
        var result = await _getDetailsService.ExecuteAsync(new GetHierarchyDetailsRequest { HierarchyId = id });

        if (!result.Succeeded)
            return BadRequest(result);

        return result.Data is null ? NoContent() : Ok(result);
    }
}