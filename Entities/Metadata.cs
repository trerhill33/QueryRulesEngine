using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Entities
{
    public class Metadata : AuditableEntity<int>
    {
        public int HierarchyId { get; set; }
        public string ApproverId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public virtual Hierarchy Hierarchy { get; set; }
        public virtual MetadataKey MetadataKey { get; set; }
        public virtual Approver Approver { get; set; }
    }
}
