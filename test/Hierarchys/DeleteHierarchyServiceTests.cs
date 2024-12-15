using FluentValidation;
using FluentValidation.Results;
using Moq;
using QueryRulesEngine.Features.Hierarchies.DeleteHierarchy;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys
{
    [TestFixture]
    public class DeleteHierarchyServiceTests
    {
        private static class TestData
        {
            public const int ValidHierarchyId = 1;
            public const int InvalidHierarchyId = 99;

            public static readonly List<Metadata> MetadataRecords =
        [
            new()
            {
                HierarchyId = ValidHierarchyId,
                ApproverId = "331220",
                Key = "ApproverMetadataKey.ExpenseLimit",
                Value = "5000"
            },
            new()
            {
                HierarchyId = ValidHierarchyId,
                ApproverId = "331220",
                Key = "ApproverMetadataKey.Department",
                Value = "Manufacturing"
            }
        ];

            public static readonly List<MetadataKey> MetadataKeys =
        [
            // Approver metadata keys
            new()
            {
                HierarchyId = ValidHierarchyId,
                KeyName = "ApproverMetadataKey.ExpenseLimit"
            },
            new()
            {
                HierarchyId = ValidHierarchyId,
                KeyName = "ApproverMetadataKey.Department"
            },
            // Rules stored as metadata keys
            new()
            {
                HierarchyId = ValidHierarchyId,
                KeyName = "level.1.rule.1.query:[_and][Employee.Department_eq_Manufacturing]"
            }
        ];
        }

        private Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock;
        private Mock<IUnitOfWork<int>> _unitOfWorkMock;
        private Mock<IValidator<DeleteHierarchyRequest>> _validatorMock;
        private Mock<IWriteRepositoryAsync<Metadata, int>> _metadataRepoMock;
        private Mock<IWriteRepositoryAsync<MetadataKey, int>> _metadataKeyRepoMock;
        private DeleteHierarchyService _service;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Setup()
        {
            _readOnlyRepositoryMock = new Mock<IReadOnlyRepositoryAsync<int>>(MockBehavior.Strict);
            _unitOfWorkMock = new Mock<IUnitOfWork<int>>(MockBehavior.Strict);
            _validatorMock = new Mock<IValidator<DeleteHierarchyRequest>>(MockBehavior.Strict);
            _metadataRepoMock = new Mock<IWriteRepositoryAsync<Metadata, int>>(MockBehavior.Strict);
            _metadataKeyRepoMock = new Mock<IWriteRepositoryAsync<MetadataKey, int>>(MockBehavior.Strict);
            _cancellationToken = CancellationToken.None;

            SetupBasicMocks();
            _service = new DeleteHierarchyService(
                _readOnlyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _validatorMock.Object);
        }

        private void SetupBasicMocks()
        {
            _unitOfWorkMock
                .Setup(x => x.Repository<Metadata>())
                .Returns(_metadataRepoMock.Object);

            _unitOfWorkMock
                .Setup(x => x.Repository<MetadataKey>())
                .Returns(_metadataKeyRepoMock.Object);

            _unitOfWorkMock
                .Setup(u => u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    _cancellationToken))
                .Callback<Func<Task>, CancellationToken>(async (operation, _) => await operation())
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(u => u.CommitAsync(_cancellationToken))
                .ReturnsAsync(1);

            _metadataRepoMock
                .Setup(r => r.DeleteRange(It.IsAny<IEnumerable<Metadata>>()));

            _metadataKeyRepoMock
                .Setup(r => r.DeleteRange(It.IsAny<IEnumerable<MetadataKey>>()));
        }

        [Test]
        public async Task ExecuteAsync_ValidHierarchy_DeletesAllMetadata()
        {
            // Arrange
            var request = new DeleteHierarchyRequest { HierarchyId = TestData.ValidHierarchyId };

            _validatorMock
                .Setup(v => v.ValidateAsync(request, _cancellationToken))
                .ReturnsAsync(new ValidationResult());

            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAsync<Metadata>(
                    It.IsAny<Expression<Func<Metadata, bool>>>(),
                    _cancellationToken))
                .ReturnsAsync(TestData.MetadataRecords);

            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAsync<MetadataKey>(
                    It.IsAny<Expression<Func<MetadataKey, bool>>>(),
                    _cancellationToken))
                .ReturnsAsync(TestData.MetadataKeys);

            // Act
            var result = await _service.ExecuteAsync(request, _cancellationToken);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Data.HierarchyId, Is.EqualTo(TestData.ValidHierarchyId));
            });

            VerifyDeletions();
        }

        [Test]
        public async Task ExecuteAsync_NoMetadata_CompletesSuccessfully()
        {
            // Arrange
            var request = new DeleteHierarchyRequest { HierarchyId = TestData.ValidHierarchyId };

            _validatorMock
                .Setup(v => v.ValidateAsync(request, _cancellationToken))
                .ReturnsAsync(new ValidationResult());

            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAsync<Metadata>(
                    It.IsAny<Expression<Func<Metadata, bool>>>(),
                    _cancellationToken))
                .ReturnsAsync((List<Metadata>)null);

            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAsync<MetadataKey>(
                    It.IsAny<Expression<Func<MetadataKey, bool>>>(),
                    _cancellationToken))
                .ReturnsAsync((List<MetadataKey>)null);

            // Act
            var result = await _service.ExecuteAsync(request, _cancellationToken);

            // Assert
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public async Task ExecuteAsync_ValidationFails_ReturnsError()
        {
            // Arrange
            var request = new DeleteHierarchyRequest { HierarchyId = TestData.InvalidHierarchyId };
            var validationFailure = new ValidationFailure("HierarchyId", "Hierarchy does not exist");

            _validatorMock
                .Setup(v => v.ValidateAsync(request, _cancellationToken))
                .ReturnsAsync(new ValidationResult(new[] { validationFailure }));

            // Act
            var result = await _service.ExecuteAsync(request, _cancellationToken);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Messages, Does.Contain("Hierarchy does not exist"));
            });

            VerifyNoDeleteOperations();
        }

        private void VerifyDeletions()
        {
            // Verify transaction was executed
            _unitOfWorkMock.Verify(
                u => u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    _cancellationToken),
                Times.Once);

            // Verify deletion of metadata records
            _metadataRepoMock.Verify(
                r => r.DeleteRange(
                    It.Is<IEnumerable<Metadata>>(items =>
                        items.Count() == TestData.MetadataRecords.Count &&
                        items.All(m => m.HierarchyId == TestData.ValidHierarchyId))),
                Times.Once);

            // Verify deletion of metadata keys
            _metadataKeyRepoMock.Verify(
                r => r.DeleteRange(
                    It.Is<IEnumerable<MetadataKey>>(items =>
                        items.Count() == TestData.MetadataKeys.Count &&
                        items.All(mk => mk.HierarchyId == TestData.ValidHierarchyId))),
                Times.Once);

            // Verify commit
            _unitOfWorkMock.Verify(
                u => u.CommitAsync(_cancellationToken),
                Times.Once);
        }

        private void VerifyNoDeleteOperations()
        {
            _metadataRepoMock.Verify(
                r => r.DeleteRange(It.IsAny<IEnumerable<Metadata>>()),
                Times.Never);

            _metadataKeyRepoMock.Verify(
                r => r.DeleteRange(It.IsAny<IEnumerable<MetadataKey>>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.CommitAsync(_cancellationToken),
                Times.Never);
        }
    }
}