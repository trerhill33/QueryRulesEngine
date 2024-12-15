using FluentValidation;
using FluentValidation.Results;
using Moq;
using QueryRulesEngine.Features.MetadataKeys.SyncApproverMetadataKeys;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys.Metadatakeys
{
    [TestFixture]
    public class SyncApproverMetadataKeysServiceTests
    {
        // Test data class containing all shared constants and data
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

            public static List<Metadata> GetExistingMetadata() =>
        [
            // 123 has ExpenseLimit
            new Metadata
            {
                HierarchyId = ValidHierarchyId,
                ApproverId = "123",
                Key = "ApproverMetadataKey.ExpenseLimit",
                Value = null
            },
            // Both have OldKey
            new Metadata
            {
                HierarchyId = ValidHierarchyId,
                ApproverId = "123",
                Key = "ApproverMetadataKey.OldKey",
                Value = null
            },
            new Metadata
            {
                HierarchyId = ValidHierarchyId,
                ApproverId = "456",
                Key = "ApproverMetadataKey.OldKey",
                Value = null
            }
        ];

            public static List<Metadata> GetSyncedMetadata() =>
        [
            new Metadata
            {
                HierarchyId = ValidHierarchyId,
                ApproverId = "123",
                Key = "ApproverMetadataKey.ExpenseLimit",
                Value = null
            }
        ];
        }

        // Fields for mocks and service
        private Mock<IApproverMetadataRepository> _approverMetadataRepositoryMock;
        private Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock;
        private Mock<IUnitOfWork<int>> _unitOfWorkMock;
        private Mock<IValidator<SyncApproverMetadataKeysRequest>> _validatorMock;
        private Mock<IWriteRepositoryAsync<Metadata, int>> _metadataRepoMock;
        private SyncApproverMetadataKeysService _service;
        private List<Metadata> _addedMetadata;
        private List<Metadata> _deletedMetadata;

        [SetUp]
        public void Setup()
        {
            // Initialize all mocks and tracking collections
            _approverMetadataRepositoryMock = new();
            _readOnlyRepositoryMock = new();
            _unitOfWorkMock = new();
            _validatorMock = new();
            _metadataRepoMock = new();
            _addedMetadata = [];
            _deletedMetadata = [];

            SetupBasicMocks();
            CreateService();
        }

        private void SetupBasicMocks()
        {
            // Setup UnitOfWork and metadata repository
            _unitOfWorkMock
                .Setup(x => x.Repository<Metadata>())
                .Returns(_metadataRepoMock.Object);

            // Setup metadata tracking
            _metadataRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Metadata>(), It.IsAny<CancellationToken>()))
                .Callback<Metadata, CancellationToken>((m, _) => _addedMetadata.Add(m))
                .Returns((Metadata m, CancellationToken _) => Task.FromResult(m));

            _metadataRepoMock
                .Setup(r => r.DeleteAsync(It.IsAny<Metadata>()))
                .Callback<Metadata>(m => _deletedMetadata.Add(m))
                .Returns(Task.CompletedTask);

            // Setup transaction handling
            _unitOfWorkMock
                .Setup(u => u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Func<Task>, CancellationToken>(async (operation, _) => await operation());
        }

        private void CreateService()
        {
            _service = new SyncApproverMetadataKeysService(
                _approverMetadataRepositoryMock.Object,
                _readOnlyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _validatorMock.Object);
        }

        private void SetupSuccessScenario()
        {
            // Setup validation success
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<SyncApproverMetadataKeysRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // Setup metadata keys retrieval
            _approverMetadataRepositoryMock
                .Setup(r => r.GetApproverMetadataKeysAsync(
                    TestData.ValidHierarchyId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.DefinedMetadataKeys.ToList());

            // Setup approvers retrieval
            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAndTransformAsync<Approver, string>(
                    It.IsAny<Expression<Func<Approver, bool>>>(),
                    It.IsAny<Expression<Func<Approver, string>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.ApproverIds.ToList());

            // Setup existing metadata retrieval
            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAsync<Metadata>(
                    It.IsAny<Expression<Func<Metadata, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.GetExistingMetadata());
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
                Assert.That(result.Data.MetadataRecordsAdded, Is.EqualTo(3),
                    "Should add Department key for both approvers and ExpenseLimit for 456");
                Assert.That(result.Data.MetadataRecordsRemoved, Is.EqualTo(2),
                    "Should remove OldKey for both approvers");

                // Verify added records
                Assert.That(_addedMetadata, Has.Count.EqualTo(3));

                // Check specific records were added
                var added456ExpenseLimit = _addedMetadata
                    .Count(m => m.ApproverId == "456" &&
                               m.Key == "ApproverMetadataKey.ExpenseLimit") == 1;
                var added123Department = _addedMetadata
                    .Count(m => m.ApproverId == "123" &&
                               m.Key == "ApproverMetadataKey.Department") == 1;
                var added456Department = _addedMetadata
                    .Count(m => m.ApproverId == "456" &&
                               m.Key == "ApproverMetadataKey.Department") == 1;

                Assert.That(added456ExpenseLimit, Is.True, "456 should get ExpenseLimit");
                Assert.That(added123Department, Is.True, "123 should get Department");
                Assert.That(added456Department, Is.True, "456 should get Department");

                // Verify removed records
                Assert.That(_deletedMetadata, Has.Count.EqualTo(2));
                Assert.That(_deletedMetadata.All(m => m.Key == "ApproverMetadataKey.OldKey"), Is.True);
            });
        }

        [Test]
        public async Task ExecuteAsync_NoChangesNeeded_Success()
        {
            // Arrange
            var request = new SyncApproverMetadataKeysRequest(TestData.ValidHierarchyId);

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<SyncApproverMetadataKeysRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // Setup scenario where everything is in sync
            _approverMetadataRepositoryMock
                .Setup(r => r.GetApproverMetadataKeysAsync(
                    TestData.ValidHierarchyId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "ApproverMetadataKey.ExpenseLimit" }.ToList());

            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAndTransformAsync<Approver, string>(
                    It.IsAny<Expression<Func<Approver, bool>>>(),
                    It.IsAny<Expression<Func<Approver, string>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "123" }.ToList());

            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAsync<Metadata>(
                    It.IsAny<Expression<Func<Metadata, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.GetSyncedMetadata());

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Data.MetadataRecordsAdded, Is.EqualTo(0));
                Assert.That(result.Data.MetadataRecordsRemoved, Is.EqualTo(0));
                Assert.That(result.Data.AddedKeys, Is.Empty);
                Assert.That(result.Data.RemovedKeys, Is.Empty);
            });

            _metadataRepoMock.Verify(
                r => r.AddAsync(It.IsAny<Metadata>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _metadataRepoMock.Verify(
                r => r.DeleteAsync(It.IsAny<Metadata>()),
                Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_InvalidHierarchy_ReturnsError()
        {
            // Arrange
            var request = new SyncApproverMetadataKeysRequest(TestData.InvalidHierarchyId);

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<SyncApproverMetadataKeysRequest>(), It.IsAny<CancellationToken>()))
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
                Assert.That(result.Messages, Contains.Item("Hierarchy does not exist."));
            });

            _unitOfWorkMock.Verify(
                u => u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
