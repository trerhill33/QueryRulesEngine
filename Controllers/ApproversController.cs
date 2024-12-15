using Microsoft.AspNetCore.Mvc;
using QueryRulesEngine.Features.Approvers.CreateApprovers;
using QueryRulesEngine.Features.Approvers.FindApprovers;
using QueryRulesEngine.Features.MetadataKeys.TaggedMetadataUpdate;
using QueryRulesEngine.Persistence;
using Swashbuckle.AspNetCore.Annotations;

namespace QueryRulesEngine.Controllers;

[ApiController]
[Route("api/approvalhierarchymanager/v{version:apiVersion}/hierarchies/{hierarchyId:int}/approvers")]
public class ApproversController(
    ICreateApproversService createService,
    IFindApproversService findService,
    ITaggedMetadataUpdateService updateService) : BaseApiController<ApproversController>
{
    private readonly ICreateApproversService _createService = createService;
    private readonly IFindApproversService _findService = findService;
    private readonly ITaggedMetadataUpdateService _updateService = updateService;

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create approvers",
        Description = "Adds new approvers to the hierarchy",
        Tags = ["Approvers"])]
    [ProducesResponseType(typeof(Result<CreateApproversResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CreateApproversResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromRoute] int hierarchyId,
        [FromBody] CreateApproversRequest request)
    {
        var result = await _createService.ExecuteAsync(
            request with { HierarchyId = hierarchyId });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Find approvers",
        Description = "Returns all approvers in the hierarchy",
        Tags = ["Approvers"])]
    [ProducesResponseType(typeof(Result<FindApproversResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<FindApproversResponse>), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Result<FindApproversResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Find(
        [FromRoute] int hierarchyId,
        [FromBody] FindApproversRequest request)
    {

        var result = await _findService.ExecuteAsync(
            request with { HierarchyId = hierarchyId });

        if (!result.Succeeded)
            return BadRequest(result);

        return result.Data?.PotentialApprovers?.Count > 0
            ? Ok(result)
            : NoContent();
    }

    [HttpPut("metadata")]
    [SwaggerOperation(
        Summary = "Update metadata values",
        Description = "Updates metadata values for approvers by tag",
        Tags = ["Approvers"])]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMetadata(
        [FromBody] TaggedMetadataUpdateRequest request)
    {
        var result = await _updateService.UpdateMetadataValueByTagAsync(request);

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
