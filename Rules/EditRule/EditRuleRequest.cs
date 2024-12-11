using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Rules.EditRule
{
    public record EditRuleRequest
    {
        public int HierarchyId { get; init; }
        public int LevelNumber { get; init; }
        public string RuleNumber { get; init; }
        public QueryMatrix QueryMatrix { get; init; }
    }

}
