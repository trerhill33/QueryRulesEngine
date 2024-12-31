using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers;
using QueryRulesEngine.Features.Hierarchies.GetHierarchyApprovers.Models;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Persistence;
using QueryRulesEngine.QueryEngine.Processors;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys;

[TestFixture]
public class GetHierarchyApproversServiceTests
{
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IValidator<GetHierarchyApproversRequest> _validator;
    private IMetadataQueryProcessor _queryProcessor;
    private IQueryPersistenceService _queryPersistenceService;
    private GetHierarchyApproversService _service;

    [SetUp]
    public void Setup()
    {
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _validator = A.Fake<IValidator<GetHierarchyApproversRequest>>();
        _queryProcessor = A.Fake<IMetadataQueryProcessor>();
        _queryPersistenceService = A.Fake<IQueryPersistenceService>();

        _service = new GetHierarchyApproversService(
            _readOnlyRepository,
            _validator,
            _queryProcessor,
            _queryPersistenceService);
    }

    [Test]
    public async Task ExecuteAsync_ValidRequest_ReturnsApproversForFedExHierarchy()
    {
        // Arrange
        var request = new GetHierarchyApproversRequest
        {
            OriginApplication = "fedEx",
            HierarchyType = "fedex-vanderbilt",
            RequestedByTMID = "331220",
            RequestedForTMID = "999120"
        };

        var hierarchy = new Hierarchy { Id = 1, Name = "fedex-vanderbilt" };

        var metadataKeys = new List<MetadataKey>
        {
            new() { HierarchyId = 1, KeyName = "level.1.rule.1.query:[_and][Employee.Title_eq_Manager]" },
            new() { HierarchyId = 1, KeyName = "level.2.rule.1.query:[_and][ApproverMetadataKey.FinancialLimit_gte_25000]" }
        };

        var employees = new List<Employee>
        {
            new()
            {
                TMID = "331220",
                Name = "John Level1",
                Email = "john.level1@test.com",
                Title = "Manager",
                JobCode = "MGR",
                Approvers =
                [
                    new()
                    {
                        HierarchyId = 1,
                        ApproverId = "331220",
                        Metadata =
                        [
                            new() { HierarchyId = 1, ApproverId = "331220", Key = "FinancialLimit", Value = "5000" }
                        ]
                    }
                ]
            },
            new()
            {
                TMID = "999120",
                Name = "Jane Level2",
                Email = "jane.level2@test.com",
                Title = "Director",
                JobCode = "DIR",
                Approvers =
                [
                    new()
                    {
                        HierarchyId = 1,
                        ApproverId = "999120",
                        Metadata =
                        [
                            new() { HierarchyId = 1, ApproverId = "999120", Key = "FinancialLimit", Value = "25000" }
                        ]
                    }
                ]
            }
        };

        // Setup validation
        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        // Setup hierarchy lookup
        A.CallTo(() => _readOnlyRepository.FindByPredicateAsync<Hierarchy>(
            A<Expression<Func<Hierarchy, bool>>>._,
            A<CancellationToken>._))
            .Returns(hierarchy);

        // Setup metadata keys lookup
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync<MetadataKey>(
            A<Expression<Func<MetadataKey, bool>>>._,
            A<CancellationToken>._))
            .Returns(metadataKeys);

        // Setup employee lookups
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateIncludeAsync(
            A<Expression<Func<Employee, bool>>>._,
            A<Expression<Func<Employee, object>>[]>._,
            A<CancellationToken>._))
            .ReturnsNextFromSequence([employees[0]], new List<Employee> { employees[1] });

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.Approvers.ApprovalLevel1, Has.Count.EqualTo(1));
            Assert.That(result.Data.Approvers.ApprovalLevel2, Has.Count.EqualTo(1));

            var level1Approver = result.Data.Approvers.ApprovalLevel1[0];
            Assert.That(level1Approver.TMID, Is.EqualTo("331220"));
            Assert.That(level1Approver.Metadata[0].Value, Is.EqualTo("5000"));

            var level2Approver = result.Data.Approvers.ApprovalLevel2[0];
            Assert.That(level2Approver.TMID, Is.EqualTo("999120"));
            Assert.That(level2Approver.Metadata[0].Value, Is.EqualTo("25000"));
        });
    }

    [Test]
    public async Task ExecuteAsync_HierarchyNotFound_ReturnsError()
    {
        // Arrange
        var request = new GetHierarchyApproversRequest
        {
            OriginApplication = "fedEx",
            HierarchyType = "invalid-hierarchy",
            RequestedByTMID = "331220",
            RequestedForTMID = "999120"
        };

        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        A.CallTo(() => _readOnlyRepository.FindByPredicateAsync<Hierarchy>(
            A<Expression<Func<Hierarchy, bool>>>._,
            A<CancellationToken>._))
            .Returns((Hierarchy)null);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Does.Contain("Hierarchy not found for type: invalid-hierarchy"));
        });
    }

    [Test]
    public async Task ExecuteAsync_ValidationFails_ReturnsError()
    {
        // Arrange
        var request = new GetHierarchyApproversRequest
        {
            OriginApplication = "",
            HierarchyType = "",
            RequestedByTMID = "",
            RequestedForTMID = ""
        };

        var validationFailure = new ValidationFailure("HierarchyType", "Hierarchy type is required");
        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult([validationFailure]));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Does.Contain("Hierarchy type is required"));
        });
    }

    [Test]
    public async Task ExecuteAsync_RetailCorporate_ReturnsManagerAndMelissa()
    {
        // Arrange
        var request = new GetHierarchyApproversRequest
        {
            OriginApplication = "fedEx",
            HierarchyType = "RetailCorporate",
            RequestedByTMID = "USER_TMID",
            RequestedForTMID = null
        };

        var hierarchy = new Hierarchy { Id = 1, Name = "RetailCorporate" };

        var metadataKeys = new List<MetadataKey>
    {
        new()
        {
            HierarchyId = 1,
            KeyName = "level.1.rule.1.query:[_and][Employee.TMID_eq_@Context.RequestedTMIDReportsTo]"
        },
        new()
        {
            HierarchyId = 1,
            KeyName = "level.2.rule.1.query:[_and][Employee.TMID_eq_MELISSA_TMID]"
        }
    };

        var employees = new List<Employee>
    {
        new()
        {
            TMID = "MANAGER_TMID",
            Name = "Manager Name",
            Email = "manager@test.com",
            Title = "Manager",
            JobCode = "MGR",
            Approvers = new List<Approver>
            {
                new()
                {
                    HierarchyId = 1,
                    ApproverId = "MANAGER_TMID",
                    Metadata = new List<Metadata>
                    {
                        new()
                        {
                            HierarchyId = 1,
                            ApproverId = "MANAGER_TMID",
                            Key = "Role",
                            Value = "Manager"
                        }
                    }
                }
            }
        },
        new()
        {
            TMID = "MELISSA_TMID",
            Name = "Melissa Allen",
            Email = "melissa.allen@test.com",
            Title = "Notification Receiver",
            JobCode = "NOTIFY",
            Approvers = new List<Approver>
            {
                new()
                {
                    HierarchyId = 1,
                    ApproverId = "MELISSA_TMID",
                    Metadata = new List<Metadata>
                    {
                        new()
                        {
                            HierarchyId = 1,
                            ApproverId = "MELISSA_TMID",
                            Key = "Role",
                            Value = "Notifier"
                        }
                    }
                }
            }
        }
    };

        // Setup validation
        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        // Setup hierarchy lookup
        A.CallTo(() => _readOnlyRepository.FindByPredicateAsync<Hierarchy>(
            A<Expression<Func<Hierarchy, bool>>>._,
            A<CancellationToken>._))
            .Returns(hierarchy);

        // Setup metadata keys lookup
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync<MetadataKey>(
            A<Expression<Func<MetadataKey, bool>>>._,
            A<CancellationToken>._))
            .Returns(metadataKeys);

        // Setup employee lookups
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateIncludeAsync(
            A<Expression<Func<Employee, bool>>>._,
            A<Expression<Func<Employee, object>>[]>._,
            A<CancellationToken>._))
            .ReturnsNextFromSequence(
                new List<Employee> { employees[0] },  // Level 1 - Manager
                new List<Employee> { employees[1] }   // Level 2 - Melissa
            );

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);

            // Level 1 - Manager
            Assert.That(result.Data.Approvers.ApprovalLevel1, Has.Count.EqualTo(1));
            var level1Approver = result.Data.Approvers.ApprovalLevel1[0];
            Assert.That(level1Approver.TMID, Is.EqualTo("MANAGER_TMID"));
            Assert.That(level1Approver.Name, Is.EqualTo("Manager Name"));
            Assert.That(level1Approver.Metadata[0].Key, Is.EqualTo("Role"));
            Assert.That(level1Approver.Metadata[0].Value, Is.EqualTo("Manager"));

            // Level 2 - Melissa
            Assert.That(result.Data.Approvers.ApprovalLevel2, Has.Count.EqualTo(1));
            var level2Approver = result.Data.Approvers.ApprovalLevel2[0];
            Assert.That(level2Approver.TMID, Is.EqualTo("MELISSA_TMID"));
            Assert.That(level2Approver.Name, Is.EqualTo("Melissa Allen"));
            Assert.That(level2Approver.Metadata[0].Key, Is.EqualTo("Role"));
            Assert.That(level2Approver.Metadata[0].Value, Is.EqualTo("Notifier"));
        });
    }

    [Test]
    public async Task ExecuteAsync_HFA_ReturnsBillKudlets()
    {
        // Arrange
        var request = new GetHierarchyApproversRequest
        {
            OriginApplication = "fedEx",
            HierarchyType = "HFA",
            RequestedByTMID = "USER_TMID",
            RequestedForTMID = null
        };

        var hierarchy = new Hierarchy { Id = 2, Name = "HFA" };

        var metadataKeys = new List<MetadataKey>
    {
        new()
        {
            HierarchyId = 2,
            KeyName = "level.1.rule.1.query:[_and][Employee.TMID_eq_BILL_TMID]"
        }
    };

        var employees = new List<Employee>
    {
        new()
        {
            TMID = "BILL_TMID",
            Name = "Bill Kudlets",
            Email = "bill.kudlets@test.com",
            Title = "HFA Approver",
            JobCode = "MGR",
            Approvers = new List<Approver>
            {
                new()
                {
                    HierarchyId = 2,
                    ApproverId = "BILL_TMID",
                    Metadata = new List<Metadata>
                    {
                        new()
                        {
                            HierarchyId = 2,
                            ApproverId = "BILL_TMID",
                            Key = "Role",
                            Value = "Primary"
                        }
                    }
                }
            }
        }
    };

        SetupBasicMocks(request, hierarchy, metadataKeys, employees);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);

            // Level 1 - Bill Kudlets
            Assert.That(result.Data.Approvers.ApprovalLevel1, Has.Count.EqualTo(1));
            var level1Approver = result.Data.Approvers.ApprovalLevel1[0];
            Assert.That(level1Approver.TMID, Is.EqualTo("BILL_TMID"));
            Assert.That(level1Approver.Name, Is.EqualTo("Bill Kudlets"));

            // Level 2 - Should be empty for HFA
            Assert.That(result.Data.Approvers.ApprovalLevel2, Is.Empty);
        });
    }

    [Test]
    public async Task ExecuteAsync_Vanderbilt_ReturnsMichelleWithBackups()
    {
        // Arrange
        var request = new GetHierarchyApproversRequest
        {
            OriginApplication = "fedEx",
            HierarchyType = "fedex-vanderbilt",
            RequestedByTMID = "USER_TMID",
            RequestedForTMID = null
        };

        var hierarchy = new Hierarchy { Id = 3, Name = "fedex-vanderbilt" };

        var metadataKeys = new List<MetadataKey>
    {
        new()
        {
            HierarchyId = 3,
            KeyName = "level.1.rule.1.query:[_or][Employee.TMID_eq_MICHELLE_TMID][_and][ApproverMetadataKey.BackupFor_eq_MICHELLE_TMID]"
        }
    };

        var employees = new List<Employee>
    {
        new()
        {
            TMID = "MICHELLE_TMID",
            Name = "Michelle Batson",
            Email = "michelle.batson@test.com",
            Title = "Primary Approver",
            JobCode = "MGR",
            Approvers = new List<Approver>
            {
                new()
                {
                    HierarchyId = 3,
                    ApproverId = "MICHELLE_TMID",
                    Metadata = new List<Metadata>
                    {
                        new()
                        {
                            HierarchyId = 3,
                            ApproverId = "MICHELLE_TMID",
                            Key = "Role",
                            Value = "Primary"
                        }
                    }
                }
            }
        },
        new()
        {
            TMID = "BRAD_TMID",
            Name = "Brad Magnusen",
            Email = "brad.magnusen@test.com",
            Title = "Backup Approver",
            JobCode = "MGR",
            Approvers = new List<Approver>
            {
                new()
                {
                    HierarchyId = 3,
                    ApproverId = "BRAD_TMID",
                    Metadata = new List<Metadata>
                    {
                        new()
                        {
                            HierarchyId = 3,
                            ApproverId = "BRAD_TMID",
                            Key = "BackupFor",
                            Value = "MICHELLE_TMID"
                        }
                    }
                }
            }
        },
        new()
        {
            TMID = "ADELE_TMID",
            Name = "Adele Bryant",
            Email = "adele.bryant@test.com",
            Title = "Backup Approver",
            JobCode = "MGR",
            Approvers = new List<Approver>
            {
                new()
                {
                    HierarchyId = 3,
                    ApproverId = "ADELE_TMID",
                    Metadata = new List<Metadata>
                    {
                        new()
                        {
                            HierarchyId = 3,
                            ApproverId = "ADELE_TMID",
                            Key = "BackupFor",
                            Value = "MICHELLE_TMID"
                        },
                        new()
                        {
                            HierarchyId = 3,
                            ApproverId = "ADELE_TMID",
                            Key = "Location",
                            Value = "Silverton"
                        }
                    }
                }
            }
        }
    };

        SetupBasicMocks(request, hierarchy, metadataKeys, employees);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);

            // Level 1 - Should include Michelle and both backups
            Assert.That(result.Data.Approvers.ApprovalLevel1, Has.Count.EqualTo(3));

            var approvers = result.Data.Approvers.ApprovalLevel1;

            // Verify Michelle (Primary)
            var michelle = approvers.First(a => a.TMID == "MICHELLE_TMID");
            Assert.That(michelle.Name, Is.EqualTo("Michelle Batson"));
            Assert.That(michelle.Metadata.Any(m => m.Key == "Role" && m.Value == "Primary"), Is.True);

            // Verify Brad (Backup)
            var brad = approvers.First(a => a.TMID == "BRAD_TMID");
            Assert.That(brad.Name, Is.EqualTo("Brad Magnusen"));
            Assert.That(brad.Metadata.Any(m => m.Key == "BackupFor" && m.Value == "MICHELLE_TMID"), Is.True);

            // Verify Adele (Backup with Location)
            var adele = approvers.First(a => a.TMID == "ADELE_TMID");
            Assert.That(adele.Name, Is.EqualTo("Adele Bryant"));
            Assert.That(adele.Metadata.Any(m => m.Key == "BackupFor" && m.Value == "MICHELLE_TMID"), Is.True);
            Assert.That(adele.Metadata.Any(m => m.Key == "Location" && m.Value == "Silverton"), Is.True);

            // Level 2 - Should be empty for Vanderbilt
            Assert.That(result.Data.Approvers.ApprovalLevel2, Is.Empty);
        });
    }

    // Helper method for common mock setup
    private void SetupBasicMocks(
        GetHierarchyApproversRequest request,
        Hierarchy hierarchy,
        List<MetadataKey> metadataKeys,
        List<Employee> employees)
    {
        // Setup validation
        A.CallTo(() => _validator.ValidateAsync(request, A<CancellationToken>._))
            .Returns(new ValidationResult());

        // Setup hierarchy lookup
        A.CallTo(() => _readOnlyRepository.FindByPredicateAsync<Hierarchy>(
            A<Expression<Func<Hierarchy, bool>>>._,
            A<CancellationToken>._))
            .Returns(hierarchy);

        // Setup metadata keys lookup
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync<MetadataKey>(
            A<Expression<Func<MetadataKey, bool>>>._,
            A<CancellationToken>._))
            .Returns(metadataKeys);

        // Setup employee lookups
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateIncludeAsync(
            A<Expression<Func<Employee, bool>>>._,
            A<Expression<Func<Employee, object>>[]>._,
            A<CancellationToken>._))
            .Returns(employees);
    }
}