using FluentValidation;
using FluentValidation.Results;
using Moq;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Hierarchys.MetadataKeys.AddApproverMetadataKey;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys.Metadatakeys;

[TestFixture]
public class AddApproverMetadataKeyServiceTests(
    Mock<IApproverMetadataRepository> approverMetadataRepositoryMock = null,
    Mock<IReadOnlyRepositoryAsync<int>> readOnlyRepositoryMock = null,
    Mock<IUnitOfWork<int>> unitOfWorkMock = null,
    Mock<IValidator<AddApproverMetadataKeyRequest>> validatorMock = null)
{
    private readonly Mock<IApproverMetadataRepository> _approverMetadataRepositoryMock = approverMetadataRepositoryMock ?? new();
    private readonly Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock = readOnlyRepositoryMock ?? new();
    private readonly Mock<IUnitOfWork<int>> _unitOfWorkMock = unitOfWorkMock ?? new();
    private readonly Mock<IValidator<AddApproverMetadataKeyRequest>> _validatorMock = validatorMock ?? new();
    private readonly Mock<IWriteRepositoryAsync<Metadata, int>> _metadataRepoMock = new();
    private readonly AddApproverMetadataKeyService _service;

    public AddApproverMetadataKeyServiceTests() : this(new(), new(), new(), new())
    {
        _unitOfWorkMock
            .Setup(x => x.Repository<Metadata>())
            .Returns(_metadataRepoMock.Object);

        _service = new AddApproverMetadataKeyService(
            _approverMetadataRepositoryMock.Object,
            _readOnlyRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_ValidRequest_AddsMetadataKeyAndRecords()
    {
        // Arrange
        var request = new AddApproverMetadataKeyRequest
        {
            HierarchyId = 1,
            KeyName = "UniqueKey"
        };

        var existingApprovers = new List<string> { "123", "456" };

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<AddApproverMetadataKeyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _readOnlyRepositoryMock
            .Setup(r => r.FindAllByPredicateAndTransformAsync(
                It.IsAny<Expression<Func<Approver, bool>>>(),
                It.IsAny<Expression<Func<Approver, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingApprovers);

        var addedMetadata = new List<Metadata>();
        _metadataRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Metadata>(), It.IsAny<CancellationToken>()))
            .Callback<Metadata, CancellationToken>((m, _) => addedMetadata.Add(m))
            .ReturnsAsync((Metadata m, CancellationToken _) => m);

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
            Assert.That(result.Data.HierarchyId, Is.EqualTo(request.HierarchyId), "HierarchyId should match");
            Assert.That(result.Data.KeyName, Is.EqualTo($"ApproverMetadataKey.{request.KeyName}"), "KeyName should match");

            // Verify metadata records were created for each approver
            Assert.That(addedMetadata, Has.Count.EqualTo(existingApprovers.Count));
            foreach (var metadata in addedMetadata)
            {
                Assert.That(metadata.HierarchyId, Is.EqualTo(request.HierarchyId));
                Assert.That(metadata.Key, Is.EqualTo($"ApproverMetadataKey.{request.KeyName}"));
                Assert.That(metadata.Value, Is.Null);
                Assert.That(existingApprovers, Contains.Item(metadata.ApproverId));
            }
        });

        _approverMetadataRepositoryMock.Verify(
            r => r.CreateApproverMetadataKeyAsync(
                request.HierarchyId,
                request.KeyName,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Previous error test cases remain the same...
}