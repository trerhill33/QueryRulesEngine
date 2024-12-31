using FluentValidation;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers.Models;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using System.Linq.Expressions;

namespace QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers;

public sealed class GetHierarchyApproversService(
    IReadOnlyRepositoryAsync<int> readOnlyRepository,
    IValidator<GetHierarchyApproversRequest> validator)
{
    private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
    private readonly IValidator<GetHierarchyApproversRequest> _validator = validator;

    public async Task<Result<GetHierarchyApproversResponse>> ExecuteAsync(
        GetHierarchyApproversRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return await Result<GetHierarchyApproversResponse>.FailAsync(
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList());
            }

            var employees = await _readOnlyRepository.FindAllByPredicateIncludeAsync<Employee>(
                e => e.Approvers.Any(a => a.HierarchyId.ToString() == request.HierarchyId),
                [
                    e => e.Approvers,
                    e => e.Approvers.Select(a => a.Metadata)
                ],
                cancellationToken);

            var level1Approvers = employees
                .Where(e => e.Approvers.Any(a =>
                    a.HierarchyId.ToString() == request.HierarchyId &&
                    a.Metadata.Any(m => m.Key.StartsWith("level.1"))))
                .Select(e => MapToApproverDetails(e, request.HierarchyId))
                .ToList();

            var level2Approvers = employees
                .Where(e => e.Approvers.Any(a =>
                    a.HierarchyId.ToString() == request.HierarchyId &&
                    a.Metadata.Any(m => m.Key.StartsWith("level.2"))))
                .Select(e => MapToApproverDetails(e, request.HierarchyId))
                .ToList();

            var response = new GetHierarchyApproversResponse
            {
                Approvers = new ApproversDto
                {
                    ApprovalLevel1 = level1Approvers,
                    ApprovalLevel2 = level2Approvers
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

    private static ApproverDetailsDto MapToApproverDetails(Employee employee, string hierarchyId)
    {
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