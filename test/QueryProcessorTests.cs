using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Builders;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Processors;

namespace QueryRulesEngine.Tests
{
    [TestFixture]
    public class QueryProcessorTests
    {
        private IQueryProcessor _queryProcessor;
        private List<Employee> _testData;
        private IQueryable<Employee> _source;

        [SetUp]
        public void Setup()
        {
            _queryProcessor = new QueryProcessor();

            // Setup test data
            _testData =
            [
                new() { TMID = "1", Name = "John Doe", Title = "Manager", JobCode = "MGR", Email = "john.doe@example.com" },
                new() { TMID = "2", Name = "Jane Smith", Title = "Developer", JobCode = "DEV", Email = "jane.smith@example.com" },
                new() { TMID = "3", Name = "Bob Wilson", Title = "Senior Manager", JobCode = "SRMGR", Email = "bob.wilson@example.com" },
                new() { TMID = "4", Name = "Alice Brown", Title = "Senior Developer", JobCode = "SRDEV", Email = "alice.brown@example.com" }
            ];

            _source = _testData.AsQueryable();
        }

        [Test]
        public void ApplyQuery_SimpleEquality_ReturnsMatchingRecords()
        {
            // Arrange
            var query = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Title", QueryOperator.Equal, "Manager")
                .Build();

            // Act
            var result = _queryProcessor.ApplyQuery(_source, query).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result.Single().Name, Is.EqualTo("John Doe"));
            });
        }

        [Test]
        public void ApplyQuery_ComplexConditions_ReturnsMatchingRecords()
        {
            // Arrange
            var query = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Title", QueryOperator.Like, "Senior")
                .AddNestedConditions(nested => nested
                    .WithLogicalOperator(QueryOperator.Or)
                    .AddCondition("JobCode", QueryOperator.Equal, "DEV")
                    .AddCondition("Email", QueryOperator.Like, "example"))
                .Build();

            // Act
            var result = _queryProcessor.ApplyQuery(_source, query).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result.All(e => e.Title.Contains("Senior")), Is.True);
            });
        }

        [Test]
        public void ApplyQuery_LikeOperator_ReturnsMatchingRecords()
        {
            // Arrange
            var query = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Title", QueryOperator.Like, "Senior")
                .Build();

            // Act
            var result = _queryProcessor.ApplyQuery(_source, query).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result.All(e => e.Title.Contains("Senior")), Is.True);
            });
        }

        [Test]
        public void ApplyQuery_MultipleAndConditions_ReturnsMatchingRecords()
        {
            // Arrange
            var query = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Title", QueryOperator.Like, "Manager")
                .AddCondition("JobCode", QueryOperator.Equal, "MGR")
                .Build();

            // Act
            var result = _queryProcessor.ApplyQuery(_source, query).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result.Single().Name, Is.EqualTo("John Doe"));
            });
        }

        [Test]
        public void ApplyQuery_OrConditions_ReturnsMatchingRecords()
        {
            // Arrange
            var query = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.Or)
                .AddCondition("Title", QueryOperator.Equal, "Manager")
                .AddCondition("JobCode", QueryOperator.Like, "DEV")
                .Build();

            // Act
            var result = _queryProcessor.ApplyQuery(_source, query).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(3));
            });
        }

        [Test]
        public void ApplyQuery_EmptyMatrix_ReturnsAllRecords()
        {
            // Arrange
            var query = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .Build();

            // Act
            var result = _queryProcessor.ApplyQuery(_source, query).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(_testData.Count));
        }
    }
}
