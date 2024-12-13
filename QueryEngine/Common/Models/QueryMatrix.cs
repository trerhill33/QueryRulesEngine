namespace QueryRulesEngine.QueryEngine.Common.Models
{
    public sealed record QueryMatrix
    {
        public required QueryOperator LogicalOperator { get; set; }
        public required IReadOnlyCollection<QueryCondition> Conditions { get; init; } = [];
        public IReadOnlyCollection<QueryMatrix> NestedMatrices { get; init; } = [];
    }
}
