using Microsoft.AspNetCore.Mvc;
using QueryRulesEngine.Features.MetadataKeys.MetaDataGridBuilder;
using QueryRulesEngine.Features.MetadataKeys.RemoveApproverMetadataKey;
using QueryRulesEngine.Features.MetadataKeys.SyncApproverMetadataKeys;
using QueryRulesEngine.Persistence;
using Swashbuckle.AspNetCore.Annotations;
namespace QueryRulesEngine.Controllers
{
    [ApiController]
    [Route("api/approvalhierarchymanager/v{version:apiVersion}/hierarchies/{hierarchyId:int}/metadata-keys")]
    public class MetadataKeysController(
        IAddApproverMetadataKeyService addService,
        IRemoveApproverMetadataKeyService removeService,
        ISyncApproverMetadataKeysService syncService,
        IMetadataKeyQueryService queryService) : BaseApiController<MetadataKeysController>
    {
        private readonly IAddApproverMetadataKeyService _addService = addService;
        private readonly IRemoveApproverMetadataKeyService _removeService = removeService;
        private readonly ISyncApproverMetadataKeysService _syncService = syncService;
        private readonly IMetadataKeyQueryService _queryService = queryService;

        [HttpPost]
        [SwaggerOperation(
            Summary = "Add metadata key",
            Description = "Adds a new metadata key to the hierarchy",
            Tags = ["MetadataKeys"])]
        [ProducesResponseType(typeof(Result<AddApproverMetadataKeyResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<AddApproverMetadataKeyResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromRoute] int hierarchyId,
            [FromBody] AddApproverMetadataKeyRequest request)
        {
            var result = await _addService.ExecuteAsync(
                request with { HierarchyId = hierarchyId });

            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{keyName}")]
        [SwaggerOperation(
            Summary = "Remove metadata key",
            Description = "Removes a metadata key and all associated values from the hierarchy",
            Tags = ["MetadataKeys"])]
        [ProducesResponseType(typeof(Result<RemoveApproverMetadataKeyResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RemoveApproverMetadataKeyResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Remove(
            [FromRoute] int hierarchyId,
            [FromRoute] string keyName)
        {
            var request = new RemoveApproverMetadataKeyRequest(
                hierarchyId,
                keyName);

            var result = await _removeService.ExecuteAsync(request);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpPost("sync")]
        [SwaggerOperation(
            Summary = "Sync metadata keys",
            Description = "Synchronizes metadata keys across all approvers in the hierarchy",
            Tags = ["MetadataKeys"])]
        [ProducesResponseType(typeof(Result<SyncApproverMetadataKeysResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<SyncApproverMetadataKeysResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Sync(
            [FromRoute] int hierarchyId)
        {
            var request = new SyncApproverMetadataKeysRequest(hierarchyId);
            var result = await _syncService.ExecuteAsync(request);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{keyName}/grid")]
        [SwaggerOperation(
            Summary = "Get metadata grid",
            Description = "Returns a grid view of metadata values for a specific key across all approvers",
            Tags = ["MetadataKeys"])]
        [ProducesResponseType(typeof(Result<MetadataGridResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<MetadataGridResponse>), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Result<MetadataGridResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetGrid(
            [FromRoute] int hierarchyId,
            [FromRoute] string keyName)
        {
            var result = await _queryService.GetMetadataValuesForKeyAsync(
                $"ApproverMetadataKey.{keyName}");

            if (!result.Succeeded)
                return BadRequest(result);

            return result.Data.Data.Any() ? Ok(result) : NoContent();
        }
    }
}
