namespace QueryRulesEngine.Features.Rules.RemoveRule
{
    public sealed record RemoveRuleResponse(
        int RuleId,
        string KeyName
    );
}
