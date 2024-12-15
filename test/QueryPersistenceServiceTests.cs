using QueryRulesEngine.QueryEngine.Builders;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Persistence;

namespace QueryRulesEngine.Tests
{
    [TestFixture]
    public class QueryPersistenceServiceTests
    {
        private IQueryPersistenceService _persistenceService;

        [SetUp]
        public void Setup()
        {
            _persistenceService = new QueryPersistenceService();
        }

        [Test]
        public void ConvertToStorageFormat_SimpleCondition_ReturnsCorrectFormat()
        {
            // Arrange
            var matrix = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Department", QueryOperator.Equal, "Sales")
                .Build();

            // Act
            var result = _persistenceService.ConvertToStorageFormat(matrix);

            // Assert
            Assert.That(result, Is.EqualTo("[_and][Department_eq_Sales]"));
        }

        [Test]
        public void ConvertToStorageFormat_MultipleConditions_ReturnsCorrectFormat()
        {
            // Arrange
            var matrix = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.Or)
                .AddCondition("Department", QueryOperator.Equal, "Sales")
                .AddCondition("Experience", QueryOperator.GreaterThan, 5)
                .Build();

            // Act
            var result = _persistenceService.ConvertToStorageFormat(matrix);

            // Assert
            Assert.That(result, Is.EqualTo("[_or][Department_eq_Sales][Experience_gt_5]"));
        }

        [Test]
        public void ConvertToStorageFormat_NestedConditions_ReturnsCorrectFormat()
        {
            // Arrange
            var matrix = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Department", QueryOperator.Equal, "Sales")
                .AddNestedConditions(nested => nested
                    .WithLogicalOperator(QueryOperator.Or)
                    .AddCondition("Experience", QueryOperator.GreaterThan, 5)
                    .AddCondition("Title", QueryOperator.Equal, "Manager"))
                .Build();

            // Act
            var result = _persistenceService.ConvertToStorageFormat(matrix);

            // Assert
            Assert.That(result, Is.EqualTo("[_and][Department_eq_Sales][_or][Experience_gt_5][Title_eq_Manager]"));
        }

        [Test]
        public void ConvertToStorageFormat_HandlesSpacesCorrectly()
        {
            // Arrange
            var matrix = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Title", QueryOperator.Equal, "Senior Manager")
                .Build();

            // Act
            var result = _persistenceService.ConvertToStorageFormat(matrix);

            // Assert
            Assert.That(result, Is.EqualTo("[_and][Title_eq_Senior~Manager]"));
        }

        [Test]
        public void ParseFromStorageFormat_SimpleCondition_ReturnsCorrectMatrix()
        {
            // Arrange
            var storageFormat = "[_and][Department_eq_Sales]";

            // Act
            var result = _persistenceService.ParseFromStorageFormat(storageFormat);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.LogicalOperator, Is.EqualTo(QueryOperator.And));
                Assert.That(result.Conditions, Has.Count.EqualTo(1));

                var condition = result.Conditions.Single();
                Assert.That(condition.Field, Is.EqualTo("Department"));
                Assert.That(condition.Operator, Is.EqualTo(QueryOperator.Equal));
                Assert.That(condition.Value.Value, Is.EqualTo("Sales"));
            });
        }

        [Test]
        public void ParseFromStorageFormat_HandlesSpacesCorrectly()
        {
            // Arrange
            var storageFormat = "[_and][Title_eq_Senior~Manager]";

            // Act
            var result = _persistenceService.ParseFromStorageFormat(storageFormat);

            // Assert
            var condition = result.Conditions.Single();
            Assert.That(condition.Value.Value, Is.EqualTo("Senior Manager"));
        }

        [Test]
        public void ParseFromStorageFormat_ComplexNestedQuery_ReturnsCorrectMatrix()
        {
            // Arrange
            var storageFormat = "[_and][Department_eq_Sales][_or][Experience_gt_5][Title_eq_Manager]";

            // Act
            var result = _persistenceService.ParseFromStorageFormat(storageFormat);

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
        public void ParseFromStorageFormat_InvalidFormat_ThrowsArgumentException()
        {
            // Arrange
            var invalidFormat = "invalid_format";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _persistenceService.ParseFromStorageFormat(invalidFormat));
        }

        [Test]
        public void ParseFromStorageFormat_NullOrEmpty_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => _persistenceService.ParseFromStorageFormat(null));
                Assert.Throws<ArgumentException>(() => _persistenceService.ParseFromStorageFormat(string.Empty));
            });
        }

        [Test]
        public void ConvertToStorageFormat_MetadataCondition_ReturnsCorrectFormat()
        {
            // Arrange
            var matrix = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Metadata.expense_limit", QueryOperator.GreaterThan, "5000")
                .Build();

            // Act
            var result = _persistenceService.ConvertToStorageFormat(matrix);

            // Assert
            Assert.That(result, Is.EqualTo("[_and][Metadata.expense_limit_gt_5000]"));
        }

        [Test]
        public void ParseFromStorageFormat_MetadataCondition_ReturnsCorrectMatrix()
        {
            // Arrange
            var storageFormat = "[_and][Metadata.expenseLimit_gt_5000]";

            // Act
            var result = _persistenceService.ParseFromStorageFormat(storageFormat);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.LogicalOperator, Is.EqualTo(QueryOperator.And));
                Assert.That(result.Conditions, Has.Count.EqualTo(1));

                var condition = result.Conditions.Single();
                Assert.That(condition.Field, Is.EqualTo("Metadata.expenseLimit"));
                Assert.That(condition.Operator, Is.EqualTo(QueryOperator.GreaterThan));
                Assert.That(condition.Value.Value, Is.EqualTo("5000"));
            });
        }

        [Test]
        public void ConvertToStorageFormat_CombinedMetadataAndEntityFields_ReturnsCorrectFormat()
        {
            // Arrange
            var matrix = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Department", QueryOperator.Equal, "Sales")
                .AddCondition("Metadata.location", QueryOperator.Equal, "NY")
                .Build();

            // Act
            var result = _persistenceService.ConvertToStorageFormat(matrix);

            // Assert
            Assert.That(result, Is.EqualTo("[_and][Department_eq_Sales][Metadata.location_eq_NY]"));
        }

        [Test]
        public void ConvertToStorageFormat_MetadataWithArrayValue_ReturnsCorrectFormat()
        {
            // Arrange
            var matrix = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Metadata.location", QueryOperator.In, new[] { "NY", "CA", "TX" })
                .Build();

            // Act
            var result = _persistenceService.ConvertToStorageFormat(matrix);

            // Assert
            Assert.That(result, Is.EqualTo("[_and][Metadata.location_in_NY|CA|TX]"));
        }

        [Test]
        public void ParseFromStorageFormat_ComplexMetadataQuery_ReturnsCorrectMatrix()
        {
            // Arrange
            var storageFormat = "[_and][Department_eq_Sales][_or][Metadata.expenseLimit_gt_5000][Metadata.locationCode_eq_NY]";

            // Act
            var result = _persistenceService.ParseFromStorageFormat(storageFormat);

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
    }
}
