using QueryRulesEngine.Entities;
using QueryRulesEngine.QueryEngine.Builders;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Processors;

namespace QueryRulesEngine.Tests
{
    [TestFixture]
    public class MetadataQueryProcessorTests
    {
        private MetadataQueryProcessor _metadataQueryProcessor;
        private IQueryable<Employee> _source;
        private IQueryProcessor _baseProcessor = new QueryProcessor();

        [SetUp]
        public void Setup()
        {
            _metadataQueryProcessor = new MetadataQueryProcessor(_baseProcessor);

            // Setup test data
            var employees = new List<Employee>
            {
                new()
                {
                    TMID = "TM001",
                    Name = "John Doe",
                    Email = "john.doe@example.com",
                    Title = "Manager",
                    JobCode = "MGR",
                    Approvers =
                    [
                        new()
                        {
                            Metadata =
                            [
                                new() { Key = "location", Value = "NY" },
                                new() { Key = "expense_limit", Value = "5000" }
                            ]
                        }
                    ]
                },
                new()
                {
                    TMID = "TM002",
                    Name = "Jane Smith",
                    Email = "jane.smith@example.com",
                    Title = "Developer",
                    JobCode = "DEV",
                    Approvers =
                    [
                        new()
                        {
                            Metadata =
                            [
                                new() { Key = "location", Value = "CA" },
                                new() { Key = "expense_limit", Value = "10000" }
                            ]
                        }
                    ]
                }
            };

            _source = employees.AsQueryable();
        }

        [Test]
        public void ApplyQuery_MetadataCondition_ReturnsMatchingRecords()
        {
            // Arrange
            var query = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("Metadata.expense_limit", QueryOperator.GreaterThan, "7500")
                .Build();

            // Act
            var result = _metadataQueryProcessor.ApplyQuery(_source, query).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().Name, Is.EqualTo("Jane Smith"));
        }

        [Test]
        public void ApplyQuery_CombinedStaticAndMetadata_ReturnsMatchingRecords()
        {
            // Arrange
            var query = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.And)
                .AddCondition("TMID", QueryOperator.Equal, "TM001")
                .AddCondition("Metadata.location", QueryOperator.Equal, "NY")
                .Build();

            // Act
            var resultt = _metadataQueryProcessor.ApplyQuery(_source, query);
            var result = _metadataQueryProcessor.ApplyQuery(_source, query).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().Name, Is.EqualTo("John Doe"));
        }

        [Test]
        public void ApplyQuery_ComplexConditions_ReturnsMatchingRecords()
        {
            // Arrange
            var query = new QueryBuilder()
                .WithLogicalOperator(QueryOperator.Or)
                .AddNestedConditions(nested => nested
                    .WithLogicalOperator(QueryOperator.And)
                    .AddCondition("TMID", QueryOperator.Equal, "TM001")
                    .AddCondition("Metadata.expense_limit", QueryOperator.LessThan, "6000"))
                .AddNestedConditions(nested => nested
                    .WithLogicalOperator(QueryOperator.And)
                    .AddCondition("JobCode", QueryOperator.Equal, "DEV")
                    .AddCondition("Metadata.location", QueryOperator.Equal, "CA"))
                .Build();

            // Act
            var result = _metadataQueryProcessor.ApplyQuery(_source, query).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
        }
    }
}
