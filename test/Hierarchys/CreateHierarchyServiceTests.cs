using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Features.Hierarchies.CreateHierarchy;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;

namespace QueryRulesEngine.Tests.Hierarchys;

[TestFixture]
public class CreateHierarchyServiceTests
{
    private IHierarchyRepository _hierarchyRepository;
    private ILevelRepository _levelRepository;
    private IValidator<CreateHierarchyRequest> _validator;
    private CreateHierarchyService _service;

    [SetUp]
    public void Setup()
    {
        _hierarchyRepository = A.Fake<IHierarchyRepository>();
        _levelRepository = A.Fake<ILevelRepository>();
        _validator = A.Fake<IValidator<CreateHierarchyRequest>>();

        _service = new CreateHierarchyService(
            _hierarchyRepository,
            _levelRepository,
            _validator);
    }

    [Test]
    public async Task ExecuteAsync_ValidRequest_CreatesHierarchyAndDefaultLevels()
    {
        // Arrange
        var request = new CreateHierarchyRequest
        {
            Name = "Test Hierarchy",
            Description = "Test Description"
        };

        var createdHierarchy = new Hierarchy
        {
            Id = 1,
            Name = request.Name,
            Description = request.Description
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _hierarchyRepository.CreateHierarchyAsync(
            request.Name,
            request.Description,
            A<string>._,
            A<CancellationToken>._))
            .Returns(createdHierarchy);

        A.CallTo(() => _levelRepository.CreateDefaultLevelsAsync(
            createdHierarchy.Id,
            A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.Id, Is.EqualTo(createdHierarchy.Id));
            Assert.That(result.Data.Name, Is.EqualTo(request.Name));
            Assert.That(result.Data.Description, Is.EqualTo(request.Description));
        });

        A.CallTo(() => _hierarchyRepository.CreateHierarchyAsync(
            request.Name,
            request.Description,
            A<string>._,
            A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _levelRepository.CreateDefaultLevelsAsync(
            createdHierarchy.Id,
            A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExecuteAsync_EmptyName_ReturnsNameRequiredError()
    {
        // Arrange
        var request = new CreateHierarchyRequest { Name = "" };

        var validationFailures = new[] { new ValidationFailure("Name", "Name is required") };
        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Is.EqualTo(new[] { "Name is required" }));
        });

        A.CallTo(() => _hierarchyRepository.CreateHierarchyAsync(
            A<string>._,
            A<string>._,
            A<string>._,
            A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _levelRepository.CreateDefaultLevelsAsync(
            A<int>._,
            A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task ExecuteAsync_DuplicateName_ReturnsDuplicateError()
    {
        // Arrange
        var request = new CreateHierarchyRequest { Name = "Existing" };

        var validationFailures = new[] { new ValidationFailure("Name", "Hierarchy with this name already exists") };
        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Is.EqualTo(new[] { "Hierarchy with this name already exists" }));
        });

        A.CallTo(() => _hierarchyRepository.CreateHierarchyAsync(
            A<string>._,
            A<string>._,
            A<string>._,
            A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _levelRepository.CreateDefaultLevelsAsync(
            A<int>._,
            A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}