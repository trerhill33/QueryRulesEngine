using Microsoft.AspNetCore.Mvc;
using QueryRulesEngine.Features.Rules.AddRuleToLevel;
using QueryRulesEngine.Features.Rules.EditRule;
using QueryRulesEngine.Features.Rules.RemoveRule;
using QueryRulesEngine.Persistence;
using Swashbuckle.AspNetCore.Annotations;

namespace QueryRulesEngine.Controllers;

[ApiController]
[Route("api/approvalhierarchymanager/v{version:apiVersion}/hierarchies/{hierarchyId:int}/levels/{levelNumber:int}/rules")]
public class RulesController(
    IAddRuleToLevelService addService,
    IEditRuleService editService,
    IRemoveRuleService removeService) : BaseApiController<RulesController>
{
    private readonly IAddRuleToLevelService _addService = addService;
    private readonly IEditRuleService _editService = editService;
    private readonly IRemoveRuleService _removeService = removeService;

    [HttpPost]
    [SwaggerOperation(
        Summary = "Add rule to level",
        Description = "Creates a new rule for a specific level in the hierarchy",
        Tags = ["Rules"])]
    [ProducesResponseType(typeof(Result<AddRuleToLevelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AddRuleToLevelResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromRoute] int hierarchyId,
        [FromRoute] int levelNumber,
        [FromBody] AddRuleToLevelRequest request)
    {
        var result = await _addService.ExecuteAsync(
            request with
            {
                HierarchyId = hierarchyId,
                LevelNumber = levelNumber
            });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{ruleId}")]
    [SwaggerOperation(
        Summary = "Edit existing rule",
        Description = "Updates an existing rule's configuration",
        Tags = ["Rules"])]
    [ProducesResponseType(typeof(Result<EditRuleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<EditRuleResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Edit(
        [FromRoute] int hierarchyId,
        [FromRoute] int levelNumber,
        [FromRoute] string ruleId,
        [FromBody] EditRuleRequest request)
    {
        var result = await _editService.ExecuteAsync(
            request with
            {
                HierarchyId = hierarchyId,
                LevelNumber = levelNumber,
                RuleNumber = ruleId
            });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{ruleId}")]
    [SwaggerOperation(
        Summary = "Remove rule",
        Description = "Removes a rule from the hierarchy level",
        Tags = ["Rules"])]
    [ProducesResponseType(typeof(Result<RemoveRuleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RemoveRuleResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Remove(
        [FromRoute] int hierarchyId,
        [FromRoute] int levelNumber,
        [FromRoute] string ruleId)
    {
        var request = new RemoveRuleRequest(
            hierarchyId,
            levelNumber,
            ruleId);

        var result = await _removeService.ExecuteAsync(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
