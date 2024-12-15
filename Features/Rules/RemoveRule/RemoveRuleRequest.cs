namespace QueryRulesEngine.Features.Rules.RemoveRule;

public record RemoveRuleRequest(
    int HierarchyId,
    int LevelNumber,
    string RuleNumber
);
