using QueryRulesEngine.Entities;
using QueryRulesEngine.QueryEngine.Builders;
using QueryRulesEngine.QueryEngine.Common.Models;
using System.Linq.Expressions;

namespace QueryRulesEngine.QueryEngine.Processors
{
    public class MetadataQueryProcessor(IQueryProcessor baseProcessor) : IMetadataQueryProcessor
    {
        private readonly IQueryProcessor _baseProcessor = baseProcessor;
        private const string MetadataPrefix = "Metadata.";
        private const string EmployeePrefix = "Employee.";

        public IQueryable<T> ApplyQuery<T>(IQueryable<T> source, QueryMatrix matrix) where T : class
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(matrix);

            var expression = BuildExpression<T>(matrix);
            return source.Where(expression);
        }

        public Expression<Func<T, bool>> BuildExpression<T>(QueryMatrix matrix) where T : class
        {
            ArgumentNullException.ThrowIfNull(matrix);

            var parameter = Expression.Parameter(typeof(T), "x");
            var body = BuildMatrixExpression(matrix, parameter);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        private Expression BuildMatrixExpression(QueryMatrix matrix, ParameterExpression parameter)
        {
            var expressions = new List<Expression>();

            // Handle direct conditions
            foreach (var condition in matrix.Conditions)
            {
                if (typeof(Employee).IsAssignableFrom(parameter.Type))
                {
                    // Starting from Employee - handle metadata navigation
                    expressions.Add(
                        condition.Field.StartsWith(MetadataPrefix)
                            ? BuildMetadataRelatedExpression(parameter, condition)
                            : BuildSimpleExpression(parameter, condition));
                }
                else if (typeof(Metadata).IsAssignableFrom(parameter.Type))
                {
                    // Starting from Metadata - handle employee navigation
                    expressions.Add(
                        condition.Field.StartsWith(EmployeePrefix)
                            ? BuildEmployeeRelatedExpression(parameter, condition)
                            : BuildSimpleExpression(parameter, condition));
                }
            }

            // Handle nested matrices
            foreach (var nested in matrix.NestedMatrices)
            {
                expressions.Add(BuildMatrixExpression(nested, parameter));
            }

            return ExpressionBuilder.CombineConditions(expressions, matrix.LogicalOperator);
        }

        private Expression BuildSimpleExpression(ParameterExpression parameter, QueryCondition condition)
        {
            var property = Expression.Property(parameter, condition.Field);
            var value = Expression.Constant(condition.Value.Value);
            return ExpressionBuilder.BuildComparisonExpression(property, value, condition.Operator);
        }

        private Expression BuildMetadataRelatedExpression(ParameterExpression employeeParam, QueryCondition condition)
        {
            // Handle Employee -> Approvers -> Metadata path
            var metadataKey = condition.Field.Substring(MetadataPrefix.Length);

            // Create parameters for the navigation path
            var approverParam = Expression.Parameter(typeof(Approver), "a");
            var metadataParam = Expression.Parameter(typeof(Metadata), "m");

            // Build m.Key == metadataKey
            var keyEqual = Expression.Equal(
                Expression.Property(metadataParam, nameof(Metadata.Key)),
                Expression.Constant(metadataKey));

            // Build value comparison
            var valueProperty = Expression.Property(metadataParam, nameof(Metadata.Value));
            var valueComparison = BuildMetadataValueComparison(valueProperty, condition);

            // Combine key and value conditions
            var metadataCondition = Expression.AndAlso(keyEqual, valueComparison);

            // Build the nested Any expressions
            // First Any: a.Metadata.Any(m => m.Key == key && <value comparison>)
            var metadataAny = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Any),
                new[] { typeof(Metadata) },
                Expression.Property(approverParam, nameof(Approver.Metadata)),
                Expression.Lambda<Func<Metadata, bool>>(metadataCondition, metadataParam));

            // Second Any: e.Approvers.Any(a => a.Metadata.Any(...))
            return Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Any),
                new[] { typeof(Approver) },
                Expression.Property(employeeParam, nameof(Employee.Approvers)),
                Expression.Lambda<Func<Approver, bool>>(metadataAny, approverParam));
        }

        private Expression BuildEmployeeRelatedExpression(ParameterExpression metadataParam, QueryCondition condition)
        {
            // Handle Metadata -> Approver -> Employee path
            var employeeField = condition.Field.Substring(EmployeePrefix.Length);

            // Build the navigation path to Employee
            var approverProperty = Expression.Property(metadataParam, nameof(Metadata.Approver));
            var employeeProperty = Expression.Property(approverProperty, nameof(Approver.Employee));

            // Build the final property access and comparison
            var targetProperty = Expression.Property(employeeProperty, employeeField);
            var value = Expression.Constant(condition.Value.Value);

            return ExpressionBuilder.BuildComparisonExpression(targetProperty, value, condition.Operator);
        }

        private Expression BuildMetadataValueComparison(Expression valueProperty, QueryCondition condition)
        {
            Expression compareValue;

            // For numeric comparisons, parse both sides as decimal
            if (condition.Operator.Value is "_gt" or "_lt" or "_gte" or "_lte")
            {
                var parseMethod = typeof(decimal).GetMethod(nameof(decimal.Parse), new[] { typeof(string) });
                valueProperty = Expression.Call(parseMethod, valueProperty);
                compareValue = Expression.Constant(decimal.Parse(condition.Value.Value.ToString()));
            }
            else
            {
                compareValue = Expression.Constant(condition.Value.Value.ToString());
            }

            return ExpressionBuilder.BuildComparisonExpression(valueProperty, compareValue, condition.Operator);
        }
    }
}