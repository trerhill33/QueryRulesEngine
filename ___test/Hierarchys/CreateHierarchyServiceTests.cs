using ApprovalHierarchyManager.Application.Features.ApprovalHierarchy.CreateHierarchy.Models;
using FluentValidation.Results;
using FluentValidation;
using Moq;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Hierarchys.CreateHierarchy;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Tests.Hierarchys
{
    [TestFixture]
    public class CreateHierarchyServiceTests(
        Mock<IHierarchyRepository>? hierarchyRepositoryMock = null,
        Mock<ILevelRepository>? levelRepositoryMock = null,
        Mock<IValidator<CreateHierarchyRequest>>? validatorMock = null)
    {
        private readonly Mock<IHierarchyRepository> _hierarchyRepositoryMock = hierarchyRepositoryMock ?? new();
        private readonly Mock<ILevelRepository> _levelRepositoryMock = levelRepositoryMock ?? new();
        private readonly Mock<IValidator<CreateHierarchyRequest>> _validatorMock = validatorMock ?? new();
        private readonly CreateHierarchyService _service;

        public CreateHierarchyServiceTests() : this(new(), new(), new())
        {
            _service = new CreateHierarchyService(
                _hierarchyRepositoryMock.Object,
                _levelRepositoryMock.Object,
                _validatorMock.Object);
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

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<CreateHierarchyRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var createdHierarchy = new Hierarchy
            {
                Id = 1,
                Name = request.Name,
                Description = request.Description
            };

            _hierarchyRepositoryMock
                .Setup(x => x.CreateHierarchyAsync(request.Name, request.Description, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdHierarchy);

            _levelRepositoryMock
                .Setup(x => x.CreateDefaultLevelsAsync(createdHierarchy.Id, It.IsAny<CancellationToken>()))
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

            _hierarchyRepositoryMock.Verify(
                x => x.CreateHierarchyAsync(request.Name, request.Description, It.IsAny<CancellationToken>()),
                Times.Once);

            _levelRepositoryMock.Verify(
                x => x.CreateDefaultLevelsAsync(createdHierarchy.Id, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_EmptyName_ReturnsNameRequiredError()
        {
            // Arrange
            var request = new CreateHierarchyRequest { Name = "" };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<CreateHierarchyRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Messages, Is.EqualTo(new[] { "Name is required" }));
            });

            _hierarchyRepositoryMock.Verify(
                x => x.CreateHierarchyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _levelRepositoryMock.Verify(
                x => x.CreateDefaultLevelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_DuplicateName_ReturnsDuplicateError()
        {
            // Arrange
            var request = new CreateHierarchyRequest { Name = "Existing" };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<CreateHierarchyRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "Hierarchy with this name already exists") }));

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Messages, Is.EqualTo(new[] { "Hierarchy with this name already exists" }));
            });

            _hierarchyRepositoryMock.Verify(
                x => x.CreateHierarchyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _levelRepositoryMock.Verify(
                x => x.CreateDefaultLevelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}