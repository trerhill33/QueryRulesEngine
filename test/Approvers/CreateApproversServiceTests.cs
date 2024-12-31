using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using NUnit.Framework.Legacy;
using QueryRulesEngine.Features.Approvers.CreateApprovers;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Approvers;

[TestFixture]
public class CreateApproversServiceTests
{
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IUnitOfWork<int> _unitOfWork;
    private IValidator<CreateApproversRequest> _validator;
    private IWriteRepositoryAsync<Approver, int> _approverRepo;
    private IWriteRepositoryAsync<Metadata, int> _metadataRepo;
    private CreateApproversService _service;

    [SetUp]
    public void Setup()
    {
        // Initialize fakes
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _unitOfWork = A.Fake<IUnitOfWork<int>>();
        _validator = A.Fake<IValidator<CreateApproversRequest>>();
        _approverRepo = A.Fake<IWriteRepositoryAsync<Approver, int>>();
        _metadataRepo = A.Fake<IWriteRepositoryAsync<Metadata, int>>();

        // Setup UnitOfWork repositories
        A.CallTo(() => _unitOfWork.Repository<Approver>()).Returns(_approverRepo);
        A.CallTo(() => _unitOfWork.Repository<Metadata>()).Returns(_metadataRepo);

        _service = new CreateApproversService(
            _readOnlyRepository,
            _unitOfWork,
            _validator);
    }

    [Test]
    public async Task ExecuteAsync_ValidRequest_CreatesApproversAndMetadata()
    {
        // Arrange
        var request = new CreateApproversRequest(
            HierarchyId: 1,
            EmployeeTMIds: ["123", "456" ]
        );

        var existingMetadataKeys = new List<string>
        {
            "ApproverMetadataKey.ExpenseLimit",
            "ApproverMetadataKey.Department"
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
            A<Expression<Func<MetadataKey, bool>>>._,
            A<Expression<Func<MetadataKey, string>>>._,
            A<CancellationToken>._))
            .Returns(existingMetadataKeys);

        var addedApprovers = new List<Approver>();
        var addedMetadata = new List<Metadata>();

        A.CallTo(() => _approverRepo.AddAsync(A<Approver>._, A<CancellationToken>._))
            .Invokes((Approver approver, CancellationToken _) =>
            {
                approver.Id = addedApprovers.Count + 1;
                addedApprovers.Add(approver);
            })
            .ReturnsLazily((Approver approver, CancellationToken _) => Task.FromResult(approver));

        A.CallTo(() => _metadataRepo.AddAsync(A<Metadata>._, A<CancellationToken>._))
            .Invokes((Metadata metadata, CancellationToken _) =>
            {
                metadata.Id = addedMetadata.Count + 1;
                addedMetadata.Add(metadata);
            })
            .ReturnsLazily((Metadata metadata, CancellationToken _) => Task.FromResult(metadata));

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .Invokes(async (Func<Task> operation, CancellationToken _) => await operation());

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            // Verify basic success
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.ApproversCreated, Is.EqualTo(2));
            Assert.That(result.Data.CreatedApproverTMIds, Is.EquivalentTo(request.EmployeeTMIds));

            // Verify approvers
            Assert.That(addedApprovers, Has.Count.EqualTo(2));
            CollectionAssert.AllItemsAreUnique(addedApprovers.Select(a => a.ApproverId));
            Assert.That(addedApprovers.Select(a => a.ApproverId), Is.EquivalentTo(request.EmployeeTMIds));
            Assert.That(addedApprovers.All(a => a.HierarchyId == request.HierarchyId));

            // Verify metadata
            Assert.That(addedMetadata, Has.Count.EqualTo(4), "Should create 2 metadata records per approver");

            foreach (var approver in addedApprovers)
            {
                var approverMetadata = addedMetadata.Where(m => m.ApproverId == approver.ApproverId).ToList();
                Assert.That(approverMetadata, Has.Count.EqualTo(2), $"Approver {approver.ApproverId} should have 2 metadata records");

                // Verify metadata keys
                var keys = approverMetadata.Select(m => m.Key).ToList();
                CollectionAssert.AreEquivalent(existingMetadataKeys, keys);

                // Verify all values are null
                Assert.That(approverMetadata.All(m => m.Value == string.Empty));
            }
        });

        // Verify transaction was used
        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExecuteAsync_DuplicateApprovers_ReturnsError()
    {
        // Arrange
        var request = new CreateApproversRequest(
            HierarchyId: 1,
            EmployeeTMIds: ["123", "456"]
        );

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("EmployeeTMIds", "One or more employees are already approvers for this hierarchy")
            }));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Contains.Item("One or more employees are already approvers for this hierarchy"));
        });

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task ExecuteAsync_InvalidEmployees_ReturnsError()
    {
        // Arrange
        var request = new CreateApproversRequest(
            HierarchyId: 1,
                EmployeeTMIds: ["nonexistent"]
        );

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("EmployeeTMIds", "One or more employees do not exist")
            }));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Contains.Item("One or more employees do not exist"));
        });
    }
}
