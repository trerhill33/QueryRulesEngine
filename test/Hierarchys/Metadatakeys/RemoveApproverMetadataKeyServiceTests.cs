using FluentValidation;
using FluentValidation.Results;
using Moq;
using QueryRulesEngine.Features.MetadataKeys.RemoveApproverMetadataKey;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys.Metadatakeys;

[TestFixture]
public class RemoveApproverMetadataKeyServiceTests(
    Mock<IApproverMetadataRepository> approverMetadataRepositoryMock = null,
    Mock<IReadOnlyRepositoryAsync<int>> readOnlyRepositoryMock = null,
    Mock<IUnitOfWork<int>> unitOfWorkMock = null,
    Mock<IValidator<RemoveApproverMetadataKeyRequest>> validatorMock = null)
{
    private readonly Mock<IApproverMetadataRepository> _approverMetadataRepositoryMock = approverMetadataRepositoryMock ?? new();
    private readonly Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock = readOnlyRepositoryMock ?? new();
    private readonly Mock<IUnitOfWork<int>> _unitOfWorkMock = unitOfWorkMock ?? new();
    private readonly Mock<IValidator<RemoveApproverMetadataKeyRequest>> _validatorMock = validatorMock ?? new();
    private readonly Mock<IWriteRepositoryAsync<Metadata, int>> _metadataRepoMock = new();
    private readonly Mock<IWriteRepositoryAsync<MetadataKey, int>> _metadataKeyRepoMock = new();
    private readonly RemoveApproverMetadataKeyService _service;

    public RemoveApproverMetadataKeyServiceTests() : this(new(), new(), new(), new())
    {
        _unitOfWorkMock
            .Setup(x => x.Repository<Metadata>())
            .Returns(_metadataRepoMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Repository<MetadataKey>())
            .Returns(_metadataKeyRepoMock.Object);

        _service = new RemoveApproverMetadataKeyService(
            _approverMetadataRepositoryMock.Object,
            _readOnlyRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object);
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

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RemoveApproverMetadataKeyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _readOnlyRepositoryMock
            .Setup(r => r.FindByPredicateAsync(
                It.IsAny<Expression<Func<MetadataKey, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadataKey);

        _readOnlyRepositoryMock
            .Setup(r => r.FindAllByPredicateAsync(
                It.IsAny<Expression<Func<Metadata, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMetadataRecords);

        var deletedMetadataRecords = new List<Metadata>();
        _metadataRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<Metadata>()))
            .Callback<Metadata>(m => deletedMetadataRecords.Add(m))
            .Returns(Task.CompletedTask);

        var deletedMetadataKey = false;
        _metadataKeyRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<MetadataKey>()))
            .Callback<MetadataKey>(_ => deletedMetadataKey = true)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Func<Task>, CancellationToken>(async (operation, _) => await operation());

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

        _unitOfWorkMock.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_KeyDoesNotExist_ReturnsError()
    {
        // Arrange
        var request = new RemoveApproverMetadataKeyRequest(
            HierarchyId: 1,
            KeyName: "NonExistentKey"
        );

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RemoveApproverMetadataKeyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
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

        _unitOfWorkMock.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_HierarchyDoesNotExist_ReturnsError()
    {
        // Arrange
        var request = new RemoveApproverMetadataKeyRequest(
            HierarchyId: 99,
            KeyName: "SomeKey"
        );

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RemoveApproverMetadataKeyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
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

        _unitOfWorkMock.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
