using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.QueryEngine.Builders
{
    /// <summary>
    /// Fluent builder for constructing QueryMatrix objects.
    /// </summary>
    public sealed class QueryBuilder : IQueryBuilder
    {
        private readonly List<QueryCondition> _conditions = [];
        private readonly List<QueryMatrix> _nestedMatrices = [];
        private QueryOperator _logicalOperator = QueryOperator.And;

        /// <summary>
        /// Sets the logical operator (AND/OR/NOT) for combining conditions.
        /// </summary>
        /// <param name="op">Logical operator to apply.</param>
        /// <returns>The current instance of QueryBuilder.</returns>
        public QueryBuilder WithLogicalOperator(QueryOperator op)
        {
            if (op.Type != OperatorType.Logical)
                throw new ArgumentException("Operator must be a logical operator (AND/OR/NOT).", nameof(op));

            _logicalOperator = op;
            return this;
        }

        /// <summary>
        /// Adds a condition. The method intelligently determines the ConditionValueType based on the operator.
        /// </summary>
        /// <param name="field">Field name to apply the condition.</param>
        /// <param name="op">Operator to apply.</param>
        /// <param name="value">Value to compare against. For 'IN'/'NOT IN', provide an IEnumerable<object>.</param>
        /// <returns>The current instance of QueryBuilder.</returns>
        public QueryBuilder AddCondition(string field, QueryOperator op, object value)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or whitespace.", nameof(field));

            if (value == null)
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");

            if (op.Type == OperatorType.Logical)
                throw new ArgumentException("Cannot use logical operator for condition.", nameof(op));

            ConditionValue conditionValue = op.Type switch
            {
                OperatorType.Text => ConditionValue.Pattern(value.ToString()!),
                OperatorType.Comparison when op == QueryOperator.In || op == QueryOperator.NotIn =>
                    ConditionValue.Array(((IEnumerable<object>)value).ToList()),
                OperatorType.Comparison => ConditionValue.Single(value),
                _ => throw new NotSupportedException($"Operator type '{op.Type}' is not supported.")
            };

            _conditions.Add(new QueryCondition
            {
                Field = field,
                Operator = op,
                Value = conditionValue
            });

            return this;
        }

        /// <summary>
        /// Adds a nested query matrix for complex conditions.
        /// </summary>
        /// <param name="matrix">Nested QueryMatrix object.</param>
        /// <returns>The current instance of QueryBuilder.</returns>
        public QueryBuilder AddNestedMatrix(QueryMatrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix), "Nested QueryMatrix cannot be null.");

            _nestedMatrices.Add(matrix);
            return this;
        }

        /// <summary>
        /// Adds a nested condition group using a sub-builder action.
        /// </summary>
        /// <param name="builderAction">Action to configure the nested QueryBuilder.</param>
        /// <returns>The current instance of QueryBuilder.</returns>
        public QueryBuilder AddNestedConditions(Action<QueryBuilder> builderAction)
        {
            if (builderAction == null)
                throw new ArgumentNullException(nameof(builderAction), "Builder action cannot be null.");

            var nestedBuilder = new QueryBuilder();
            builderAction(nestedBuilder);
            _nestedMatrices.Add(nestedBuilder.Build());
            return this;
        }

        /// <summary>
        /// Builds and returns the final QueryMatrix.
        /// </summary>
        /// <returns>A constructed QueryMatrix object.</returns>
        public QueryMatrix Build()
        {
            return new QueryMatrix
            {
                LogicalOperator = _logicalOperator,
                Conditions = _conditions.AsReadOnly(),
                NestedMatrices = _nestedMatrices.AsReadOnly()
            };
        }
    }
}

