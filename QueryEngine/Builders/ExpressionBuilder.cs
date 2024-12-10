using QueryRulesEngine.QueryEngine.Common.Models;
using System.Linq.Expressions;

namespace QueryRulesEngine.QueryEngine.Builders
{
    public static class ExpressionBuilder
    {
        public static Expression BuildComparisonExpression(Expression property, Expression value, QueryOperator op)
        {
            return op.Value switch
            {
                "_eq" => Expression.Equal(property, value),
                "_neq" => Expression.NotEqual(property, value),
                "_gt" => Expression.GreaterThan(property, value),
                "_lt" => Expression.LessThan(property, value),
                "_gte" => Expression.GreaterThanOrEqual(property, value),
                "_lte" => Expression.LessThanOrEqual(property, value),
                "_in" => BuildInExpression(property, value),
                "_nin" => Expression.Not(BuildInExpression(property, value)),
                "_like" => BuildLikeExpression(property, value, false),
                "_ilike" => BuildLikeExpression(property, value, true),
                _ => throw new NotSupportedException($"Operator {op.Value} not supported")
            };
        }

        public static Expression CombineConditions(List<Expression> conditions, QueryOperator logicalOperator)
        {
            if (conditions.Count == 0)
                return Expression.Constant(true);

            return logicalOperator.Value switch
            {
                "_and" => conditions.Aggregate(Expression.AndAlso),
                "_or" => conditions.Aggregate(Expression.OrElse),
                "_not" when conditions.Count == 1 => Expression.Not(conditions[0]),
                _ => throw new NotSupportedException($"Logical operator {logicalOperator.Value} not supported")
            };
        }

        public static Expression BuildInExpression(Expression property, Expression values)
        {
            if (values.Type.IsArray ||
                (values.Type.IsGenericType && values.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                values.Type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var containsMethod = typeof(Enumerable)
                    .GetMethods()
                    .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(property.Type);

                return Expression.Call(null, containsMethod, values, property);
            }

            throw new ArgumentException("Values must be an array or IEnumerable<T> for IN operator.");
        }

        public static Expression BuildLikeExpression(Expression property, Expression pattern, bool caseInsensitive)
        {
            if (property.Type != typeof(string))
                throw new ArgumentException("LIKE operator can only be used with string properties.");

            var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;

            if (caseInsensitive)
            {
                var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
                property = Expression.Call(property, toLowerMethod);
                pattern = Expression.Call(pattern, toLowerMethod);
            }

            return Expression.Call(property, containsMethod, pattern);
        }
    }
}
