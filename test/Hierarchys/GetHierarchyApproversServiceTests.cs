using FakeItEasy;
using FluentValidation.Results;
using FluentValidation;
using QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers.Models;
using QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QueryRulesEngine.Tests.Hierarchys;

[TestFixture]
public class GetHierarchyApproversServiceTests
{
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IValidator<GetHierarchyApproversRequest> _validator;
    private GetHierarchyApproversService _service;
    private List<Employee> _testData;

    [SetUp]
    public void Setup()
    {
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _validator = A.Fake<IValidator<GetHierarchyApproversRequest>>();
        _service = new GetHierarchyApproversService(_readOnlyRepository, _validator);

        // Setup test data
        _testData =
        [
            new Employee
            {
                TMID = "TM001",
                Name = "John Level1",
                Email = "john.level1@test.com",
                Title = "Manager",
                JobCode = "MGR",
                Approvers =
                [
                    new()
                    {
                        HierarchyId = 1,
                        ApproverId = "TM001",
                        Metadata =
                        [
                            new() { HierarchyId = 1, ApproverId = "TM001", Key = "level.1.FinancialLimit", Value = "5000" }
                        ]
                    }
                ]
            },
            new Employee
            {
                TMID = "TM002",
                Name = "Jane Level2",
                Email = "jane.level2@test.com",
                Title = "Director",
                JobCode = "DIR",
                Approvers =
                [
                    new()
                    {
                        HierarchyId = 1,
                        ApproverId = "TM002",
                        Metadata =
                        [
                            new() { HierarchyId = 1, ApproverId = "TM002", Key = "level.2.FinancialLimit", Value = "25000" }
                        ]
                    }
                ]
            }
        ];

        // Setup repository to return test data
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateIncludeAsync(
            A<Expression<Func<Employee, bool>>>._,
            A<Expression<Func<Employee, object>>[]>._,
            A<CancellationToken>._))
            .ReturnsLazily((Expression<Func<Employee, bool>> predicate, Expression<Func<Employee, object>>[] includes, CancellationToken token) =>
            {
                var compiledPredicate = predicate.Compile();
                return _testData.Where(compiledPredicate).ToList();
            });
    }

    [Test]
    public async Task ExecuteAsync_ValidRequest_ReturnsApproversByLevel()
    {
        // Arrange
        var request = new GetHierarchyApproversRequest
        {
            HierarchyId = "1"
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        // Act
        Result<GetHierarchyApproversResponse>? result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.Approvers.ApprovalLevel1, Has.Count.EqualTo(1));
            Assert.That(result.Data.Approvers.ApprovalLevel2, Has.Count.EqualTo(1));

            // Level 1 Approver
            var level1Approver = result.Data.Approvers.ApprovalLevel1[0];
            Assert.That(level1Approver.TMID, Is.EqualTo("TM001"));
            Assert.That(level1Approver.Metadata, Has.Count.EqualTo(1));
            Assert.That(level1Approver.Metadata[0].Value, Is.EqualTo("5000"));

            // Level 2 Approver
            var level2Approver = result.Data.Approvers.ApprovalLevel2[0];
            Assert.That(level2Approver.TMID, Is.EqualTo("TM002"));
            Assert.That(level2Approver.Metadata, Has.Count.EqualTo(1));
            Assert.That(level2Approver.Metadata[0].Value, Is.EqualTo("25000"));
        });
    }

    [Test]
    public async Task ExecuteAsync_NoApprovers_ReturnsEmptyLists()
    {
        // Arrange
        var request = new GetHierarchyApproversRequest
        {
            HierarchyId = "999" // Non-existent hierarchy
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        // Clear test data
        _testData.Clear();

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.Approvers.ApprovalLevel1, Is.Empty);
            Assert.That(result.Data.Approvers.ApprovalLevel2, Is.Empty);
        });
    }

    [Test]
    public async Task ExecuteAsync_ValidationFails_ReturnsError()
    {
        // Arrange
        var request = new GetHierarchyApproversRequest
        {
            HierarchyId = "" // Invalid empty ID
        };

        var validationFailure = new ValidationFailure("HierarchyId", "Hierarchy ID is required");
        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult([validationFailure]));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Does.Contain("Hierarchy ID is required"));
        });
    }
}