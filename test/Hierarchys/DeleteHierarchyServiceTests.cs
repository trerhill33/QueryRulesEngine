using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Features.Hierarchies.DeleteHierarchy;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys;

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

    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IUnitOfWork<int> _unitOfWork;
    private IValidator<DeleteHierarchyRequest> _validator;
    private IWriteRepositoryAsync<Metadata, int> _metadataRepo;
    private IWriteRepositoryAsync<MetadataKey, int> _metadataKeyRepo;
    private DeleteHierarchyService _service;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>(opts =>
            opts.Strict());
        _unitOfWork = A.Fake<IUnitOfWork<int>>(opts =>
            opts.Strict());
        _validator = A.Fake<IValidator<DeleteHierarchyRequest>>(opts =>
            opts.Strict());
        _metadataRepo = A.Fake<IWriteRepositoryAsync<Metadata, int>>(opts =>
            opts.Strict());
        _metadataKeyRepo = A.Fake<IWriteRepositoryAsync<MetadataKey, int>>(opts =>
            opts.Strict());
        _cancellationToken = CancellationToken.None;

        SetupBasicFakes();
        _service = new DeleteHierarchyService(
            _readOnlyRepository,
            _unitOfWork,
            _validator);
    }

    private void SetupBasicFakes()
    {
        A.CallTo(() => _unitOfWork.Repository<Metadata>())
            .Returns(_metadataRepo);

        A.CallTo(() => _unitOfWork.Repository<MetadataKey>())
            .Returns(_metadataKeyRepo);

        // Allow any DeleteRange call (don't be strict about it)
        A.CallTo(() => _metadataRepo.DeleteRange(A<IEnumerable<Metadata>>._))
            .DoesNothing();

        A.CallTo(() => _metadataKeyRepo.DeleteRange(A<IEnumerable<MetadataKey>>._))
            .DoesNothing();

        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(
            A<Func<Task>>._,
            _cancellationToken))
            .Invokes(async (Func<Task> operation, CancellationToken _) =>
                await operation())
            .Returns(Task.CompletedTask);

        A.CallTo(() => _unitOfWork.CommitAsync(_cancellationToken))
            .Returns(1);
    }

    private void VerifyDeletions()
    {
        // Verify transaction was executed
        A.CallTo(() => _unitOfWork.ExecuteInTransactionAsync(
            A<Func<Task>>._,
            _cancellationToken))
            .MustHaveHappenedOnceExactly();

        // Verify deletion of metadata records using simpler matching
        A.CallTo(() => _metadataRepo.DeleteRange(
            A<IEnumerable<Metadata>>.That.Matches(items =>
                items.Any(m => m.HierarchyId == TestData.ValidHierarchyId))))
            .MustHaveHappenedOnceExactly();

        // Verify deletion of metadata keys using simpler matching
        A.CallTo(() => _metadataKeyRepo.DeleteRange(
            A<IEnumerable<MetadataKey>>.That.Matches(items =>
                items.Any(mk => mk.HierarchyId == TestData.ValidHierarchyId))))
            .MustHaveHappenedOnceExactly();

        // Verify commit
        A.CallTo(() => _unitOfWork.CommitAsync(_cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExecuteAsync_ValidHierarchy_DeletesAllMetadata()
    {
        // Arrange
        var request = new DeleteHierarchyRequest { HierarchyId = TestData.ValidHierarchyId };

        A.CallTo(() => _validator.ValidateAsync(request, _cancellationToken))
            .Returns(new ValidationResult());

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync<Metadata>(
            A<Expression<Func<Metadata, bool>>>._,
            _cancellationToken))
            .Returns(TestData.MetadataRecords);

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync<MetadataKey>(
            A<Expression<Func<MetadataKey, bool>>>._,
            _cancellationToken))
            .Returns(TestData.MetadataKeys);

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
}