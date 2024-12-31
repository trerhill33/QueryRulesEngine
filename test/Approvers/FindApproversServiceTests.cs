using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.Approvers.FindApprovers;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Processors;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Approvers;

[TestFixture]
public class FindApproversServiceTests
{
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IQueryProcessor _queryProcessor;
    private IValidator<FindApproversRequest> _validator;
    private FindApproversService _service;

    [SetUp]
    public void Setup()
    {
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _queryProcessor = A.Fake<IQueryProcessor>();
        _validator = A.Fake<IValidator<FindApproversRequest>>();

        _service = new FindApproversService(
            _readOnlyRepository,
            _queryProcessor,
            _validator);
    }

    [Test]
    public async Task ExecuteAsync_ValidRequest_ReturnsMatchingEmployees()
    {
        // Arrange
        var request = new FindApproversRequest(
            HierarchyId: 1,
            QueryMatrix: new QueryMatrix
            {
                LogicalOperator = QueryOperator.And,
                Conditions =
                [
                    new QueryCondition
                {
                    Field = "Employee.Title",
                    Operator = QueryOperator.Equal,
                    Value = ConditionValue.Single("Manager")
                }
                ]
            });

        var expectedEmployees = new List<EmployeeDto>
    {
        new()
        {
            TMID = "123",
            Name = "John Manager",
            Email = "john@test.com",
            Title = "Manager",
            JobCode = "MGR"
        }
    };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _queryProcessor.BuildExpression<Employee>(request.QueryMatrix))
            .Returns((Expression<Func<Employee, bool>>)(e => e.Title == "Manager"));

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync(
                A<Expression<Func<Employee, bool>>>._,
                A<Expression<Func<Employee, EmployeeDto>>>._,
                A<CancellationToken>._))
            .Returns(expectedEmployees);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.PotentialApprovers, Is.EqualTo(expectedEmployees));
        });

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync(
            A<Expression<Func<Employee, bool>>>._,
            A<Expression<Func<Employee, EmployeeDto>>>._,
            A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExecuteAsync_InvalidHierarchy_ReturnsError()
    {
        // Arrange
        var request = new FindApproversRequest(1, new QueryMatrix
        {
            LogicalOperator = QueryOperator.And,
            Conditions = []
        });

        var validationFailures = new[]
        {
        new ValidationFailure("HierarchyId", "Hierarchy does not exist")
    };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Contains.Item("Hierarchy does not exist"));
        });

        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAndTransformAsync(
            A<Expression<Func<Employee, bool>>>._,
            A<Expression<Func<Employee, EmployeeDto>>>._,
            A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task ExecuteAsync_InvalidQueryMatrix_ReturnsError()
    {
        // Arrange
        var request = new FindApproversRequest(1, new QueryMatrix
        {
            LogicalOperator = QueryOperator.And,
            Conditions =
            [
                new QueryCondition
            {
                Field = "InvalidField",  // Not an Employee field
                Operator = QueryOperator.Equal,
                Value = ConditionValue.Single("Value")
            }
            ]
        });

        var validationFailures = new[]
        {
        new ValidationFailure("QueryMatrix", "Query matrix can only contain Employee fields")
    };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Contains.Item("Query matrix can only contain Employee fields"));
        });
    }
}
