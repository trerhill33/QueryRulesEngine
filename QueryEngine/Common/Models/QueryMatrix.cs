namespace QueryRulesEngine.QueryEngine.Common.Models
{
    public sealed record QueryMatrix
    {
        public required QueryOperator LogicalOperator { get; init; }
        public required IReadOnlyCollection<QueryCondition> Conditions { get; init; } = new List<QueryCondition>();
        public required IReadOnlyCollection<QueryMatrix> NestedMatrices { get; init; } = new List<QueryMatrix>();
    }
}
