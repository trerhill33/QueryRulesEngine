using FluentValidation;
using FluentValidation.Results;
using Moq;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Hierarchys.EditRule;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.Repositories.Interfaces;
using QueryRulesEngine.Rules.EditRule;

namespace QueryRulesEngine.Tests.Hierarchys
{
    [TestFixture]
    public class EditRuleServiceTests(
        Mock<IRuleRepository> ruleRepositoryMock = null,
        Mock<ILevelRepository> levelRepositoryMock = null,
        Mock<IValidator<EditRuleRequest>> validatorMock = null)
    {
        private readonly Mock<IRuleRepository> _ruleRepositoryMock = ruleRepositoryMock ?? new();
        private readonly Mock<ILevelRepository> _levelRepositoryMock = levelRepositoryMock ?? new();
        private readonly Mock<IValidator<EditRuleRequest>> _validatorMock = validatorMock ?? new();
        private readonly EditRuleService _service;

        public EditRuleServiceTests() : this(new(), new(), new())
        {
            _service = new EditRuleService(
                _ruleRepositoryMock.Object,
                _levelRepositoryMock.Object,
                _validatorMock.Object);
        }

        [Test]
        public async Task ExecuteAsync_ValidRequest_UpdatesRule()
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

            var request = new EditRuleRequest
            {
                HierarchyId = 1,
                LevelNumber = 1,
                RuleNumber = "1",
                QueryMatrix = queryMatrix
            };

            var existingRule = new RuleDto
            {
                HierarchyId = request.HierarchyId,
                LevelNumber = request.LevelNumber,
                RuleNumber = request.RuleNumber,
                QueryMatrix = queryMatrix
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<EditRuleRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ruleRepositoryMock
                .Setup(x => x.GetRuleAsync(
                    request.HierarchyId,
                    request.LevelNumber,
                    request.RuleNumber,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRule);

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);

            _ruleRepositoryMock.Verify(
                x => x.UpdateRuleAsync(
                    It.Is<RuleDto>(r =>
                        r.HierarchyId == request.HierarchyId &&
                        r.LevelNumber == request.LevelNumber &&
                        r.RuleNumber == request.RuleNumber &&
                        r.QueryMatrix == request.QueryMatrix),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_RuleNotFound_ReturnsError()
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

            var request = new EditRuleRequest
            {
                HierarchyId = 1,
                LevelNumber = 1,
                RuleNumber = "999", // Non-existent rule
                QueryMatrix = queryMatrix
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<EditRuleRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ruleRepositoryMock
                .Setup(x => x.GetRuleAsync(
                    request.HierarchyId,
                    request.LevelNumber,
                    request.RuleNumber,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((RuleDto)null);

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Messages, Contains.Item("Rule not found."));
            });

            _ruleRepositoryMock.Verify(
                x => x.UpdateRuleAsync(
                    It.IsAny<RuleDto>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_InvalidRequest_ReturnsValidationErrors()
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

            var request = new EditRuleRequest
            {
                HierarchyId = 1,
                LevelNumber = 1,
                RuleNumber = "", // Invalid empty rule number
                QueryMatrix = queryMatrix
            };

            var validationFailures = new[]
            {
            new ValidationFailure("RuleNumber", "Rule number is required"),
            new ValidationFailure("QueryMatrix", "Query matrix must contain at least one condition")
        };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<EditRuleRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Messages, Is.EquivalentTo(validationFailures.Select(f => f.ErrorMessage)));
            });

            _ruleRepositoryMock.Verify(
                x => x.GetRuleAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_WithApproverMetadataKeys_ValidatesKeysExist()
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

            var request = new EditRuleRequest
            {
                HierarchyId = 1,
                LevelNumber = 1,
                RuleNumber = "1",
                QueryMatrix = queryMatrix
            };

            var existingRule = new RuleDto
            {
                HierarchyId = request.HierarchyId,
                LevelNumber = request.LevelNumber,
                RuleNumber = request.RuleNumber
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<EditRuleRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ruleRepositoryMock
                .Setup(x => x.GetRuleAsync(
                    request.HierarchyId,
                    request.LevelNumber,
                    request.RuleNumber,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRule);

            _ruleRepositoryMock
                .Setup(x => x.GetExistingMetadataKeysAsync(
                    request.HierarchyId,
                    It.Is<IEnumerable<string>>(keys => keys.Contains("ApproverMetadataKey.Location")),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["ApproverMetadataKey.Location"]);

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
        public async Task ExecuteAsync_WithInvalidApproverMetadataKey_ReturnsError()
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

            var request = new EditRuleRequest
            {
                HierarchyId = 1,
                LevelNumber = 1,
                RuleNumber = "1",
                QueryMatrix = queryMatrix
            };

            var existingRule = new RuleDto
            {
                HierarchyId = request.HierarchyId,
                LevelNumber = request.LevelNumber,
                RuleNumber = request.RuleNumber,
                QueryMatrix = queryMatrix
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<EditRuleRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ruleRepositoryMock
                .Setup(x => x.GetRuleAsync(
                    request.HierarchyId,
                    request.LevelNumber,
                    request.RuleNumber,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRule);

            _ruleRepositoryMock
                .Setup(x => x.GetExistingMetadataKeysAsync(
                    request.HierarchyId,
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Messages, Does.Contain("Error editing rule: The following metadata keys do not exist: ApproverMetadataKey.InvalidKey"));
            });
        }
    }
}
