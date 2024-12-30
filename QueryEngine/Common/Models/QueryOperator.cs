namespace QueryRulesEngine.QueryEngine.Common.Models;

public sealed record QueryOperator(string Value, string Description, OperatorType Type = OperatorType.Comparison)
{
    #region Comparison Operators
    public static readonly QueryOperator Equal = new("_eq", "equals");
    public static readonly QueryOperator NotEqual = new("_neq", "not equals");
    public static readonly QueryOperator GreaterThan = new("_gt", "greater than");
    public static readonly QueryOperator LessThan = new("_lt", "less than");
    public static readonly QueryOperator GreaterThanOrEqual = new("_gte", "greater than or equal");
    public static readonly QueryOperator LessThanOrEqual = new("_lte", "less than or equal");
    public static readonly QueryOperator In = new("_in", "in array");
    public static readonly QueryOperator NotIn = new("_nin", "not in array");
    #endregion

    #region Text Operators
    public static readonly QueryOperator Like = new("_like", "pattern match", OperatorType.Text);
    public static readonly QueryOperator ILike = new("_ilike", "case insensitive pattern match", OperatorType.Text);
    #endregion

    #region Logical Operators
    public static readonly QueryOperator And = new("_and", "logical and", OperatorType.Logical);
    public static readonly QueryOperator Or = new("_or", "logical or", OperatorType.Logical);
    public static readonly QueryOperator Not = new("_not", "logical not", OperatorType.Logical);
    #endregion

    public static readonly IReadOnlyCollection<QueryOperator> All =
    [
        Equal, NotEqual, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual,
        Like, ILike,
        And, Or, Not
    ];

    public static QueryOperator FromString(string value) =>
        All.FirstOrDefault(op => op.Value == value)
        ?? throw new ArgumentException($"Unsupported operator: {value}", nameof(value));
}
