namespace QueryRulesEngine.Features.Rules.GetRules.Models;

public sealed record GetRulesResponse
{
    public required int HierarchyId { get; init; }
    public required IReadOnlyCollection<HierarchyLevel> Levels { get; init; } = [];
}
