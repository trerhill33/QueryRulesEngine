using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Rules.EditRule
{
    public sealed record EditRuleResponse(
        string KeyName,
        QueryMatrix UpdatedQueryMatrix
    );
}
