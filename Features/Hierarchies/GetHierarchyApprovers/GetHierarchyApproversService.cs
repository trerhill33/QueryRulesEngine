using FluentValidation;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers.Models;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Persistence;
using QueryRulesEngine.QueryEngine.Processors;
using System.Linq.Expressions;

namespace QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers;

public sealed class GetHierarchyApproversService(
    IReadOnlyRepositoryAsync<int> readOnlyRepository,
    IValidator<GetHierarchyApproversRequest> validator,
    IMetadataQueryProcessor queryProcessor,
    IQueryPersistenceService queryPersistenceService)
{
    private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
    private readonly IValidator<GetHierarchyApproversRequest> _validator = validator;
    private readonly IMetadataQueryProcessor _queryProcessor = queryProcessor;
    private readonly IQueryPersistenceService _queryPersistenceService = queryPersistenceService;

    public async Task<Result<GetHierarchyApproversResponse>> ExecuteAsync(
        GetHierarchyApproversRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return await Result<GetHierarchyApproversResponse>.FailAsync(
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList());
            }

            // Get hierarchy by type
            var hierarchy = await _readOnlyRepository.FindByPredicateAsync<Hierarchy>(
                h => h.Name == request.HierarchyType,
                cancellationToken);

            if (hierarchy == null)
            {
                return await Result<GetHierarchyApproversResponse>.FailAsync(
                    $"Hierarchy not found for type: {request.HierarchyType}");
            }

            // Get metadata keys (rules) for levels
            var metadataKeys = await _readOnlyRepository.FindAllByPredicateAsync<MetadataKey>(
                mk => mk.HierarchyId == hierarchy.Id &&
                      (mk.KeyName.StartsWith("level.1") || mk.KeyName.StartsWith("level.2")),
                cancellationToken);

            // Process each level and apply rules
            var level1Approvers = await GetApproversForLevel(
                hierarchy.Id,
                metadataKeys.Where(mk => mk.KeyName.StartsWith("level.1")),
                request.RequestedByTMID,
                request.RequestedForTMID,
                cancellationToken);

            var level2Approvers = await GetApproversForLevel(
                hierarchy.Id,
                metadataKeys.Where(mk => mk.KeyName.StartsWith("level.2")),
                request.RequestedByTMID,
                request.RequestedForTMID,
                cancellationToken);

            var response = new GetHierarchyApproversResponse
            {
                Approvers = new ApproversDto
                {
                    ApprovalLevel1 = level1Approvers.Select(e => MapToApproverDetails(e, hierarchy.Id.ToString())).ToList(),
                    ApprovalLevel2 = level2Approvers.Select(e => MapToApproverDetails(e, hierarchy.Id.ToString())).ToList()
                }
            };

            return await Result<GetHierarchyApproversResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<GetHierarchyApproversResponse>.FailAsync(
                $"Error getting hierarchy approvers: {ex.Message}",
                ResultStatus.Error);
        }
    }

    private async Task<List<Employee>> GetApproversForLevel(
        int hierarchyId,
        IEnumerable<MetadataKey> levelRules,
        string? requestedByTMID,
        string? requestedForTMID,
        CancellationToken cancellationToken)
    {
        var approvers = new List<Employee>();

        foreach (var rule in levelRules.OrderBy(r => r.KeyName))
        {
            var queryMatrix = _queryPersistenceService.ParseFromStorageFormat(rule.KeyName);

            // Build expression with context
            var expression = _queryProcessor.BuildExpressionFromQueryMatrix<Employee>(
                queryMatrix,
                requestedByTMID ?? requestedForTMID); // Use either TMID as context

            var ruleResults = await _readOnlyRepository.FindAllByPredicateIncludeAsync(
                expression,
                _queryProcessor.GetRequiredIncludes(),
                cancellationToken);

            approvers.AddRange(ruleResults);
        }

        return approvers.Distinct().ToList();
    }

    private static ApproverDetailsDto MapToApproverDetails(Employee employee, string hierarchyId)
    {
        // Existing mapping logic
        return new ApproverDetailsDto
        {
            TMID = employee.TMID,
            Name = employee.Name,
            Email = employee.Email,
            Title = employee.Title,
            JobCode = employee.JobCode,
            Metadata = employee.Approvers
                .Where(a => a.HierarchyId.ToString() == hierarchyId)
                .SelectMany(a => a.Metadata)
                .Select(m => new ApproverMetadataDto
                {
                    ApproverId = employee.Id.ToString(),
                    ApproverName = employee.Name,
                    HierarchyId = int.Parse(hierarchyId),
                    Key = m.Key,
                    Value = m.Value
                })
                .ToList()
        };
    }
}