using QueryRulesEngine.QueryEngine.Builders;
using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Tests
{
    [TestFixture]
    public class QueryBuilderTests
    {
        private QueryBuilder _builder;

        [SetUp]
        public void Setup()
        {
            _builder = new QueryBuilder();
        }

        [Test]
        public void WithLogicalOperator_ValidOperator_SetsOperator()
        {
            // Act
            var result = _builder
                .WithLogicalOperator(QueryOperator.Or)
                .Build();

            // Assert
            Assert.That(result.LogicalOperator, Is.EqualTo(QueryOperator.Or));
        }

        [Test]
        public void WithLogicalOperator_NonLogicalOperator_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _builder.WithLogicalOperator(QueryOperator.Equal));
        }

        [Test]
        public void AddCondition_SimpleEquality_CreatesValidCondition()
        {
            // Act
            var result = _builder
                .AddCondition("Department", QueryOperator.Equal, "Sales")
                .Build();

            // Assert
            var condition = result.Conditions.Single();
            Assert.Multiple(() =>
            {
                Assert.That(condition.Field, Is.EqualTo("Department"));
                Assert.That(condition.Operator, Is.EqualTo(QueryOperator.Equal));
                Assert.That(condition.Value.Value, Is.EqualTo("Sales"));
                Assert.That(condition.Value.Type, Is.EqualTo(ConditionValueType.Single));
            });
        }

        [Test]
        public void AddCondition_InOperator_CreatesArrayCondition()
        {
            // Arrange
            var values = new[] { "MGR1", "MGR2", "MGR3" };

            // Act
            var result = _builder
                .AddCondition("JobCode", QueryOperator.In, values)
                .Build();

            // Assert
            var condition = result.Conditions.Single();
            Assert.Multiple(() =>
            {
                Assert.That(condition.Field, Is.EqualTo("JobCode"));
                Assert.That(condition.Operator, Is.EqualTo(QueryOperator.In));
                Assert.That(condition.Value.Type, Is.EqualTo(ConditionValueType.Array));
                Assert.That(condition.Value.Value, Is.EquivalentTo(values));
            });
        }

        [Test]
        public void AddNestedConditions_ComplexQuery_CreatesValidStructure()
        {
            // Act
            var result = _builder
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Department", QueryOperator.Equal, "Sales")
                .AddNestedConditions(nested => nested
                    .WithLogicalOperator(QueryOperator.Or)
                    .AddCondition("Experience", QueryOperator.GreaterThan, 5)
                    .AddCondition("Title", QueryOperator.Equal, "Manager"))
                .Build();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.LogicalOperator, Is.EqualTo(QueryOperator.And));
                Assert.That(result.Conditions, Has.Count.EqualTo(1));
                Assert.That(result.NestedMatrices, Has.Count.EqualTo(1));

                var nestedMatrix = result.NestedMatrices.Single();
                Assert.That(nestedMatrix.LogicalOperator, Is.EqualTo(QueryOperator.Or));
                Assert.That(nestedMatrix.Conditions, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public void AddCondition_NullField_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _builder.AddCondition(null, QueryOperator.Equal, "Value"));
        }

        [Test]
        public void AddCondition_NullValue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _builder.AddCondition("Field", QueryOperator.Equal, null));
        }
        [Test]
        public void AddCondition_MetadataField_CreatesValidCondition()
        {
            // Act
            var result = _builder
                .AddCondition("Metadata.expense_limit", QueryOperator.GreaterThan, "5000")
                .Build();

            // Assert
            var condition = result.Conditions.Single();
            Assert.Multiple(() =>
            {
                Assert.That(condition.Field, Is.EqualTo("Metadata.expense_limit"));
                Assert.That(condition.Operator, Is.EqualTo(QueryOperator.GreaterThan));
                Assert.That(condition.Value.Value, Is.EqualTo("5000"));
                Assert.That(condition.Value.Type, Is.EqualTo(ConditionValueType.Single));
            });
        }

        [Test]
        public void AddNestedConditions_CombiningMetadataAndEntityFields_CreatesValidStructure()
        {
            // Act
            var result = _builder
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Department", QueryOperator.Equal, "Sales")
                .AddNestedConditions(nested => nested
                    .WithLogicalOperator(QueryOperator.Or)
                    .AddCondition("Metadata.location", QueryOperator.Equal, "NY")
                    .AddCondition("Metadata.override_approver", QueryOperator.Equal, "331220"))
                .Build();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.LogicalOperator, Is.EqualTo(QueryOperator.And));
                Assert.That(result.Conditions, Has.Count.EqualTo(1));
                Assert.That(result.NestedMatrices, Has.Count.EqualTo(1));

                var nestedMatrix = result.NestedMatrices.Single();
                Assert.That(nestedMatrix.LogicalOperator, Is.EqualTo(QueryOperator.Or));
                Assert.That(nestedMatrix.Conditions, Has.Count.EqualTo(2));
                Assert.That(nestedMatrix.Conditions.All(c => c.Field.StartsWith("Metadata.")), Is.True);
            });
        }

        [Test]
        public void AddCondition_MetadataArrayCondition_CreatesValidCondition()
        {
            // Arrange
            var locations = new[] { "NY", "CA", "TX" };

            // Act
            var result = _builder
                .AddCondition("Metadata.location", QueryOperator.In, locations)
                .Build();

            // Assert
            var condition = result.Conditions.Single();
            Assert.Multiple(() =>
            {
                Assert.That(condition.Field, Is.EqualTo("Metadata.location"));
                Assert.That(condition.Operator, Is.EqualTo(QueryOperator.In));
                Assert.That(condition.Value.Type, Is.EqualTo(ConditionValueType.Array));
                Assert.That(condition.Value.Value, Is.EquivalentTo(locations));
            });
        }
    }
}
