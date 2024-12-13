using FluentValidation;
using FluentValidation.Results;
using Moq;
using QueryRulesEngine.dtos;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.Repositories.Interfaces;
using QueryRulesEngine.Rules.AddRuleToLevel;
using System.Data;

namespace QueryRulesEngine.Tests.Hierarchys
{
    [TestFixture]
    public class AddRuleToLevelServiceTests(
        Mock<IRuleRepository> ruleRepositoryMock = null,
        Mock<ILevelRepository> levelRepositoryMock = null,
        Mock<IValidator<AddRuleToLevelRequest>> validatorMock = null)
    {
        private readonly Mock<IRuleRepository> _ruleRepositoryMock = ruleRepositoryMock ?? new();
        private readonly Mock<ILevelRepository> _levelRepositoryMock = levelRepositoryMock ?? new();
        private readonly Mock<IValidator<AddRuleToLevelRequest>> _validatorMock = validatorMock ?? new();
        private readonly AddRuleToLevelService _service;

        public AddRuleToLevelServiceTests() : this(new(), new(), new())
        {
            _service = new AddRuleToLevelService(
                _ruleRepositoryMock.Object,
                _levelRepositoryMock.Object,
                _validatorMock.Object);
        }

        [Test]
        public async Task ExecuteAsync_ValidRequest_CreatesRule()
        {
            // Arrange
            var queryMatrix = new QueryMatrix
            {
                LogicalOperator = QueryOperator.And,
                Conditions =
                [
                    new QueryCondition
            {
                Field = "Department",
                Operator = QueryOperator.Equal,
                Value = ConditionValue.Single("Sales")
            }
                ],
                NestedMatrices = []
            };

            var request = new AddRuleToLevelRequest
            {
                HierarchyId = 1,
                LevelNumber = 1,
                QueryMatrix = queryMatrix
            };

            RuleDto capturedRule = null;

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<AddRuleToLevelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ruleRepositoryMock
                .Setup(x => x.GetNextRuleNumberAsync(
                    request.HierarchyId,
                    request.LevelNumber,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _ruleRepositoryMock
                .Setup(x => x.CreateRuleAsync(It.IsAny<RuleDto>(), It.IsAny<CancellationToken>()))
                .Callback<RuleDto, CancellationToken>((rule, _) => capturedRule = rule)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Data.HierarchyId, Is.EqualTo(request.HierarchyId));
                Assert.That(result.Data.LevelNumber, Is.EqualTo(request.LevelNumber));
                Assert.That(result.Data.RuleNumber, Is.EqualTo("1"));

                // Verify captured rule
                Assert.That(capturedRule, Is.Not.Null, "CreateRuleAsync was not called");
                Assert.That(capturedRule.HierarchyId, Is.EqualTo(request.HierarchyId), "HierarchyId mismatch");
                Assert.That(capturedRule.LevelNumber, Is.EqualTo(request.LevelNumber), "LevelNumber mismatch");
                Assert.That(capturedRule.RuleNumber, Is.EqualTo("1"), "RuleNumber mismatch");
                Assert.That(capturedRule.QueryMatrix, Is.EqualTo(request.QueryMatrix), "QueryMatrix mismatch");
            });

            _ruleRepositoryMock.Verify(
                x => x.CreateRuleAsync(It.IsAny<RuleDto>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WithMetadataKeys_ValidatesKeysExist()
        {
            // Arrange
            var queryMatrix = new QueryMatrix
            {
                LogicalOperator = QueryOperator.And,
                Conditions =
                [
                    new QueryCondition
                {
                    Field = "ApproverMetadataKey.Location",
                    Operator = QueryOperator.Equal,
                    Value = ConditionValue.Single("NY")
                }
                ],
                NestedMatrices = []
            };

            var request = new AddRuleToLevelRequest
            {
                HierarchyId = 1,
                LevelNumber = 1,
                QueryMatrix = queryMatrix
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<AddRuleToLevelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ruleRepositoryMock
                .Setup(x => x.GetExistingMetadataKeysAsync(
                    request.HierarchyId,
                    It.Is<IEnumerable<string>>(keys => keys.Contains("ApproverMetadataKey.Location")),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "ApproverMetadataKey.Location" });

            _ruleRepositoryMock
                .Setup(x => x.GetNextRuleNumberAsync(
                    request.HierarchyId,
                    request.LevelNumber,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);

            _ruleRepositoryMock.Verify(
                x => x.GetExistingMetadataKeysAsync(
                    request.HierarchyId,
                    It.Is<IEnumerable<string>>(keys => keys.Contains("ApproverMetadataKey.Location")),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_InvalidMetadataKeys_ReturnsError()
        {
            // Arrange
            var queryMatrix = new QueryMatrix
            {
                LogicalOperator = QueryOperator.And,
                Conditions =
                [
                    new QueryCondition
                {
                    Field = "ApproverMetadataKey.InvalidKey",
                    Operator = QueryOperator.Equal,
                    Value = ConditionValue.Single("Value")
                }
                ],
                NestedMatrices = []
            };

            var request = new AddRuleToLevelRequest
            {
                HierarchyId = 1,
                LevelNumber = 1,
                QueryMatrix = queryMatrix
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<AddRuleToLevelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ruleRepositoryMock
                .Setup(x => x.GetExistingMetadataKeysAsync(
                    request.HierarchyId,
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Messages, Does.Contain("Error adding rule to level: The following metadata keys do not exist: ApproverMetadataKey.InvalidKey"));
            });
        }
    }
}