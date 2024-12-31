namespace QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers.Models;

public sealed record GetHierarchyApproversRequest
{
    public required string OriginApplication { get; init; }
    public required string HierarchyType { get; init; }
    public string? RequestedByTMID { get; init; }
    public string? RequestedForTMID { get; init; }
}