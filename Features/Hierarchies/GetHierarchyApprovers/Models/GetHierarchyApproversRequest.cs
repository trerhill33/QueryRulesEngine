namespace QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers.Models;

public sealed record GetHierarchyApproversRequest
{
    public required string HierarchyId { get; init; }
}
