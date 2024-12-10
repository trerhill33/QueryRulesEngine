namespace QueryRulesEngine.Entities
{
    public class Approver
    {
        public int HierarchyId { get; set; }
        public string ApproverId { get; set; }
        public virtual Hierarchy Hierarchy { get; set; }
        public virtual Employee Employee { get; set; }
        public virtual ICollection<Metadata> Metadata { get; set; }
    }
}
