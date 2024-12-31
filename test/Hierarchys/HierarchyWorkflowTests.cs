using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.Hierarchies.CreateHierarchy;
using QueryRulesEngine.Features.MetadataKeys.AddApproverMetadataKey;
using QueryRulesEngine.Features.MetadataKeys.SyncApproverMetadataKeys;
using QueryRulesEngine.Features.Rules.AddRuleToLevel;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using QueryRulesEngine.QueryEngine.Common.Models;
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

        public static readonly string[] InitialMetadataKeys =
        {
            "ExpenseLimit",
            "Department",
            "Region"
        };

        public static readonly string[] AdditionalMetadataKeys =
        {
            "OverrideLevel",
            "ApprovalType"
        };

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

    private IHierarchyRepository _hierarchyRepository;
    private ILevelRepository _levelRepository;
    private IRuleRepository _ruleRepository;
    private IApproverMetadataRepository _approverMetadataRepository;
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IUnitOfWork<int> _unitOfWork;
    private ICreateHierarchyService _createHierarchyService;
    private AddApproverMetadataKeyService _addApproverMetadataKeyService;
    private AddRuleToLevelService _addRuleToLevelService;
    private SyncApproverMetadataKeysService _syncService;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _hierarchyRepository = A.Fake<IHierarchyRepository>();
        _levelRepository = A.Fake<ILevelRepository>();
        _ruleRepository = A.Fake<IRuleRepository>();
        _approverMetadataRepository = A.Fake<IApproverMetadataRepository>();
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _unitOfWork = A.Fake<IUnitOfWork<int>>();
        _cancellationToken = CancellationToken.None;

        SetupBasicFakes();
        CreateServices();
    }

    private void SetupBasicFakes()
    {
        var metadataRepo = A.Fake<IWriteRepositoryAsync<Metadata, int>>();
        A.CallTo(() => _unitOfWork.Repository<Metadata>()).Returns(metadataRepo);

        // UnitOfWork
        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, _cancellationToken))
            .Invokes(async (Func<Task> operation, CancellationToken _) => await operation());
        A.CallTo(() => _unitOfWork.CommitAsync(_cancellationToken)).Returns(1);

        // HierarchyRepository
        A.CallTo(() => _hierarchyRepository.IsUniqueHierarchyNameAsync(TestData.HierarchyName, _cancellationToken)).Returns(true);
        A.CallTo(() => _hierarchyRepository.HierarchyExistsAsync(TestData.ValidHierarchyId, _cancellationToken)).Returns(true);
        A.CallTo(() => _hierarchyRepository.CreateHierarchyAsync(TestData.HierarchyName, TestData.HierarchyDescription, A<string>._, _cancellationToken))
            .Returns(TestData.GetCreatedHierarchy());

        // LevelRepository
        A.CallTo(() => _levelRepository.CreateDefaultLevelsAsync(TestData.ValidHierarchyId, _cancellationToken)).Returns(Task.CompletedTask);
        A.CallTo(() => _levelRepository.LevelExistsAsync(TestData.ValidHierarchyId, A<int>._, _cancellationToken)).Returns(true);

        // ApproverMetadataRepository
        A.CallTo(() => _approverMetadataRepository.IsUniqueKeyForHierarchyAsync(TestData.ValidHierarchyId, A<string>._, _cancellationToken)).Returns(true);
        A.CallTo(() => _approverMetadataRepository.CreateApproverMetadataKeyAsync(TestData.ValidHierarchyId, A<string>._, _cancellationToken))
            .Returns(Task.CompletedTask);
        A.CallTo(() => _approverMetadataRepository.GetApproverMetadataKeysAsync(TestData.ValidHierarchyId, _cancellationToken))
            .Returns(TestData.DefinedMetadataKeys.ToList());

        // ReadOnlyRepository
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<Approver, string>(A<Expression<Func<Approver, bool>>>._, A<Expression<Func<Approver, string>>>._, _cancellationToken))
            .Returns(TestData.ApproverIds.ToList());
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync<Metadata>(A<Expression<Func<Metadata, bool>>>._, _cancellationToken)).Returns(new List<Metadata>());

        // RuleRepository
        A.CallTo(() => _ruleRepository.ExistsAsync(A<RuleDto>._, _cancellationToken)).Returns(false);
        A.CallTo(() => _ruleRepository.GetNextRuleNumberAsync(A<int>._, A<int>._, _cancellationToken)).ReturnsLazily((int _, int level, CancellationToken _) => level + 1);
        A.CallTo(() => _ruleRepository.CreateRuleAsync(A<RuleDto>._, _cancellationToken)).Returns(Task.CompletedTask);
        A.CallTo(() => _ruleRepository.GetExistingMetadataKeysAsync(A<int>._, A<IEnumerable<string>>._, _cancellationToken))
            .Returns(TestData.DefinedMetadataKeys.ToList());
    }

    private void CreateServices()
    {
        var createHierarchyValidator = new CreateHierarchyValidator(_hierarchyRepository);
        var addMetadataKeyValidator = new AddApproverMetadataKeyValidator(_hierarchyRepository, _approverMetadataRepository);
        var addRuleValidator = A.Fake<IValidator<AddRuleToLevelRequest>>();

        A.CallTo(() => addRuleValidator.ValidateAsync(A<AddRuleToLevelRequest>._, _cancellationToken))
            .Returns(new ValidationResult());

        _createHierarchyService = new CreateHierarchyService(_hierarchyRepository, _levelRepository, createHierarchyValidator);

        _addApproverMetadataKeyService = new AddApproverMetadataKeyService(
            _approverMetadataRepository,
            _readOnlyRepository,
            _unitOfWork,
            addMetadataKeyValidator);

        _addRuleToLevelService = new AddRuleToLevelService(
            _ruleRepository,
            _levelRepository,
            addRuleValidator);

        var syncValidator = A.Fake<IValidator<SyncApproverMetadataKeysRequest>>();
        A.CallTo(() => syncValidator.ValidateAsync(A<SyncApproverMetadataKeysRequest>._, _cancellationToken))
            .Returns(new ValidationResult());

        _syncService = new SyncApproverMetadataKeysService(
            _approverMetadataRepository,
            _readOnlyRepository,
            _unitOfWork,
            syncValidator);
    }

    private async Task<IEnumerable<Result>> AddMetadataKeys(IEnumerable<string> keys)
    {
        var results = new List<Result>();
        foreach (var key in keys)
        {
            var result = await _addApproverMetadataKeyService.ExecuteAsync(new AddApproverMetadataKeyRequest
            {
                HierarchyId = TestData.ValidHierarchyId,
                KeyName = key
            });
            results.Add(result);
        }
        return results;
    }

    private void VerifyBasicOperations(int expectedKeyCount)
    {
        A.CallTo(() => _hierarchyRepository.CreateHierarchyAsync(TestData.HierarchyName, TestData.HierarchyDescription, A<string>._, _cancellationToken))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _levelRepository.CreateDefaultLevelsAsync(TestData.ValidHierarchyId, _cancellationToken))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _approverMetadataRepository.CreateApproverMetadataKeyAsync(TestData.ValidHierarchyId, A<string>._, _cancellationToken))
            .MustHaveHappened(expectedKeyCount, Times.Exactly);
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
        var createResult = await _createHierarchyService.ExecuteAsync(createRequest);
        var addKeyResults = await AddMetadataKeys(TestData.InitialMetadataKeys);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(createResult.Succeeded, Is.True);
            Assert.That(createResult.Data.Id, Is.EqualTo(TestData.ValidHierarchyId));
            Assert.That(addKeyResults, Has.All.Matches<Result>(r => r.Succeeded));
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
    }


    [Test]
    public async Task CreateHierarchy_WithMetadataKeysAndSync_Success()
    {
        // Arrange: Setup sync-specific fakes
        A.CallTo(() => _approverMetadataRepository.GetApproverMetadataKeysAsync(TestData.ValidHierarchyId, _cancellationToken))
            .Returns([.. TestData.DefinedMetadataKeys]);

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<Approver, string>(
                A<Expression<Func<Approver, bool>>>._,
                A<Expression<Func<Approver, string>>>._,
                _cancellationToken))
            .Returns([.. TestData.ApproverIds]);

        // Act: Create hierarchy with partial keys
        var createResult = await _createHierarchyService.ExecuteAsync(new CreateHierarchyRequest
        {
            Name = TestData.HierarchyName,
            Description = TestData.HierarchyDescription
        });

        var keyResults = await AddMetadataKeys(TestData.InitialMetadataKeys);

        // Sync metadata keys
        var syncResult = await _syncService.ExecuteAsync(
            new SyncApproverMetadataKeysRequest(TestData.ValidHierarchyId));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(createResult.Succeeded, Is.True, "Hierarchy creation should succeed");
            Assert.That(keyResults, Has.All.Matches<Result>(r => r.Succeeded), "All initial metadata keys should be added successfully");
            Assert.That(syncResult.Succeeded, Is.True, "Metadata sync should succeed");
            Assert.That(syncResult.Data.MetadataRecordsAdded, Is.GreaterThan(0), "Sync should add missing metadata records");
        });

        // Adjusted expectation to match the exact number of calls
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<Approver, string>(
                A<Expression<Func<Approver, bool>>>._,
                A<Expression<Func<Approver, string>>>._,
                _cancellationToken))
            .MustHaveHappened(4, Times.Exactly);

        A.CallTo(() => _approverMetadataRepository.GetApproverMetadataKeysAsync(TestData.ValidHierarchyId, _cancellationToken))
            .MustHaveHappenedOnceExactly();
    }
}
