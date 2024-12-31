using FakeItEasy;
using QueryRulesEngine.Features.MetadataKeys.TaggedMetadataUpdate;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys.Metadatakeys;

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

    private IUnitOfWork<int> _unitOfWork;
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IWriteRepositoryAsync<Metadata, int> _metadataRepo;
    private TaggedMetadataUpdateService _updateService;
    private List<Metadata> _addedMetadata;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _unitOfWork = A.Fake<IUnitOfWork<int>>();
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _metadataRepo = A.Fake<IWriteRepositoryAsync<Metadata, int>>();
        _addedMetadata = new List<Metadata>();
        _cancellationToken = CancellationToken.None;

        A.CallTo(() => _unitOfWork.Repository<Metadata>()).Returns(_metadataRepo);

        A.CallTo(() => _metadataRepo.AddAsync(A<Metadata>._, A<CancellationToken>._))
            .Invokes((Metadata m, CancellationToken _) => _addedMetadata.Add(m))
            .ReturnsLazily((Metadata m, CancellationToken _) => Task.FromResult(m));

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(A<Func<Task>>._, A<CancellationToken>._))
            .Invokes((Func<Task> operation, CancellationToken _) => operation());

        A.CallTo(() => _unitOfWork.CommitAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(1));

        _updateService = new TaggedMetadataUpdateService(_unitOfWork, _readOnlyRepository);
    }

    [Test]
    public async Task UpdateMetadataValueByTag_ValidTag_UpdatesAllTaggedHierarchies()
    {
        // Arrange
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<Hierarchy, int>(
                A<Expression<Func<Hierarchy, bool>>>._,
                A<Expression<Func<Hierarchy, int>>>._,
                _cancellationToken))
            .Returns(TestData.TaggedHierarchyIds);

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

        A.CallTo(() => _metadataRepo.AddAsync(
            A<Metadata>.That.Matches(m =>
                m.ApproverId == TestData.ApproverId &&
                m.Key == TestData.MetadataKey &&
                m.Value == TestData.Value),
            _cancellationToken)).MustHaveHappened(TestData.TaggedHierarchyIds.Count, Times.Exactly);
    }

    [Test]
    public async Task UpdateMetadataValueByTag_TagNotFound_ReturnsFailure()
    {
        // Arrange
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<Hierarchy, int>(
                A<Expression<Func<Hierarchy, bool>>>._,
                A<Expression<Func<Hierarchy, int>>>._,
                _cancellationToken))
            .Returns([]);

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

        A.CallTo(() => _metadataRepo.AddAsync(A<Metadata>._, _cancellationToken)).MustNotHaveHappened();
    }
}
