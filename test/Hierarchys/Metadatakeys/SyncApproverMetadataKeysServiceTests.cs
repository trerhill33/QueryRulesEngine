using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using NUnit.Framework;
using QueryRulesEngine.Features.MetadataKeys.SyncApproverMetadataKeys;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys.Metadatakeys;

[TestFixture]
public class SyncApproverMetadataKeysServiceTests
{
    private static class TestData
    {
        public const int ValidHierarchyId = 1;
        public const int InvalidHierarchyId = 99;
        public static readonly string[] ApproverIds = { "123", "456" };

        public static readonly string[] DefinedMetadataKeys =
        {
            "ApproverMetadataKey.ExpenseLimit",
            "ApproverMetadataKey.Department"
        };

        public static readonly string[] ObsoleteKeys =
        {
            "ApproverMetadataKey.OldKey"
        };

        public static List<Metadata> GetExistingMetadata() => new()
        {
            new Metadata { HierarchyId = ValidHierarchyId, ApproverId = "123", Key = "ApproverMetadataKey.ExpenseLimit", Value = null },
            new Metadata { HierarchyId = ValidHierarchyId, ApproverId = "123", Key = "ApproverMetadataKey.OldKey", Value = null },
            new Metadata { HierarchyId = ValidHierarchyId, ApproverId = "456", Key = "ApproverMetadataKey.OldKey", Value = null }
        };

        public static List<Metadata> GetSyncedMetadata() => new()
        {
            new Metadata { HierarchyId = ValidHierarchyId, ApproverId = "123", Key = "ApproverMetadataKey.ExpenseLimit", Value = null }
        };
    }

    private IApproverMetadataRepository _approverMetadataRepository;
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IUnitOfWork<int> _unitOfWork;
    private IValidator<SyncApproverMetadataKeysRequest> _validator;
    private IWriteRepositoryAsync<Metadata, int> _metadataRepo;
    private SyncApproverMetadataKeysService _service;
    private List<Metadata> _addedMetadata;
    private List<Metadata> _deletedMetadata;
    private static readonly string[] sourceArray = new[] { "123" };

    [SetUp]
    public void Setup()
    {
        _approverMetadataRepository = A.Fake<IApproverMetadataRepository>();
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _unitOfWork = A.Fake<IUnitOfWork<int>>();
        _validator = A.Fake<IValidator<SyncApproverMetadataKeysRequest>>();
        _metadataRepo = A.Fake<IWriteRepositoryAsync<Metadata, int>>();
        _addedMetadata = [];
        _deletedMetadata = [];

        A.CallTo(() => _unitOfWork.Repository<Metadata>()).Returns(_metadataRepo);
        A.CallTo(() => _metadataRepo.AddAsync(A<Metadata>._, A<CancellationToken>._))
            .Invokes((Metadata m, CancellationToken _) => _addedMetadata.Add(m))
            .ReturnsLazily((Metadata m, CancellationToken _) => Task.FromResult(m));
        A.CallTo(() => _metadataRepo.DeleteAsync(A<Metadata>._))
            .Invokes((Metadata m) => _deletedMetadata.Add(m))
            .Returns(Task.CompletedTask);
        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .Invokes((Func<Task> operation, CancellationToken _) => operation());

        _service = new SyncApproverMetadataKeysService(
            _approverMetadataRepository,
            _readOnlyRepository,
            _unitOfWork,
            _validator);
    }

    private void SetupSuccessScenario()
    {
        A.CallTo(() => _validator.ValidateAsync(A<SyncApproverMetadataKeysRequest>._, A<CancellationToken>._))
            .Returns(new ValidationResult());
        A.CallTo(() => _approverMetadataRepository.GetApproverMetadataKeysAsync(
            TestData.ValidHierarchyId, A<CancellationToken>._))
            .Returns([.. TestData.DefinedMetadataKeys]);
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<Approver, string>(
            A<Expression<Func<Approver, bool>>>._, A<Expression<Func<Approver, string>>>._, A<CancellationToken>._))
            .Returns([.. TestData.ApproverIds]);
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync<Metadata>(
            A<Expression<Func<Metadata, bool>>>._, A<CancellationToken>._))
            .Returns(TestData.GetExistingMetadata());
    }

    [Test]
    public async Task ExecuteAsync_AddAndRemoveMetadataRecords_Success()
    {
        // Arrange
        var request = new SyncApproverMetadataKeysRequest(TestData.ValidHierarchyId);
        SetupSuccessScenario();

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.MetadataRecordsAdded, Is.EqualTo(3));
            Assert.That(result.Data.MetadataRecordsRemoved, Is.EqualTo(2));
            Assert.That(_addedMetadata, Has.Count.EqualTo(3));
            Assert.That(_deletedMetadata.All(m => m.Key == "ApproverMetadataKey.OldKey"), Is.True);
        });
    }

    [Test]
    public async Task ExecuteAsync_NoChangesNeeded_Success()
    {
        // Arrange
        var request = new SyncApproverMetadataKeysRequest(TestData.ValidHierarchyId);

        A.CallTo(() => _validator.ValidateAsync(A<SyncApproverMetadataKeysRequest>._, A<CancellationToken>._))
            .Returns(new ValidationResult());
        A.CallTo(() => _approverMetadataRepository.GetApproverMetadataKeysAsync(
            TestData.ValidHierarchyId, A<CancellationToken>._))
            .Returns(["ApproverMetadataKey.ExpenseLimit"]);
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<Approver, string>(
            A<Expression<Func<Approver, bool>>>._, A<Expression<Func<Approver, string>>>._, A<CancellationToken>._))
            .Returns([.. sourceArray]);
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync<Metadata>(
            A<Expression<Func<Metadata, bool>>>._, A<CancellationToken>._))
            .Returns(TestData.GetSyncedMetadata());

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.MetadataRecordsAdded, Is.EqualTo(0));
            Assert.That(result.Data.MetadataRecordsRemoved, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task ExecuteAsync_InvalidHierarchy_ReturnsError()
    {
        // Arrange
        var request = new SyncApproverMetadataKeysRequest(TestData.InvalidHierarchyId);

        A.CallTo(() => _validator.ValidateAsync(A<SyncApproverMetadataKeysRequest>._, A<CancellationToken>._))
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("HierarchyId", "Hierarchy does not exist.")
            }));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Contains.Item("Hierarchy does not exist."));
        });
    }
}
