namespace QueryRulesEngine.Rules.RemoveRule
{
    public sealed record RemoveRuleRequest(
        int HierarchyId,
        int LevelNumber,
        string RuleNumber
    );
}
