using QueryRulesEngine.QueryEngine.Builders;
using QueryRulesEngine.QueryEngine.Common.Models;
using System.Linq.Expressions;

namespace QueryRulesEngine.QueryEngine.Processors
{
    public class QueryProcessor : IQueryProcessor
    {
        public IQueryable<T> ApplyQuery<T>(IQueryable<T> source, QueryMatrix matrix)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(matrix);

            var expression = BuildExpression<T>(matrix);
            return source.Where(expression);
        }

        public Expression<Func<T, bool>> BuildExpression<T>(QueryMatrix matrix)
        {
            ArgumentNullException.ThrowIfNull(matrix);

            var parameter = Expression.Parameter(typeof(T), "x");
            var body = BuildMatrixExpression(matrix, parameter);

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        private Expression BuildMatrixExpression(QueryMatrix matrix, ParameterExpression parameter)
        {
            var expressions = matrix.Conditions
                .Select(c => BuildConditionExpression(c, parameter))
                .Concat(matrix.NestedMatrices
                    .Select(nested => BuildMatrixExpression(nested, parameter)))
                .ToList();

            return ExpressionBuilder.CombineConditions(expressions, matrix.LogicalOperator);
        }

        private Expression BuildConditionExpression(QueryCondition condition, ParameterExpression parameter)
        {
            var property = Expression.Property(parameter, condition.Field);
            var value = Expression.Constant(condition.Value.Value);
            return ExpressionBuilder.BuildComparisonExpression(property, value, condition.Operator);
        }
    }
}
