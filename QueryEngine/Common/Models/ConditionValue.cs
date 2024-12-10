namespace QueryRulesEngine.QueryEngine.Common.Models
{
    public sealed record ConditionValue(object Value, ValueType Type)
    {
        public static ConditionValue Single(object value) => new(value, ConditionValueType.Single);
        public static ConditionValue Array(IEnumerable<object> values) => new(values, ConditionValueType.Array);
        public static ConditionValue Pattern(string pattern) => new(pattern, ConditionValueType.Pattern);
    }
}
