using FakeItEasy;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Persistence;
using QueryRulesEngine.QueryEngine.Processors;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests;

[TestFixture]
public class MetadataQueryProcessorTests
{
    private IMetadataQueryProcessor _metadataQueryProcessor;
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IQueryPersistenceService _persistenceService;
    private List<Employee> _testData;

    [SetUp]
    public void Setup()
    {
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _persistenceService = new QueryPersistenceService();
        _metadataQueryProcessor = new MetadataQueryProcessor();

        // Setup test data
        _testData =
        [
            new()
        {
            TMID = "TM001",
            Name = "John Doe",
            ReportsTo = "TM003",
            Approvers =
            [
                new()
                {
                    HierarchyId = 1,
                    ApproverId = "TM001",
                    Metadata =
                    [
                        new() { HierarchyId = 1, ApproverId = "TM001", Key = "FinancialLimit", Value = "5000" },
                        new() { HierarchyId = 1, ApproverId = "TM001", Key = "BackupFor", Value = "TM002" }
                    ]
                }
            ]
        },
        new()
        {
            TMID = "TM002",
            Name = "Jane Smith",
            ReportsTo = "TM001",
            Approvers =
            [
                new()
                {
                    HierarchyId = 1,
                    ApproverId = "TM002",
                    Metadata =
                    [
                        new() { HierarchyId = 1, ApproverId = "TM002", Key = "FinancialLimit", Value = "25000" }
                    ]
                }
            ]
        },
        new()
        {
            TMID = "TM003",
            Name = "Alice Manager",
            ReportsTo = null,
            Approvers =
            [
                new()
                {
                    HierarchyId = 1,
                    ApproverId = "TM003",
                    Metadata =
                    [
                        new() { HierarchyId = 1, ApproverId = "TM003", Key = "FinancialLimit", Value = "50000" }
                    ]
                }
            ]
        }
        ];

        // Setup repository to actually filter the data
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateIncludeAsync(
            A<Expression<Func<Employee, bool>>>._,
            A<Expression<Func<Employee, object>>[]>._,
            A<CancellationToken>._))
            .ReturnsLazily((Expression<Func<Employee, bool>> predicate, Expression<Func<Employee, object>>[] includes, CancellationToken token) =>
            {
                var compiledPredicate = predicate.Compile();
                return _testData.Where(compiledPredicate).ToList();
            });
    }

    [Test]
    public async Task PrimaryApproverWithFinancialLimit_ReturnsMatchingRecords()
    {
        // Arrange
        var storedQuery = "level.1.rule.1.query:[_and][Employee.TMID_eq_TM002][ApproverMetadataKey.FinancialLimit_gte_25000]";
        var queryMatrix = _persistenceService.ParseFromStorageFormat(storedQuery);
        var expression = _metadataQueryProcessor.BuildExpressionFromQueryMatrix<Employee>(queryMatrix);

        // Act
        var result = await _readOnlyRepository.FindAllByPredicateIncludeAsync(
            expression,
            _metadataQueryProcessor.GetRequiredIncludes(),
            default);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].TMID, Is.EqualTo("TM002"));
    }

    [Test]
    public async Task BackupApproverCheck_ReturnsMatchingRecords()
    {
        // Arrange
        var storedQuery = "level.1.rule.1.query:[_and][ApproverMetadataKey.BackupFor_eq_TM002]";
        var queryMatrix = _persistenceService.ParseFromStorageFormat(storedQuery);
        var expression = _metadataQueryProcessor.BuildExpressionFromQueryMatrix<Employee>(queryMatrix);

        // Act
        var result = await _readOnlyRepository.FindAllByPredicateIncludeAsync(
            expression,
            _metadataQueryProcessor.GetRequiredIncludes(),
            default);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].TMID, Is.EqualTo("TM001"));
    }

    [Test]
    public async Task ContextWithPropertyNavigation_ReturnsMatchingRecords()
    {
        // Arrange
        var storedQuery = "level.1.rule.1.query:[_and][Employee.ReportsTo_eq_@Context.RequestedByTMID]";
        var queryMatrix = _persistenceService.ParseFromStorageFormat(storedQuery);
        var expression = _metadataQueryProcessor.BuildExpressionFromQueryMatrix<Employee>(queryMatrix, "TM003");

        // Act
        var result = await _readOnlyRepository.FindAllByPredicateIncludeAsync(
            expression,
            _metadataQueryProcessor.GetRequiredIncludes(),
            default);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].TMID, Is.EqualTo("TM001")); // TM001 reports to TM003
    }

    [Test]
    public async Task ComplexNestedConditions_ReturnsMatchingRecords()
    {
        // Arrange
        var storedQuery = @"level.1.rule.1.query:[_or][_and][Employee.TMID_eq_TM002][ApproverMetadataKey.FinancialLimit_gte_25000][_and][ApproverMetadataKey.BackupFor_eq_TM002]";
        var queryMatrix = _persistenceService.ParseFromStorageFormat(storedQuery);
        var expression = _metadataQueryProcessor.BuildExpressionFromQueryMatrix<Employee>(queryMatrix);

        // Act
        var result = await _readOnlyRepository.FindAllByPredicateIncludeAsync(
            expression,
            _metadataQueryProcessor.GetRequiredIncludes(),
            default);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Select(e => e.TMID), Does.Contain("TM001")); // Backup approver
        Assert.That(result.Select(e => e.TMID), Does.Contain("TM002")); // Primary approver
    }

    [Test]
    public async Task NoMatchingRecords_ReturnsEmptyList()
    {
        // Arrange
        var storedQuery = "level.1.rule.1.query:[_and][Employee.TMID_eq_TM999]"; // Non-existent TMID
        var queryMatrix = _persistenceService.ParseFromStorageFormat(storedQuery);
        var expression = _metadataQueryProcessor.BuildExpressionFromQueryMatrix<Employee>(queryMatrix);

        // Act
        var result = await _readOnlyRepository.FindAllByPredicateIncludeAsync(
            expression,
            _metadataQueryProcessor.GetRequiredIncludes(),
            default);

        // Assert
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task HighFinancialLimit_ReturnsOnlyQualifiedApprovers()
    {
        // Arrange
        var storedQuery = "level.1.rule.1.query:[_and][ApproverMetadataKey.FinancialLimit_gte_40000]";
        var queryMatrix = _persistenceService.ParseFromStorageFormat(storedQuery);
        var expression = _metadataQueryProcessor.BuildExpressionFromQueryMatrix<Employee>(queryMatrix);

        // Act
        var result = await _readOnlyRepository.FindAllByPredicateIncludeAsync(
            expression,
            _metadataQueryProcessor.GetRequiredIncludes(),
            default);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].TMID, Is.EqualTo("TM003")); // Only TM003 has limit > 40000
    }
}