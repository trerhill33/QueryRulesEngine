namespace QueryRulesEngine.Features.Rules.GetRules.Models;
public sealed record HierarchyLevel
{
    public required int Level { get; init; }
    public required IReadOnlyCollection<HierarchyRule> Rules { get; init; } = [];
}