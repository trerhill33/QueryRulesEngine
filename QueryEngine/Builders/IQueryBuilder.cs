using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.QueryEngine.Builders
{
    public interface IQueryBuilder
    {
        QueryBuilder AddCondition(string field, QueryOperator op, object value);
        QueryBuilder AddNestedConditions(Action<QueryBuilder> builderAction);
        QueryBuilder AddNestedMatrix(QueryMatrix matrix);
        QueryMatrix Build();
        QueryBuilder WithLogicalOperator(QueryOperator op);
    }
}