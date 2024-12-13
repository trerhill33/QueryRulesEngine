using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Features.Rules.EditRule;

public sealed record EditRuleResponse(
    string KeyName,
    QueryMatrix UpdatedQueryMatrix
);
