using QueryRulesEngine.dtos;

namespace QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers.Models;

public sealed record GetHierarchyApproversResponse
{
    public required ApproversDto Approvers { get; init; }
}