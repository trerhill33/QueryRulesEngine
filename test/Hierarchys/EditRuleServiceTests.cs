using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.Rules.EditRule;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Tests.Hierarchys;

[TestFixture]
public class EditRuleServiceTests
{
    private IRuleRepository _ruleRepository;
    private ILevelRepository _levelRepository;
    private IValidator<EditRuleRequest> _validator;
    private EditRuleService _service;

    [SetUp]
    public void Setup()
    {
        _ruleRepository = A.Fake<IRuleRepository>();
        _levelRepository = A.Fake<ILevelRepository>();
        _validator = A.Fake<IValidator<EditRuleRequest>>();

        _service = new EditRuleService(
            _ruleRepository,
            _levelRepository,
            _validator);
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

        var request = new EditRuleRequest(
            HierarchyId: 1,
            LevelNumber: 1,
            RuleNumber: "1",
            QueryMatrix: queryMatrix
        );

        var existingRule = new RuleDto
        {
            HierarchyId = request.HierarchyId,
            LevelNumber = request.LevelNumber,
            RuleNumber = request.RuleNumber,
            QueryMatrix = queryMatrix
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _ruleRepository.GetRuleAsync(
            request.HierarchyId,
            request.LevelNumber,
            request.RuleNumber,
            A<CancellationToken>._))
            .Returns(existingRule);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.That(result.Succeeded, Is.True);

        A.CallTo(() => _ruleRepository.UpdateRuleAsync(
            A<RuleDto>.That.Matches(r =>
                r.HierarchyId == request.HierarchyId &&
                r.LevelNumber == request.LevelNumber &&
                r.RuleNumber == request.RuleNumber &&
                r.QueryMatrix == request.QueryMatrix),
            A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
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

        var request = new EditRuleRequest(
            HierarchyId: 1,
            LevelNumber: 1,
            RuleNumber: "999", // Non-existent rule
            QueryMatrix: queryMatrix
        );

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _ruleRepository.GetRuleAsync(
            request.HierarchyId,
            request.LevelNumber,
            request.RuleNumber,
            A<CancellationToken>._))
            .Returns((RuleDto)null);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Contains.Item("Rule not found."));
        });

        A.CallTo(() => _ruleRepository.UpdateRuleAsync(
            A<RuleDto>._,
            A<CancellationToken>._))
            .MustNotHaveHappened();
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

        var request = new EditRuleRequest(
            HierarchyId: 1,
            LevelNumber: 1,
            RuleNumber: "", // Invalid empty rule number
            QueryMatrix: queryMatrix
        );

        var validationFailures = new[]
        {
            new ValidationFailure("RuleNumber", "Rule number is required"),
            new ValidationFailure("QueryMatrix", "Query matrix must contain at least one condition")
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Is.EquivalentTo(validationFailures.Select(f => f.ErrorMessage)));
        });

        A.CallTo(() => _ruleRepository.GetRuleAsync(
            A<int>._,
            A<int>._,
            A<string>._,
            A<CancellationToken>._))
            .MustNotHaveHappened();
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

        var request = new EditRuleRequest(
            HierarchyId: 1,
            LevelNumber: 1,
            RuleNumber: "1",
            QueryMatrix: queryMatrix
        );

        var existingRule = new RuleDto
        {
            HierarchyId = request.HierarchyId,
            LevelNumber = request.LevelNumber,
            RuleNumber = request.RuleNumber
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _ruleRepository.GetRuleAsync(
            request.HierarchyId,
            request.LevelNumber,
            request.RuleNumber,
            A<CancellationToken>._))
            .Returns(existingRule);

        A.CallTo(() => _ruleRepository.GetExistingMetadataKeysAsync(
            request.HierarchyId,
            A<IEnumerable<string>>.That.Contains("ApproverMetadataKey.Location"),
            A<CancellationToken>._))
            .Returns(Task.FromResult(new List<string> { "ApproverMetadataKey.Location" }));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.That(result.Succeeded, Is.True);

        A.CallTo(() => _ruleRepository.GetExistingMetadataKeysAsync(
            request.HierarchyId,
            A<IEnumerable<string>>.That.Contains("ApproverMetadataKey.Location"),
            A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
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

        var request = new EditRuleRequest(
            HierarchyId: 1,
            LevelNumber: 1,
            RuleNumber: "1",
            QueryMatrix: queryMatrix
        );

        var existingRule = new RuleDto
        {
            HierarchyId = request.HierarchyId,
            LevelNumber = request.LevelNumber,
            RuleNumber = request.RuleNumber,
            QueryMatrix = queryMatrix
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _ruleRepository.GetRuleAsync(
            request.HierarchyId,
            request.LevelNumber,
            request.RuleNumber,
            A<CancellationToken>._))
            .Returns(existingRule);

        A.CallTo(() => _ruleRepository.GetExistingMetadataKeysAsync(
            request.HierarchyId,
            A<IEnumerable<string>>._,
            A<CancellationToken>._))
            .Returns(Task.FromResult(new List<string>()));

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
