namespace QueryRulesEngine.QueryEngine.Common.Models
{
    public sealed record QueryCondition
    {
        public required string Field { get; init; }
        public required QueryOperator Operator { get; init; }
        public required ConditionValue Value { get; init; }
    }

}
