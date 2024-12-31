using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Features.MetadataKeys.RemoveApproverMetadataKey;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys.Metadatakeys;

[TestFixture]
public class RemoveApproverMetadataKeyServiceTests
{
    private IApproverMetadataRepository _approverMetadataRepository;
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IUnitOfWork<int> _unitOfWork;
    private IValidator<RemoveApproverMetadataKeyRequest> _validator;
    private IWriteRepositoryAsync<Metadata, int> _metadataRepo;
    private IWriteRepositoryAsync<MetadataKey, int> _metadataKeyRepo;
    private RemoveApproverMetadataKeyService _service;

    [SetUp]
    public void Setup()
    {
        // Initialize fakes
        _approverMetadataRepository = A.Fake<IApproverMetadataRepository>();
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _unitOfWork = A.Fake<IUnitOfWork<int>>();
        _validator = A.Fake<IValidator<RemoveApproverMetadataKeyRequest>>();
        _metadataRepo = A.Fake<IWriteRepositoryAsync<Metadata, int>>();
        _metadataKeyRepo = A.Fake<IWriteRepositoryAsync<MetadataKey, int>>();

        // Setup UnitOfWork repositories
        A.CallTo(() => _unitOfWork.Repository<Metadata>()).Returns(_metadataRepo);
        A.CallTo(() => _unitOfWork.Repository<MetadataKey>()).Returns(_metadataKeyRepo);

        _service = new RemoveApproverMetadataKeyService(
            _approverMetadataRepository,
            _readOnlyRepository,
            _unitOfWork,
            _validator);
    }

    [Test]
    public async Task ExecuteAsync_ValidRequest_RemovesKeyAndMetadataRecords()
    {
        // Arrange
        var request = new RemoveApproverMetadataKeyRequest(
            HierarchyId: 1,
            KeyName: "ExpenseLimit"
        );

        var metadataKey = new MetadataKey
        {
            Id = 1,
            HierarchyId = request.HierarchyId,
            KeyName = $"ApproverMetadataKey.{request.KeyName}"
        };

        var existingMetadataRecords = new List<Metadata>
        {
            new() { Id = 1, HierarchyId = 1, ApproverId = "123", Key = $"ApproverMetadataKey.{request.KeyName}" },
            new() { Id = 2, HierarchyId = 1, ApproverId = "456", Key = $"ApproverMetadataKey.{request.KeyName}" }
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _readOnlyRepository.FindByPredicateAsync(
            A<Expression<Func<MetadataKey, bool>>>._,
            A<CancellationToken>._))
            .Returns(metadataKey);

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync(
            A<Expression<Func<Metadata, bool>>>._,
            A<CancellationToken>._))
            .Returns(existingMetadataRecords);

        var deletedMetadataRecords = new List<Metadata>();
        A.CallTo(() => _metadataRepo.DeleteAsync(A<Metadata>._))
            .Invokes((Metadata metadata) => deletedMetadataRecords.Add(metadata))
            .Returns(Task.CompletedTask);

        var deletedMetadataKey = false;
        A.CallTo(() => _metadataKeyRepo.DeleteAsync(A<MetadataKey>._))
            .Invokes(() => deletedMetadataKey = true)
            .Returns(Task.CompletedTask);

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .Invokes(async (Func<Task> operation, CancellationToken _) => await operation());

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, "Operation should succeed");
            Assert.That(result.Data.HierarchyId, Is.EqualTo(request.HierarchyId));
            Assert.That(result.Data.KeyName, Is.EqualTo(request.KeyName));

            // Verify all metadata records were deleted
            Assert.That(deletedMetadataRecords, Has.Count.EqualTo(existingMetadataRecords.Count));
            Assert.That(deletedMetadataKey, Is.True, "Metadata key should be deleted");
        });

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExecuteAsync_KeyDoesNotExist_ReturnsError()
    {
        // Arrange
        var request = new RemoveApproverMetadataKeyRequest(
            HierarchyId: 1,
            KeyName: "NonExistentKey"
        );

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("KeyName", "Metadata key does not exist for this hierarchy.")
            }));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Is.Not.Null.And.Contains("Metadata key does not exist for this hierarchy."));
        });

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task ExecuteAsync_HierarchyDoesNotExist_ReturnsError()
    {
        // Arrange
        var request = new RemoveApproverMetadataKeyRequest(
            HierarchyId: 99,
            KeyName: "SomeKey"
        );

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
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
            Assert.That(result.Messages, Is.Not.Null.And.Contains("Hierarchy does not exist."));
        });

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
