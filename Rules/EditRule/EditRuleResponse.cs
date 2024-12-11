using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Rules.EditRule
{
    public sealed record EditRuleResponse(
        int RuleId,
        string KeyName,
        QueryMatrix UpdatedQueryMatrix
    );
}
