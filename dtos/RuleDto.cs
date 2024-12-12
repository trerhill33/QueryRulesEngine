using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.dtos
{
    public class RuleDto
    {
        public int HierarchyId { get; init; }
        public int LevelNumber { get; init; }
        public string RuleNumber { get; init; }
        public QueryMatrix QueryMatrix { get; set; }
    }
}
