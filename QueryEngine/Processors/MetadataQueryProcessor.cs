using Microsoft.EntityFrameworkCore;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Builders;
using QueryRulesEngine.QueryEngine.Common.Models;
using System.Linq.Expressions;

namespace QueryRulesEngine.QueryEngine.Processors;

public class MetadataQueryProcessor : IMetadataQueryProcessor
{
    private string? _context;
    private string? _hierarchyId;

    public Expression<Func<Employee, object>>[] GetRequiredIncludes() => [
        e => e.Approvers,
        e => e.Approvers.Select(a => a.Metadata)
    ];

    public Expression<Func<T, bool>> BuildExpressionFromQueryMatrix<T>(
        QueryMatrix matrix,
        string? hierarchyId = null,
        string? context = null) 
        where T : class
    {
        ArgumentNullException.ThrowIfNull(matrix);
        _context = context;
        _hierarchyId = hierarchyId;

        var parameter = Expression.Parameter(typeof(T), "x");
        var body = BuildMatrixExpression(matrix, parameter);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private Expression BuildMatrixExpression(
        QueryMatrix matrix, 
        ParameterExpression parameter)
    {
        var expressions = new List<Expression>();

        // Handle direct conditions (not nested)
        foreach (var condition in matrix.Conditions)
        {
            ValidateCondition(condition);

            if (condition.Value.Value?.ToString()?.StartsWith(QueryPrefixes.Context) == true)
            {
                expressions.Add(BuildContextExpression(parameter, condition));
            }
            else if (condition.Field.StartsWith(QueryPrefixes.Metadata))
            {
                expressions.Add(BuildMetadataRelatedExpression(parameter, condition));
            }
            else if (condition.Field.StartsWith(QueryPrefixes.Employee))
            {
                expressions.Add(BuildEmployeeExpression(parameter, condition));
            }
        }

        // Handle nested matrices (not direct)
        foreach (var nested in matrix.NestedMatrices)
        {
            expressions.Add(BuildMatrixExpression(nested, parameter));
        }

        return ExpressionBuilder.CombineConditions(expressions, matrix.LogicalOperator);
    }

    private Expression BuildEmployeeExpression(
        ParameterExpression parameter, 
        QueryCondition condition)
    {
        // Strip off the "Employee." prefix to get just the property name
        var propertyName = condition.Field.StartsWith(QueryPrefixes.Employee)
            ? condition.Field[QueryPrefixes.Employee.Length..]
            : condition.Field;

        var property = Expression.Property(parameter, propertyName);
        var value = Expression.Constant(condition.Value.Value);
        return ExpressionBuilder.BuildComparisonExpression(property, value, condition.Operator);
    }

    private Expression BuildMetadataRelatedExpression(
        ParameterExpression parameter, 
        QueryCondition condition)
    {
        // Get the metadata key name (e.g., "FinancialLimit" from "ApproverMetadataKey.FinancialLimit")
        var metadataKey = condition.Field[QueryPrefixes.Metadata.Length..];

        // Build navigation path
        var approversProperty = Expression.Property(parameter, "Approvers");

        // Build the metadata filter
        var approverParam = Expression.Parameter(typeof(Approver), "a");
        var metadataParam = Expression.Parameter(typeof(Metadata), "m");

        // m.Key == metadataKey && <value comparison>
        var keyEqual = Expression.Equal(
            Expression.Property(metadataParam, "Key"),
            Expression.Constant(metadataKey));

        var valueProperty = Expression.Property(metadataParam, "Value");
        var valueComparison = BuildMetadataValueComparison(valueProperty, condition);

        var metadataCondition = Expression.AndAlso(keyEqual, valueComparison);

        // Build the Any expressions for metadata
        var metadataAny = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Any),
            [typeof(Metadata)],
            Expression.Property(approverParam, "Metadata"),
            Expression.Lambda<Func<Metadata, bool>>(metadataCondition, metadataParam));

        // Add hierarchy filter if hierarchyId is present
        Expression approverCondition = metadataAny;
        if (_hierarchyId != null)
        {
            var hierarchyIdProperty = Expression.Property(approverParam, "HierarchyId");
            var hierarchyIdValue = Expression.Constant(int.Parse(_hierarchyId));
            var hierarchyIdEqual = Expression.Equal(hierarchyIdProperty, hierarchyIdValue);
            approverCondition = Expression.AndAlso(hierarchyIdEqual, metadataAny);
        }

        // Build the Any expression for approvers
        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Any),
            [typeof(Approver)],
            approversProperty,
            Expression.Lambda<Func<Approver, bool>>(metadataAny, approverParam));
    }

    private Expression BuildContextExpression(
        ParameterExpression parameter, 
        QueryCondition condition)
    {
        // Parse the context path (e.g., "@Context.RequestedByTMID.ReportsTo")
        var contextPath = condition.Value.Value.ToString()[QueryPrefixes.Context.Length..];
        var parts = contextPath.Split('.');

        // Use the actual context value passed in at runtime instead of a parameter
        var contextValue = Expression.Constant(_context);

        // If there's a property navigation (e.g., "ReportsTo")
        if (parts.Length > 1)
        {
            var propertyName = parts[1].StartsWith(QueryPrefixes.Employee)
                ? parts[1][QueryPrefixes.Employee.Length..]
                : parts[1];

            var propertyToCompare = Expression.Property(parameter, propertyName);

            return ExpressionBuilder.BuildComparisonExpression(
                propertyToCompare,
                contextValue,
                condition.Operator);
        }

        var fieldName = condition.Field.StartsWith(QueryPrefixes.Employee)
            ? condition.Field.Substring(QueryPrefixes.Employee.Length)
            : condition.Field;

        var property = Expression.Property(parameter, fieldName);
        return ExpressionBuilder.BuildComparisonExpression(
            property,
            contextValue,
            condition.Operator);
    }

    private Expression BuildMetadataValueComparison(
        Expression valueProperty, 
        QueryCondition condition)
    {
        Expression compareValue;

        // For numeric comparisons, parse both sides as decimal
        if (condition.Operator.Value is "_gt" or "_lt" or "_gte" or "_lte")
        {
            var parseMethod = typeof(decimal).GetMethod(nameof(decimal.Parse), [typeof(string)]);
            valueProperty = Expression.Call(parseMethod, valueProperty);
            compareValue = Expression.Constant(decimal.Parse(condition.Value.Value.ToString()));
        }
        else
        {
            compareValue = Expression.Constant(condition.Value.Value.ToString());
        }

        return ExpressionBuilder.BuildComparisonExpression(valueProperty, compareValue, condition.Operator);
    }

    private Expression<Func<Employee, bool>> BuildHierarchyFilter(int hierarchyId) 
        => e => e.Approvers.Any(a => a.HierarchyId == hierarchyId);

    private static void ValidateCondition(QueryCondition condition)
    {
        if (condition.Field.StartsWith(QueryPrefixes.Employee))
        {
            var propertyName = condition.Field[QueryPrefixes.Employee.Length..];
            if (typeof(Employee).GetProperty(propertyName) == null)
            {
                throw new ArgumentException($"Employee does not contain a property named '{propertyName}'.");
            }
        }
        else if (condition.Field.StartsWith(QueryPrefixes.Metadata))
        {
            // TODO: Place holder. Not sure if we need.
            // What we are searchiang for is dynamic and lives as records in the table. Might be tricky and non performant. I see the value, but need to determine the impact. 
        }
        else
        {
            throw new ArgumentException($"Unsupported field prefix in '{condition.Field}'.");
        }

        // Validate operator
        if (!QueryOperator.All.Contains(condition.Operator))
            throw new ArgumentException($"Unsupported operator: {condition.Operator.Value}");
    }
}