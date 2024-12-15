using FluentValidation;
using FluentValidation.Results;
using Moq;
using NUnit.Framework.Legacy;
using QueryRulesEngine.Features.Approvers.CreateApprovers;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Approvers
{
    [TestFixture]
    public class CreateApproversServiceTests
    {
        private Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock;
        private Mock<IUnitOfWork<int>> _unitOfWorkMock;
        private Mock<IValidator<CreateApproversRequest>> _validatorMock;
        private Mock<IWriteRepositoryAsync<Approver, int>> _approverRepoMock;
        private Mock<IWriteRepositoryAsync<Metadata, int>> _metadataRepoMock;
        private CreateApproversService _service;

        [SetUp]
        public void Setup()
        {
            _readOnlyRepositoryMock = new Mock<IReadOnlyRepositoryAsync<int>>();
            _unitOfWorkMock = new Mock<IUnitOfWork<int>>();
            _validatorMock = new Mock<IValidator<CreateApproversRequest>>();
            _approverRepoMock = new Mock<IWriteRepositoryAsync<Approver, int>>();
            _metadataRepoMock = new Mock<IWriteRepositoryAsync<Metadata, int>>();

            // Setup UnitOfWork repositories
            _unitOfWorkMock
                .Setup(x => x.Repository<Approver>())
                .Returns(_approverRepoMock.Object);

            _unitOfWorkMock
                .Setup(x => x.Repository<Metadata>())
                .Returns(_metadataRepoMock.Object);

            _service = new CreateApproversService(
                _readOnlyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _validatorMock.Object);
        }

        [Test]
        public async Task ExecuteAsync_ValidRequest_CreatesApproversAndMetadata()
        {
            // Arrange
            var request = new CreateApproversRequest(
                HierarchyId: 1,
                EmployeeTMIds: ["123", "456"]
            );

            var existingMetadataKeys = new List<string>
            {
                "ApproverMetadataKey.ExpenseLimit",
                "ApproverMetadataKey.Department"
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<CreateApproversRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _readOnlyRepositoryMock
                .Setup(x => x.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                    It.IsAny<Expression<Func<MetadataKey, bool>>>(),
                    It.IsAny<Expression<Func<MetadataKey, string>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingMetadataKeys);

            var addedApprovers = new List<Approver>();
            var addedMetadata = new List<Metadata>();

            _approverRepoMock
                .Setup(x => x.AddAsync(It.IsAny<Approver>(), It.IsAny<CancellationToken>()))
                .Callback<Approver, CancellationToken>((approver, _) =>
                {
                    approver.Id = addedApprovers.Count + 1;
                    addedApprovers.Add(approver);
                })
                .ReturnsAsync((Approver approver, CancellationToken _) => approver);

            _metadataRepoMock
                .Setup(x => x.AddAsync(It.IsAny<Metadata>(), It.IsAny<CancellationToken>()))
                .Callback<Metadata, CancellationToken>((metadata, _) =>
                {
                    metadata.Id = addedMetadata.Count + 1;
                    addedMetadata.Add(metadata);
                })
                .ReturnsAsync((Metadata metadata, CancellationToken _) => metadata);

            _unitOfWorkMock
                .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<Task>, CancellationToken>(async (operation, _) => await operation());

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

                // Check each approver has metadata records
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
            _unitOfWorkMock.Verify(
                x => x.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_DuplicateApprovers_ReturnsError()
        {
            // Arrange
            var request = new CreateApproversRequest(
                HierarchyId: 1,
                EmployeeTMIds: ["123", "456"]
            );

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<CreateApproversRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
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

            _unitOfWorkMock.Verify(
                x => x.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_InvalidEmployees_ReturnsError()
        {
            // Arrange
            var request = new CreateApproversRequest(
                HierarchyId: 1,
                EmployeeTMIds: ["nonexistent"]
            );

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<CreateApproversRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
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
}
