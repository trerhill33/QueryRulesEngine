using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.Rules.AddRuleToLevel;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Tests.Hierarchys;

[TestFixture]
public class AddRuleToLevelServiceTests
{
    private IRuleRepository _ruleRepository;
    private ILevelRepository _levelRepository;
    private IValidator<AddRuleToLevelRequest> _validator;
    private AddRuleToLevelService _service;

    [SetUp]
    public void Setup()
    {
        _ruleRepository = A.Fake<IRuleRepository>();
        _levelRepository = A.Fake<ILevelRepository>();
        _validator = A.Fake<IValidator<AddRuleToLevelRequest>>();

        _service = new AddRuleToLevelService(
            _ruleRepository,
            _levelRepository,
            _validator);
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

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _ruleRepository.GetNextRuleNumberAsync(
            request.HierarchyId,
            request.LevelNumber,
            A<CancellationToken>._))
            .Returns(1);

        A.CallTo(() => _ruleRepository.CreateRuleAsync(A<RuleDto>._, A<CancellationToken>._))
            .Invokes((RuleDto rule, CancellationToken _) => capturedRule = rule)
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

        A.CallTo(() => _ruleRepository.CreateRuleAsync(A<RuleDto>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
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

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _ruleRepository.GetExistingMetadataKeysAsync(
            request.HierarchyId,
            A<IEnumerable<string>>.That.Contains("ApproverMetadataKey.Location"),
            A<CancellationToken>._))
            .Returns(Task.FromResult(new List<string> { "ApproverMetadataKey.Location" }));

        A.CallTo(() => _ruleRepository.GetNextRuleNumberAsync(
            request.HierarchyId,
            request.LevelNumber,
            A<CancellationToken>._))
            .Returns(1);

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

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

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
            Assert.That(result.Messages, Does.Contain("Error adding rule to level: The following metadata keys do not exist: ApproverMetadataKey.InvalidKey"));
        });
    }
}