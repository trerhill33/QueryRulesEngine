namespace QueryRulesEngine.Persistence.Entities
{
    public class Approver : AuditableEntity<int>
    {
        public int HierarchyId { get; set; }
        public string ApproverId { get; set; }
        public virtual Hierarchy Hierarchy { get; set; }
        public virtual Employee Employee { get; set; }
        public virtual ICollection<Metadata> Metadata { get; set; }
    }
}
