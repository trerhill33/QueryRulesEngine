using QueryRulesEngine.QueryEngine.Common.Models;
using System.Collections;
using System.Text;

namespace QueryRulesEngine.QueryEngine.Persistence;

public class QueryPersistenceService : IQueryPersistenceService
{
    #region Convert query matrix model to persisted query 
    /// <summary>
    /// Converts a QueryMatrix to the storage format string
    /// </summary>
    public string ConvertToStorageFormat(QueryMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var stringBuilder = new StringBuilder();

        // Add logical operator
        stringBuilder.Append($"[{matrix.LogicalOperator.Value}]");

        // Add conditions
        foreach (var condition in matrix.Conditions)
        {
            stringBuilder.Append(ConvertConditionToString(condition));
        }

        // Add nested matrices (if any)
        foreach (var nestedMatrix in matrix.NestedMatrices)
        {
            stringBuilder.Append(ConvertToStorageFormat(nestedMatrix));
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Converts the value string to its appropriate type based on the field
    /// </summary>
    private static object ConvertToTypedValue(string valueStr, string field)
    {
        // Example: For the "Amount" field, convert to decimal
        // Implement field-specific type conversions as needed
        return field switch
        {
            "Amount" => decimal.Parse(valueStr),
            _ => valueStr // Default to string if type is unknown
        };
    }

    /// <summary>
    /// Converts a single QueryCondition to its string representation
    /// </summary>
    private static string ConvertConditionToString(QueryCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        string valueStr = condition.Value.Type switch
        {
            ConditionValueType.Single => condition.Value.Value.ToString()!,
            ConditionValueType.Pattern => condition.Value.Value.ToString()!,
            ConditionValueType.Array => string.Join("|", ((IEnumerable)condition.Value.Value)
                .Cast<object>()
                .Select(x => x.ToString())),
            _ => throw new NotSupportedException($"ConditionValueType '{condition.Value.Type}' is not supported.")
        };

        // Replace spaces with ~ to handle special characters
        valueStr = valueStr.Replace(" ", "~");

        return $"[{condition.Field}{condition.Operator.Value}_{valueStr}]";

        throw new ArgumentNullException(nameof(condition));
    }
    #endregion

    #region Parse persisted query to query matrix model
    /// <summary>
    /// Parses a storage format string back into a QueryMatrix
    /// </summary>
    public QueryMatrix ParseFromStorageFormat(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            throw new ArgumentException("Query string cannot be null or empty.", nameof(queryString));

        // Only look for "query:" in the original query string, not nested ones
        var segments = queryString.Contains("query:")
            ? ExtractSegments(queryString)
            : ExtractSegmentsWithoutQueryPrefix(queryString);

        if (segments.Count == 0)
            throw new ArgumentException("Invalid query string format.", nameof(queryString));

        // First segment must be a logical operator
        var logicalOp = ParseLogicalOperator(segments[0]);
        var conditions = new List<QueryCondition>();
        var nestedMatrices = new List<QueryMatrix>();

        // Track nested matrix construction
        StringBuilder? currentNestedQuery = null;

        // Process remaining segments
        for (int i = 1; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (IsLogicalOperatorSegment(segment))
            {
                // Start new nested matrix
                currentNestedQuery = new StringBuilder(segment);
            }
            else if (currentNestedQuery != null)
            {
                // Add to current nested matrix
                currentNestedQuery.Append(segment);

                // If this is the last segment or next segment starts a new nested matrix,
                // process the current nested matrix
                if (i == segments.Count - 1 || IsLogicalOperatorSegment(segments[i + 1]))
                {
                    nestedMatrices.Add(ParseFromStorageFormat(currentNestedQuery.ToString()));
                    currentNestedQuery = null;
                }
            }
            else
            {
                // Regular condition
                conditions.Add(ParseCondition(segment));
            }
        }

        return new QueryMatrix
        {
            LogicalOperator = logicalOp,
            Conditions = conditions,
            NestedMatrices = nestedMatrices
        };
    }

    /// <summary>
    /// Extract each segment in the query
    /// </summary>
    private static List<string> ExtractSegmentsWithoutQueryPrefix(string queryString)
    {
        var segments = new List<string>();
        var currentSegment = new StringBuilder();
        int bracketCount = 0;

        foreach (char c in queryString)
        {
            if (c == '[')
                bracketCount++;
            if (c == ']')
                bracketCount--;

            currentSegment.Append(c);

            if (bracketCount == 0 && currentSegment.Length > 0)
            {
                segments.Add(currentSegment.ToString());
                currentSegment.Clear();
            }
        }

        return segments;
    }

    /// <summary>
    /// Extracts individual segments from the storage format string
    /// </summary>
    private static List<string> ExtractSegments(string queryString)
    {
        // Find the start of the actual query (after "query:")
        var queryStart = queryString.IndexOf("query:");
        if (queryStart == -1)
            throw new ArgumentException("Invalid query string format - missing 'query:' marker");

        // Get just the query part
        var actualQuery = queryString[(queryStart + 6)..]; // +6 to skip "query:"

        var segments = new List<string>();
        var currentSegment = new StringBuilder();
        int bracketCount = 0;

        foreach (char c in actualQuery)
        {
            if (c == '[')
                bracketCount++;
            if (c == ']')
                bracketCount--;

            currentSegment.Append(c);

            if (bracketCount == 0 && currentSegment.Length > 0)
            {
                segments.Add(currentSegment.ToString());
                currentSegment.Clear();
            }
        }

        return segments;
    }

    /// <summary>
    /// Parses a single condition string into a QueryCondition object
    /// </summary>
    private static QueryCondition ParseCondition(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            throw new ArgumentException("Condition segment cannot be null or empty.", nameof(segment));

        // Remove surrounding brackets
        var content = segment.Trim('[', ']');

        // Split into parts: Field, Operator, Value
        var parts = content.Split('_', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            throw new ArgumentException($"Invalid condition format: {segment}");

        var field = parts[0];
        var opValue = "_" + parts[1];  // Add underscore prefix back
        var valueStr = parts[2].Replace("~", " "); // Restore spaces

        var op = QueryOperator.FromString(opValue);

        ArgumentNullException.ThrowIfNull(op);

        // Determine the type of condition value based on the operator
        ConditionValue conditionValue = op.Type switch
        {
            OperatorType.Comparison => ConditionValue.Single(ConvertToTypedValue(valueStr, field)),
            OperatorType.Text => ConditionValue.Pattern(valueStr),
            _ => throw new NotSupportedException($"Operator type '{op.Type}' is not supported for conditions.")
        };

        return new QueryCondition
        {
            Field = field,
            Operator = op,
            Value = conditionValue
        };
    }

    /// <summary>
    /// Parses the logical operator from a segment
    /// </summary>
    private static QueryOperator ParseLogicalOperator(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            throw new ArgumentException("Logical operator segment cannot be null or empty.", nameof(segment));

        var content = segment.Trim('[', ']');

        var op = QueryOperator.FromString(content)
            ?? throw new ArgumentException($"Unsupported logical operator: {content}");

        if (!op.Type.Equals(OperatorType.Logical))
            throw new ArgumentException($"Operator '{op.Value}' is not a logical operator.");

        return op;
    }

    /// <summary>
    /// Determines if a segment starts with a logical operator
    /// </summary>
    private static bool IsLogicalOperatorSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return false;

        var content = segment.Trim('[', ']');
        return content switch
        {
            "_and" => true,
            "_or" => true,
            "_not" => true,
            _ => false
        };
    }
    #endregion
}
