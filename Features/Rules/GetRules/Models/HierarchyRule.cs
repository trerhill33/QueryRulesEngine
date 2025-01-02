using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Features.Rules.GetRules.Models;

public sealed record HierarchyRule
{
    public required int RuleNumber { get; init; }
    public required RuleConfiguration Configuration { get; init; }
    public required QueryMatrix QueryMatrix { get; init; }
    public required string OriginalQuery { get; init; }
}
