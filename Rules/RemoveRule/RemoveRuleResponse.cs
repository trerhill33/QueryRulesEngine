namespace QueryRulesEngine.Rules.RemoveRule
{
    public sealed record RemoveRuleResponse(
        int RuleId,
        string KeyName
    );
}
