using FluentValidation;
using FluentValidation.Results;
using Moq;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.Hierarchies.CreateHierarchy;
using QueryRulesEngine.Features.MetadataKeys.AddApproverMetadataKey;
using QueryRulesEngine.Features.MetadataKeys.SyncApproverMetadataKeys;
using QueryRulesEngine.Features.Rules.AddRuleToLevel;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Persistence;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys;

[TestFixture]
public class HierarchyWorkflowTests
{
    private static class TestData
    {
        public const int ValidHierarchyId = 1;
        public const string HierarchyName = "Manufacturing Approvals";
        public const string HierarchyDescription = "Manufacturing department approval workflow";

        // Initial set of metadata keys
        public static readonly string[] InitialMetadataKeys =
        {
            "ExpenseLimit",
            "Department",
            "Region"
        };

        // Additional keys to be added later
        public static readonly string[] AdditionalMetadataKeys =
        {
            "OverrideLevel",
            "ApprovalType"
        };

        // Keys for sync testing
        public static readonly string[] DefinedMetadataKeys =
        {
            "ApproverMetadataKey.ExpenseLimit",
            "ApproverMetadataKey.Department",
            "ApproverMetadataKey.Region",
            "ApproverMetadataKey.OverrideLevel",
            "ApproverMetadataKey.ApprovalType"
        };

        public static readonly string[] ApproverIds = { "123", "456" };

        public static Hierarchy GetCreatedHierarchy() => new()
        {
            Id = ValidHierarchyId,
            Name = HierarchyName,
            Description = HierarchyDescription
        };

        // Sample rules for testing
        public static QueryMatrix GetDepartmentRule() => new()
        {
            LogicalOperator = QueryOperator.And,
            Conditions =
            [
                new QueryCondition
                {
                    Field = "ApproverMetadataKey.Department",
                    Operator = QueryOperator.Equal,
                    Value = ConditionValue.Single("Manufacturing")
                }
            ]
        };

        public static QueryMatrix GetExpenseLimitRule() => new()
        {
            LogicalOperator = QueryOperator.And,
            Conditions =
            [
                new QueryCondition
                {
                    Field = "ApproverMetadataKey.ExpenseLimit",
                    Operator = QueryOperator.LessThanOrEqual,
                    Value = ConditionValue.Single("10000")
                }
            ]
        };
    }

    // Service fields
    private Mock<IHierarchyRepository> _hierarchyRepositoryMock;
    private Mock<ILevelRepository> _levelRepositoryMock;
    private Mock<IRuleRepository> _ruleRepositoryMock;
    private Mock<IApproverMetadataRepository> _approverMetadataRepositoryMock;
    private Mock<IQueryPersistenceService> _queryPersistenceServiceMock;
    private Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock;
    private Mock<IUnitOfWork<int>> _unitOfWorkMock;
    private CancellationToken _cancellationToken;

    // Services
    private ICreateHierarchyService _createHierarchyService;
    private AddApproverMetadataKeyService _addApproverMetadataKeyService;
    private AddRuleToLevelService _addRuleToLevelService;
    private SyncApproverMetadataKeysService _syncService;

    [SetUp]
    public void Setup()
    {
        InitializeMocks();
        SetupBasicMocks();
        CreateServices();
    }

    private void InitializeMocks()
    {
        _hierarchyRepositoryMock = new Mock<IHierarchyRepository>(MockBehavior.Strict);
        _levelRepositoryMock = new Mock<ILevelRepository>(MockBehavior.Strict);
        _ruleRepositoryMock = new Mock<IRuleRepository>(MockBehavior.Strict);
        _approverMetadataRepositoryMock = new Mock<IApproverMetadataRepository>(MockBehavior.Strict);
        _queryPersistenceServiceMock = new Mock<IQueryPersistenceService>(MockBehavior.Strict);
        _readOnlyRepositoryMock = new Mock<IReadOnlyRepositoryAsync<int>>(MockBehavior.Strict);
        _unitOfWorkMock = new Mock<IUnitOfWork<int>>(MockBehavior.Strict);
        _cancellationToken = CancellationToken.None;
    }

    private void SetupBasicMocks()
    {
        // Setup Write Repository for Metadata
        var metadataWriteRepoMock = new Mock<IWriteRepositoryAsync<Metadata, int>>(MockBehavior.Strict);
        metadataWriteRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Metadata>(), _cancellationToken))
            .ReturnsAsync((Metadata m, CancellationToken _) => m);

        metadataWriteRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Metadata>()))
            .Returns(Task.CompletedTask);

        metadataWriteRepoMock
            .Setup(x => x.DeleteAsync(It.IsAny<Metadata>()))
            .Returns(Task.CompletedTask);

        // Setup UnitOfWork
        _unitOfWorkMock
            .Setup(x => x.Repository<Metadata>())
            .Returns(metadataWriteRepoMock.Object);

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<Task>>(),
                _cancellationToken))
            .Callback<Func<Task>, CancellationToken>(async (operation, _) => await operation())
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(_cancellationToken))
            .ReturnsAsync(1);

        // Setup Hierarchy Repository
        _hierarchyRepositoryMock
            .Setup(x => x.IsUniqueHierarchyNameAsync(TestData.HierarchyName, _cancellationToken))
            .ReturnsAsync(true);

        _hierarchyRepositoryMock
            .Setup(x => x.HierarchyExistsAsync(TestData.ValidHierarchyId, _cancellationToken))
            .ReturnsAsync(true);

        _hierarchyRepositoryMock
            .Setup(x => x.CreateHierarchyAsync(
                TestData.HierarchyName,
                TestData.HierarchyDescription, It.IsAny<string>(),
                _cancellationToken))
            .ReturnsAsync(TestData.GetCreatedHierarchy());

        // Setup Level Repository
        _levelRepositoryMock
            .Setup(x => x.CreateDefaultLevelsAsync(TestData.ValidHierarchyId, _cancellationToken))
            .Returns(Task.CompletedTask);

        _levelRepositoryMock
            .Setup(x => x.LevelExistsAsync(
                TestData.ValidHierarchyId,
                It.IsAny<int>(),
                _cancellationToken))
            .ReturnsAsync(true);

        // Setup Approver Metadata Repository
        _approverMetadataRepositoryMock
            .Setup(x => x.IsUniqueKeyForHierarchyAsync(
                TestData.ValidHierarchyId,
                It.IsAny<string>(),
                _cancellationToken))
            .ReturnsAsync(true);

        _approverMetadataRepositoryMock
            .Setup(x => x.CreateApproverMetadataKeyAsync(
                TestData.ValidHierarchyId,
                It.IsAny<string>(),
                _cancellationToken))
            .Returns(Task.CompletedTask);

        _approverMetadataRepositoryMock
            .Setup(x => x.GetApproverMetadataKeysAsync(
                TestData.ValidHierarchyId,
                _cancellationToken))
            .ReturnsAsync(TestData.DefinedMetadataKeys.ToList());

        // Setup Read Only Repository
        _readOnlyRepositoryMock
            .Setup(x => x.FindAllByPredicateAndTransformAsync<Approver, string>(
                It.IsAny<Expression<Func<Approver, bool>>>(),
                It.IsAny<Expression<Func<Approver, string>>>(),
                _cancellationToken))
            .ReturnsAsync(TestData.ApproverIds.ToList());

        _readOnlyRepositoryMock
            .Setup(x => x.FindAllByPredicateAsync<Metadata>(
                It.IsAny<Expression<Func<Metadata, bool>>>(),
                _cancellationToken))
            .ReturnsAsync([]);

        // Setup Rule Repository
        _ruleRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<RuleDto>(), _cancellationToken))
            .ReturnsAsync(false);

        _ruleRepositoryMock
            .Setup(x => x.GetNextRuleNumberAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                _cancellationToken))
            .ReturnsAsync((int _, int level, CancellationToken _) => level + 1);

        _ruleRepositoryMock
            .Setup(x => x.CreateRuleAsync(It.IsAny<RuleDto>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        _ruleRepositoryMock
            .Setup(x => x.GetExistingMetadataKeysAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                _cancellationToken))
            .ReturnsAsync(TestData.DefinedMetadataKeys.ToList());
    }

    private void CreateServices()
    {
        var createHierarchyValidator = new CreateHierarchyValidator(_hierarchyRepositoryMock.Object);
        var addMetadataKeyValidator = new AddApproverMetadataKeyValidator(
            _hierarchyRepositoryMock.Object,
            _approverMetadataRepositoryMock.Object);
        var addRuleValidatorMock = new Mock<IValidator<AddRuleToLevelRequest>>();

        addRuleValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<AddRuleToLevelRequest>(), _cancellationToken))
            .ReturnsAsync(new ValidationResult());

        _createHierarchyService = new CreateHierarchyService(
            _hierarchyRepositoryMock.Object,
            _levelRepositoryMock.Object,
            createHierarchyValidator);

        _addApproverMetadataKeyService = new AddApproverMetadataKeyService(
            _approverMetadataRepositoryMock.Object,
            _readOnlyRepositoryMock.Object,
            _unitOfWorkMock.Object,
            addMetadataKeyValidator);

        _addRuleToLevelService = new AddRuleToLevelService(
            _ruleRepositoryMock.Object,
            _levelRepositoryMock.Object,
            addRuleValidatorMock.Object);

        var syncValidatorMock = new Mock<IValidator<SyncApproverMetadataKeysRequest>>();
        syncValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<SyncApproverMetadataKeysRequest>(), _cancellationToken))
            .ReturnsAsync(new ValidationResult());

        _syncService = new SyncApproverMetadataKeysService(
            _approverMetadataRepositoryMock.Object,
            _readOnlyRepositoryMock.Object,
            _unitOfWorkMock.Object,
            syncValidatorMock.Object);
    }

    [Test]
    public async Task CreateHierarchy_WithInitialMetadataKeys_Success()
    {
        // Arrange
        var createRequest = new CreateHierarchyRequest
        {
            Name = TestData.HierarchyName,
            Description = TestData.HierarchyDescription
        };

        // Act
        // Step 1: Create hierarchy
        var createResult = await _createHierarchyService.ExecuteAsync(createRequest);

        // Step 2: Add initial metadata keys
        var addKeyResults = await AddMetadataKeys(TestData.InitialMetadataKeys);

        // Assert
        Assert.Multiple(() =>
        {
            // Verify hierarchy creation
            Assert.That(createResult.Succeeded, Is.True, "Hierarchy creation should succeed");
            Assert.That(createResult.Data.Id, Is.EqualTo(TestData.ValidHierarchyId));

            // Verify metadata key addition
            Assert.That(addKeyResults, Has.All.Matches<Result>(r => r.Succeeded),
                "All metadata keys should be added successfully");
        });

        VerifyBasicOperations(TestData.InitialMetadataKeys.Length);
    }

    [Test]
    public async Task CreateHierarchy_AddMetadataKeysIncrementally_Success()
    {
        // Create hierarchy with initial keys
        var createResult = await _createHierarchyService.ExecuteAsync(new CreateHierarchyRequest
        {
            Name = TestData.HierarchyName,
            Description = TestData.HierarchyDescription
        });

        var initialKeyResults = await AddMetadataKeys(TestData.InitialMetadataKeys);

        // Add additional keys later
        var additionalKeyResults = await AddMetadataKeys(TestData.AdditionalMetadataKeys);

        Assert.Multiple(() =>
        {
            Assert.That(createResult.Succeeded, Is.True);
            Assert.That(initialKeyResults, Has.All.Matches<Result>(r => r.Succeeded));
            Assert.That(additionalKeyResults, Has.All.Matches<Result>(r => r.Succeeded));
        });

        VerifyBasicOperations(
            TestData.InitialMetadataKeys.Length +
            TestData.AdditionalMetadataKeys.Length);
    }

    [Test]
    public async Task CreateHierarchy_WithMetadataKeysAndRules_Success()
    {
        // Create hierarchy and add all metadata keys
        var createResult = await _createHierarchyService.ExecuteAsync(new CreateHierarchyRequest
        {
            Name = TestData.HierarchyName,
            Description = TestData.HierarchyDescription
        });

        var allKeys = TestData.InitialMetadataKeys.Concat(TestData.AdditionalMetadataKeys);
        var keyResults = await AddMetadataKeys(allKeys);

        // Add rules
        var ruleResults = await Task.WhenAll(
            CreateAndAddRule(TestData.ValidHierarchyId, 1, TestData.GetDepartmentRule(), 1),
            CreateAndAddRule(TestData.ValidHierarchyId, 1, TestData.GetExpenseLimitRule(), 2)
        );

        Assert.Multiple(() =>
        {
            Assert.That(createResult.Succeeded, Is.True);
            Assert.That(keyResults, Has.All.Matches<Result>(r => r.Succeeded));
            Assert.That(ruleResults, Has.All.Matches<Result>(r => r.Succeeded));
        });

        VerifyRuleOperations();
    }

    [Test]
    public async Task CreateHierarchy_WithMetadataKeysAndSync_Success()
    {
        // Setup sync-specific mocks
        _approverMetadataRepositoryMock
            .Setup(x => x.GetApproverMetadataKeysAsync(TestData.ValidHierarchyId, _cancellationToken))
            .ReturnsAsync(TestData.DefinedMetadataKeys.ToList());

        _readOnlyRepositoryMock
            .Setup(r => r.FindAllByPredicateAndTransformAsync<Approver, string>(
                It.IsAny<Expression<Func<Approver, bool>>>(),
                It.IsAny<Expression<Func<Approver, string>>>(),
                _cancellationToken))
            .ReturnsAsync(TestData.ApproverIds.ToList());

        // Create hierarchy with partial keys
        var createResult = await _createHierarchyService.ExecuteAsync(new CreateHierarchyRequest
        {
            Name = TestData.HierarchyName,
            Description = TestData.HierarchyDescription
        });

        var keyResults = await AddMetadataKeys(TestData.InitialMetadataKeys);

        // Sync metadata keys
        var syncResult = await _syncService.ExecuteAsync(
            new SyncApproverMetadataKeysRequest(TestData.ValidHierarchyId));

        Assert.Multiple(() =>
        {
            Assert.That(createResult.Succeeded, Is.True);
            Assert.That(keyResults, Has.All.Matches<Result>(r => r.Succeeded));
            Assert.That(syncResult.Succeeded, Is.True);
            Assert.That(syncResult.Data.MetadataRecordsAdded, Is.GreaterThan(0),
                "Sync should add missing metadata records");
        });

        VerifySyncOperations();
    }

    private async Task<IEnumerable<Result>> AddMetadataKeys(IEnumerable<string> keys)
    {
        var results = new List<Result>();
        foreach (var key in keys)
        {
            var result = await _addApproverMetadataKeyService.ExecuteAsync(
                new AddApproverMetadataKeyRequest
                {
                    HierarchyId = TestData.ValidHierarchyId,
                    KeyName = key
                });
            results.Add(result);
        }
        return results;
    }

    private async Task<Result> CreateAndAddRule(
        int hierarchyId,
        int level,
        QueryMatrix queryMatrix,
        int ruleNumber)
    {
        if (queryMatrix?.Conditions == null || !queryMatrix.Conditions.Any())
        {
            throw new ArgumentException("QueryMatrix must have conditions", nameof(queryMatrix));
        }

        return await _addRuleToLevelService.ExecuteAsync(new AddRuleToLevelRequest
        {
            HierarchyId = hierarchyId,
            LevelNumber = level,
            RuleNumber = ruleNumber.ToString(),
            QueryMatrix = queryMatrix
        });
    }

    private void VerifyBasicOperations(int expectedKeyCount)
    {
        _hierarchyRepositoryMock.Verify(
            x => x.CreateHierarchyAsync(
                TestData.HierarchyName,
                TestData.HierarchyDescription, It.IsAny<string>(),
                _cancellationToken),
            Times.Once);

        // Add verification for default levels creation
        _levelRepositoryMock.Verify(
            x => x.CreateDefaultLevelsAsync(
                TestData.ValidHierarchyId,
                _cancellationToken),
            Times.Once,
            "Default levels should be created exactly once");

        _approverMetadataRepositoryMock.Verify(
            x => x.CreateApproverMetadataKeyAsync(
                TestData.ValidHierarchyId,
                It.IsAny<string>(),
                _cancellationToken),
            Times.Exactly(expectedKeyCount));
    }


    private void VerifyRuleOperations()
    {
        _ruleRepositoryMock.Verify(
            x => x.CreateRuleAsync(
                It.IsAny<RuleDto>(),
                _cancellationToken),
            Times.Exactly(2));

        _ruleRepositoryMock.Verify(
            x => x.GetNextRuleNumberAsync(
                TestData.ValidHierarchyId,
                It.IsAny<int>(),
                _cancellationToken),
            Times.Exactly(2));
    }

    private void VerifySyncOperations()
    {
        // Verify that necessary repository methods were called during sync
        _approverMetadataRepositoryMock.Verify(
            x => x.GetApproverMetadataKeysAsync(
                TestData.ValidHierarchyId,
                _cancellationToken),
            Times.Once,
            "Should retrieve defined metadata keys once");

        _readOnlyRepositoryMock.Verify(
            r => r.FindAllByPredicateAndTransformAsync<Approver, string>(
                It.IsAny<Expression<Func<Approver, bool>>>(),
                It.IsAny<Expression<Func<Approver, string>>>(),
                _cancellationToken),
            Times.Once,
            "Should retrieve approver IDs once");

        // Verify metadata key creation attempts
        _approverMetadataRepositoryMock.Verify(
            x => x.CreateApproverMetadataKeyAsync(
                TestData.ValidHierarchyId,
                It.IsAny<string>(),
                _cancellationToken),
            Times.AtLeast(1),
            "Should create at least one missing metadata key");

        // Verify hierarchy existence check
        _hierarchyRepositoryMock.Verify(
            x => x.HierarchyExistsAsync(
                TestData.ValidHierarchyId,
                _cancellationToken),
            Times.AtLeast(1),
            "Should verify hierarchy exists");
    }
}