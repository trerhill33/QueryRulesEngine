using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using NUnit.Framework;
using QueryRulesEngine.Features.MetadataKeys.AddApproverMetadataKey;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys.Metadatakeys;

[TestFixture]
public class AddApproverMetadataKeyServiceTests
{
    private IApproverMetadataRepository _approverMetadataRepository;
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IUnitOfWork<int> _unitOfWork;
    private IValidator<AddApproverMetadataKeyRequest> _validator;
    private IWriteRepositoryAsync<Metadata, int> _metadataRepo;
    private AddApproverMetadataKeyService _service;

    [SetUp]
    public void Setup()
    {
        // Initialize fakes
        _approverMetadataRepository = A.Fake<IApproverMetadataRepository>();
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _unitOfWork = A.Fake<IUnitOfWork<int>>();
        _validator = A.Fake<IValidator<AddApproverMetadataKeyRequest>>();
        _metadataRepo = A.Fake<IWriteRepositoryAsync<Metadata, int>>();

        // Setup UnitOfWork repositories
        A.CallTo(() => _unitOfWork.Repository<Metadata>()).Returns(_metadataRepo);

        _service = new AddApproverMetadataKeyService(
            _approverMetadataRepository,
            _readOnlyRepository,
            _unitOfWork,
            _validator);
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

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync(
            A<Expression<Func<Approver, bool>>>._,
            A<Expression<Func<Approver, string>>>._,
            A<CancellationToken>._))
            .Returns(existingApprovers);

        var addedMetadata = new List<Metadata>();
        A.CallTo(() => _metadataRepo.AddAsync(A<Metadata>._, A<CancellationToken>._))
            .Invokes((Metadata metadata, CancellationToken _) => addedMetadata.Add(metadata))
            .ReturnsLazily((Metadata metadata, CancellationToken _) => Task.FromResult(metadata));

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .Invokes(async (Func<Task> operation, CancellationToken _) => await operation());

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

        // Verify repository calls
        A.CallTo(() => _approverMetadataRepository.CreateApproverMetadataKeyAsync(
            request.HierarchyId,
            request.KeyName,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(
            A<Func<Task>>._,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExecuteAsync_ValidatorFails_ReturnsError()
    {
        // Arrange
        var request = new AddApproverMetadataKeyRequest
        {
            HierarchyId = 1,
            KeyName = "InvalidKey"
        };

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("KeyName", "Invalid metadata key name")
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False, "Operation should fail due to validation errors");
            Assert.That(result.Messages, Is.EquivalentTo(validationFailures.Select(v => v.ErrorMessage)));
        });

        // Verify no repository calls were made
        A.CallTo(() => _metadataRepo.AddAsync(A<Metadata>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
