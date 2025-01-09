namespace QueryRulesEngine.Persistence.Entities
{
    public class MetadataKey : AuditableEntity<int>
    {
        public int HierarchyId { get; set; }
        public string KeyName { get; set; }
        public virtual Hierarchy Hierarchy { get; set; }
        public virtual ICollection<Metadata> Metadata { get; set; }
    }
}
