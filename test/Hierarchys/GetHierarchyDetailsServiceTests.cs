using FluentValidation;
using FluentValidation.Results;
using Moq;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;

namespace QueryRulesEngine.Tests.Hierarchys
{
    [TestFixture]
    public class GetHierarchyDetailsServiceTests
    {
        private GetHierarchyDetailsService _service;
        private Mock<IUnitOfWork<int>> _unitOfWorkMock;
        private Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock;
        private Mock<IValidator<GetHierarchyDetailsRequest>> _validatorMock;

        [SetUp]
        public void Setup()
        {
            // Initialize mocks
            _unitOfWorkMock = new Mock<IUnitOfWork<int>>();
            _readOnlyRepositoryMock = new Mock<IReadOnlyRepositoryAsync<int>>();
            _validatorMock = new Mock<IValidator<GetHierarchyDetailsRequest>>();

            // Initialize the service with mocked dependencies
            _service = new GetHierarchyDetailsService(
                _unitOfWorkMock.Object,
                _readOnlyRepositoryMock.Object,
                _validatorMock.Object);
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
            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetHierarchyDetailsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // Setup IReadOnlyRepositoryAsync to return a valid hierarchy
            var hierarchy = new Hierarchy
            {
                Id = request.HierarchyId,
                Name = "Test Hierarchy",
                Description = "Test Description"
            };
            _readOnlyRepositoryMock.Setup(r => r.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(hierarchy);

            // Setup IReadOnlyRepositoryAsync to return metadata keys
            var metadataKeys = new List<MetadataKey>
            {
                new MetadataKey { Id = 1, HierarchyId = request.HierarchyId, KeyName = "Key1" },
                new MetadataKey { Id = 2, HierarchyId = request.HierarchyId, KeyName = "Key2" }
            };
            _readOnlyRepositoryMock.Setup(r => r.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
                It.IsAny<System.Linq.Expressions.Expression<Func<MetadataKey, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<MetadataKey, MetadataKey>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(metadataKeys);

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            // Check if the operation succeeded
            if (!result.Succeeded)
            {
                Assert.Fail("Expected operation to succeed, but it failed.");
            }

            // Check if the Data property is not null
            if (result.Data == null)
            {
                Assert.Fail("Result data should not be null.");
            }

            // Check if Hierarchy details match
            if (result.Data.Id != hierarchy.Id)
            {
                Assert.Fail($"Expected Hierarchy Id to be {hierarchy.Id}, but got {result.Data.Id}.");
            }

            if (result.Data.Name != hierarchy.Name)
            {
                Assert.Fail($"Expected Hierarchy Name to be '{hierarchy.Name}', but got '{result.Data.Name}'.");
            }

            if (result.Data.Description != hierarchy.Description)
            {
                Assert.Fail($"Expected Hierarchy Description to be '{hierarchy.Description}', but got '{result.Data.Description}'.");
            }

            // Check if MetadataKeys are present and correctly formatted
            if (result.Data.MetadataKeys == null || result.Data.MetadataKeys.Count != metadataKeys.Count)
            {
                Assert.Fail($"Expected {metadataKeys.Count} metadata keys, but got {result.Data.MetadataKeys?.Count ?? 0}.");
            }

            foreach (var key in metadataKeys)
            {
                if (!result.Data.MetadataKeys.Any(mk => mk.Id == key.Id && mk.KeyName == key.KeyName))
                {
                    Assert.Fail($"Metadata key with Id {key.Id} and KeyName '{key.KeyName}' was not found in the response.");
                }
            }

            // Verify that repositories were called correctly
            _readOnlyRepositoryMock.Verify(r => r.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _readOnlyRepositoryMock.Verify(r => r.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
                It.IsAny<System.Linq.Expressions.Expression<Func<MetadataKey, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<MetadataKey, MetadataKey>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
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
            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetHierarchyDetailsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // Setup IReadOnlyRepositoryAsync to return null (hierarchy does not exist)
            _readOnlyRepositoryMock.Setup(r => r.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Hierarchy)null);

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            // Check if the operation failed
            if (result.Succeeded)
            {
                Assert.Fail("Expected operation to fail due to non-existent hierarchy, but it succeeded.");
            }

            // Check if the error message is present
            if (result.Messages == null || !result.Messages.Contains("Hierarchy not found."))
            {
                Assert.Fail("Error messages should contain 'Hierarchy not found.'");
            }

            // Verify that repositories were called correctly
            _readOnlyRepositoryMock.Verify(r => r.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>(),
                It.IsAny<CancellationToken>()), Times.Once);

            // MetadataKey repository should not be called
            _readOnlyRepositoryMock.Verify(r => r.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
                It.IsAny<System.Linq.Expressions.Expression<Func<MetadataKey, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<MetadataKey, MetadataKey>>>(),
                It.IsAny<CancellationToken>()), Times.Never);
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
            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetHierarchyDetailsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
                {
                    new ValidationFailure("HierarchyId", "Hierarchy does not exist.")
                }));

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            // Check if the operation failed
            if (result.Succeeded)
            {
                Assert.Fail("Expected operation to fail due to validation errors, but it succeeded.");
            }

            // Check if the error messages are present
            if (result.Messages == null || !result.Messages.Contains("Hierarchy does not exist."))
            {
                Assert.Fail("Error messages should contain 'Hierarchy does not exist.'");
            }

            // Verify that repositories were not called
            _readOnlyRepositoryMock.Verify(r => r.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Hierarchy, Hierarchy>>>(),
                It.IsAny<CancellationToken>()), Times.Never);

            _readOnlyRepositoryMock.Verify(r => r.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
                It.IsAny<System.Linq.Expressions.Expression<Func<MetadataKey, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<MetadataKey, MetadataKey>>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
