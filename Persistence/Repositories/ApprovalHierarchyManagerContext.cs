using Microsoft.EntityFrameworkCore;
using QueryRulesEngine.Persistence.Entities;

namespace ApprovalHierarchyManager.Infrastructure.Persistence.Contexts;

public class ApprovalHierarchyManagerContext
{
    public DbSet<MetadataKey> MetadataKey { get; set; }
    public DbSet<Metadata> Metadata { get; set; }
    public DbSet<Approver> Approver { get; set; }
    public DbSet<Hierarchy> Hierarchy { get; set; }
    public DbSet<Employee> Employee { get; set; }
}