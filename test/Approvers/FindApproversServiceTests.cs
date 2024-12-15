using FluentValidation;
using FluentValidation.Results;
using Moq;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.Approvers.FindApprovers;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Processors;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Approvers
{
    [TestFixture]
    public class FindApproversServiceTests
    {
        private Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock;
        private Mock<IQueryProcessor> _queryProcessorMock;
        private Mock<IValidator<FindApproversRequest>> _validatorMock;
        private FindApproversService _service;

        [SetUp]
        public void Setup()
        {
            _readOnlyRepositoryMock = new Mock<IReadOnlyRepositoryAsync<int>>();
            _queryProcessorMock = new Mock<IQueryProcessor>();
            _validatorMock = new Mock<IValidator<FindApproversRequest>>();

            _service = new FindApproversService(
                _readOnlyRepositoryMock.Object,
                _queryProcessorMock.Object,
                _validatorMock.Object);
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

            Expression<Func<Employee, bool>> capturedPredicate = null;

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<FindApproversRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _queryProcessorMock
                .Setup(x => x.BuildExpression<Employee>(request.QueryMatrix))
                .Returns((Expression<Func<Employee, bool>>)(e => e.Title == "Manager"));

            _readOnlyRepositoryMock
                .Setup(x => x.FindAllByPredicateAndTransformAsync(
                    It.IsAny<Expression<Func<Employee, bool>>>(),
                    It.IsAny<Expression<Func<Employee, EmployeeDto>>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Employee, bool>>, Expression<Func<Employee, EmployeeDto>>, CancellationToken>(
                    (pred, _, _) => capturedPredicate = pred)
                .ReturnsAsync(expectedEmployees);

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Data.PotentialApprovers, Is.EqualTo(expectedEmployees));
            });

            _readOnlyRepositoryMock.Verify(
                x => x.FindAllByPredicateAndTransformAsync(
                    It.IsAny<Expression<Func<Employee, bool>>>(),
                    It.IsAny<Expression<Func<Employee, EmployeeDto>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
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

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<FindApproversRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                new ValidationFailure("HierarchyId", "Hierarchy does not exist")
                }));

            // Act
            var result = await _service.ExecuteAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Messages, Contains.Item("Hierarchy does not exist"));
            });

            _readOnlyRepositoryMock.Verify(
                x => x.FindAllByPredicateAndTransformAsync(
                    It.IsAny<Expression<Func<Employee, bool>>>(),
                    It.IsAny<Expression<Func<Employee, EmployeeDto>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
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

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<FindApproversRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                new ValidationFailure("QueryMatrix", "Query matrix can only contain Employee fields")
                }));

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
}
