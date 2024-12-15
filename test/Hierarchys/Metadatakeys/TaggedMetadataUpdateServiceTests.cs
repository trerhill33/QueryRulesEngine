using Moq;
using QueryRulesEngine.Features.MetadataKeys.TaggedMetadataUpdate;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys.Metadatakeys
{
    [TestFixture]
    public class TaggedMetadataUpdateServiceTests
    {
        private static class TestData
        {
            public const string ApproverId = "331220";
            public const string MetadataKey = "ApproverMetadataKey.FinancialLimits";
            public const string Tag = "Vanderbilt";
            public const string Value = "25000";

            public static readonly List<int> TaggedHierarchyIds = [1, 2, 3];
        }

        private Mock<IUnitOfWork<int>> _unitOfWorkMock;
        private Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock;
        private Mock<IWriteRepositoryAsync<Metadata, int>> _metadataRepoMock;
        private TaggedMetadataUpdateService _updateService;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork<int>>(MockBehavior.Strict);
            _readOnlyRepositoryMock = new Mock<IReadOnlyRepositoryAsync<int>>(MockBehavior.Strict);
            _metadataRepoMock = new Mock<IWriteRepositoryAsync<Metadata, int>>(MockBehavior.Strict);
            _cancellationToken = CancellationToken.None;

            SetupBasicMocks();

            _updateService = new TaggedMetadataUpdateService(
                _unitOfWorkMock.Object,
                _readOnlyRepositoryMock.Object);
        }

        private void SetupBasicMocks()
        {
            _unitOfWorkMock
                .Setup(x => x.Repository<Metadata>())
                .Returns(_metadataRepoMock.Object);

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
                .Setup(r => r.AddAsync(It.IsAny<Metadata>(), _cancellationToken))
                .ReturnsAsync((Metadata m, CancellationToken _) => m);
        }

        [Test]
        public async Task UpdateMetadataValueByTag_ValidTag_UpdatesAllTaggedHierarchies()
        {
            // Arrange
            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAndTransformAsync<Hierarchy, int>(
                    It.Is<Expression<Func<Hierarchy, bool>>>(expr =>
                        expr.Compile().Invoke(new Hierarchy { Tag = TestData.Tag })),
                    It.IsAny<Expression<Func<Hierarchy, int>>>(),
                    _cancellationToken))
                .ReturnsAsync(TestData.TaggedHierarchyIds);

            // Act
            var request = new TaggedMetadataUpdateRequest(
                ApproverId: TestData.ApproverId,
                MetadataKey: TestData.MetadataKey,
                Tag: TestData.Tag,
                Value: TestData.Value);

            var result = await _updateService.UpdateMetadataValueByTagAsync(request, _cancellationToken);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Data, Is.EqualTo(3), "Should update 3 hierarchies");
            });

            _metadataRepoMock.Verify(
                r => r.AddAsync(
                    It.Is<Metadata>(m =>
                        m.ApproverId == TestData.ApproverId &&
                        m.Key == TestData.MetadataKey &&
                        m.Value == TestData.Value),
                    _cancellationToken),
                Times.Exactly(3));
        }

        [Test]
        public async Task UpdateMetadataValueByTag_TagNotFound_ReturnsFailure()
        {
            // Arrange
            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAndTransformAsync<Hierarchy, int>(
                    It.IsAny<Expression<Func<Hierarchy, bool>>>(),
                    It.IsAny<Expression<Func<Hierarchy, int>>>(),
                    _cancellationToken))
                .ReturnsAsync([]);

            // Act
            var request = new TaggedMetadataUpdateRequest(
                ApproverId: TestData.ApproverId,
                MetadataKey: TestData.MetadataKey,
                Tag: "NonexistentTag",
                Value: TestData.Value);

            var result = await _updateService.UpdateMetadataValueByTagAsync(request, _cancellationToken);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Messages, Does.Contain("No hierarchies found with tag: NonexistentTag"));
            });

            _metadataRepoMock.Verify(
                r => r.AddAsync(It.IsAny<Metadata>(), _cancellationToken),
                Times.Never);
        }
    }
}
