namespace QueryRulesEngine.Features.Rules.GetRules.Models;

public class HierarchyLevelRules
{
    public int Level { get; init; }
    public List<HierarchyRule> Rules { get; init; } = [];
}
