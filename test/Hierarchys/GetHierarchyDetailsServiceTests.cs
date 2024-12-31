using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;

namespace QueryRulesEngine.Tests.Hierarchys;

[TestFixture]
public class GetHierarchyDetailsServiceTests
{
    private GetHierarchyDetailsService _service;
    private IUnitOfWork<int> _unitOfWork;
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IValidator<GetHierarchyDetailsRequest> _validator;

    [SetUp]
    public void Setup()
    {
        // Initialize fakes
        _unitOfWork = A.Fake<IUnitOfWork<int>>();
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _validator = A.Fake<IValidator<GetHierarchyDetailsRequest>>();

        // Initialize the service with faked dependencies
        _service = new GetHierarchyDetailsService(
            _unitOfWork,
            _readOnlyRepository,
            _validator);
    }

    [Test]
    public async Task ExecuteAsync_ValidRequest_RetrievesHierarchyWithMetadataKeys()
    {
        // Arrange
        var request = new GetHierarchyDetailsRequest
        {
            HierarchyId = 1
        };

        // Setup validator to return a successful ValidationResult
        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        // Setup IReadOnlyRepositoryAsync to return a valid hierarchy
        var hierarchy = new Hierarchy
        {
            Id = request.HierarchyId,
            Name = "Test Hierarchy",
            Description = "Test Description"
        };

        A.CallTo(() => _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
            A<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>._,
            A<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>._,
            A<CancellationToken>._))
            .Returns(hierarchy);

        // Setup IReadOnlyRepositoryAsync to return metadata keys
        var metadataKeys = new List<MetadataKey>
        {
            new MetadataKey { Id = 1, HierarchyId = request.HierarchyId, KeyName = "Key1" },
            new MetadataKey { Id = 2, HierarchyId = request.HierarchyId, KeyName = "Key2" }
        };

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
            A<System.Linq.Expressions.Expression<Func<MetadataKey, bool>>>._,
            A<System.Linq.Expressions.Expression<Func<MetadataKey, MetadataKey>>>._,
            A<CancellationToken>._))
            .Returns(metadataKeys);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Id, Is.EqualTo(hierarchy.Id));
            Assert.That(result.Data.Name, Is.EqualTo(hierarchy.Name));
            Assert.That(result.Data.Description, Is.EqualTo(hierarchy.Description));
            Assert.That(result.Data.MetadataKeys, Has.Count.EqualTo(metadataKeys.Count));
            Assert.That(result.Data.MetadataKeys.Any(mk => mk.Id == 1 && mk.KeyName == "Key1"), Is.True);
            Assert.That(result.Data.MetadataKeys.Any(mk => mk.Id == 2 && mk.KeyName == "Key2"), Is.True);
        });

        // Verify repository calls
        A.CallTo(() => _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
            A<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>._,
            A<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>._,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
            A<System.Linq.Expressions.Expression<Func<MetadataKey, bool>>>._,
            A<System.Linq.Expressions.Expression<Func<MetadataKey, MetadataKey>>>._,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExecuteAsync_HierarchyDoesNotExist_ReturnsHierarchyNotFoundError()
    {
        // Arrange
        var request = new GetHierarchyDetailsRequest
        {
            HierarchyId = 99 // Assume this hierarchy ID does not exist
        };

        // Setup validator to return a successful ValidationResult
        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        // Setup IReadOnlyRepositoryAsync to return null (hierarchy does not exist)
        A.CallTo(() => _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
            A<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>._,
            A<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>._,
            A<CancellationToken>._))
            .Returns((Hierarchy)null);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Contains.Item("Hierarchy not found."));
        });

        // Verify repository calls
        A.CallTo(() => _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
            A<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>._,
            A<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>._,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        // MetadataKey repository should not be called
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
            A<System.Linq.Expressions.Expression<Func<MetadataKey, bool>>>._,
            A<System.Linq.Expressions.Expression<Func<MetadataKey, MetadataKey>>>._,
            A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task ExecuteAsync_ValidatorReturnsError_ReturnsValidationErrors()
    {
        // Arrange
        var request = new GetHierarchyDetailsRequest
        {
            HierarchyId = 1
        };

        // Setup validator to return a failed ValidationResult
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("HierarchyId", "Hierarchy does not exist.")
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Is.EquivalentTo(validationFailures.Select(vf => vf.ErrorMessage)));
        });

        // Verify repositories were not called
        A.CallTo(() => _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
            A<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>._,
            A<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>._,
            A<CancellationToken>._)).MustNotHaveHappened();

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
            A<System.Linq.Expressions.Expression<Func<MetadataKey, bool>>>._,
            A<System.Linq.Expressions.Expression<Func<MetadataKey, MetadataKey>>>._,
            A<CancellationToken>._)).MustNotHaveHappened();
    }
}
